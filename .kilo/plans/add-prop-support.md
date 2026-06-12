# Entity Architecture Assessment

## Requirements

| Entity Type | Needs | Doesn't Need | Examples |
|-------------|-------|--------------|----------|
| Player | Sprite, animation, hitboxes (attack), collision, clamping, movement, combat stats, state machine | — | PlayerEntity, MageEntity |
| Enemy | Sprite, animation, hitboxes (attack), collision, clamping, movement, combat stats, AI state machine | Player input routing | SkeletonEntity, BossEntity |
| Destroyable Prop | Sprite (break animation), collision (push), clamping, TakeDamage (break) | Attack hitboxes, movement, combat stats (Health/IsAlive/Died), AI | GarbageCanEntity, BarrelEntity |
| Background | Static sprite, movement bounds | Everything above | BackgroundEntity (existing, separate branch) |

## Current Interface Surface

```text
ActorEntity : ICollisionActor, IAnimated, IHitboxProvider, IMoveableEntity
  PlayerEntity : ICombatant
```

### What Works

- **Multiple player/enemy types** — clean. Extend `ActorEntity`, implement `ICombatant`, wire own state machine. No changes needed.
- **"Super" types** — best handled as expanded states/triggers in the existing Stateless controller, not new subclasses. No changes needed.
- **BackgroundEntity** — already on a separate branch, doesn't touch these interfaces. No changes needed.
- **ClampToBounds, OnCollision, Draw, null-guarded Update** — shared implementation on ActorEntity benefits all subclasses.

### What Needs Attention

**1. `GameLoop` hit resolution checks `is ICombatant`, excluding destroyable props.**

A garbage can needs `TakeDamage` to break, but `ICombatant` demands `Health`, `MaxHealth`, `IsAlive`, `Died` event — too much for a prop. The empty virtual `TakeDamage` on `ActorEntity` is unreachable because GameLoop never calls it on non-`ICombatant` entities.

**Fix:** Introduce `IDamageable` (lightweight, just `TakeDamage`), have `ICombatant : IDamageable`, and update GameLoop to check `is IDamageable`.

**2. `IHitboxProvider` on `ActorEntity` means all subclasses declare attack capability.**

A garbage can carries unused `CurrentMove = null` and `HitboxService = null` because `ActorEntity` requires them. This is a minor memory cost but a conceptual smell — it signals intent the entity doesn't have.

**Two options:**
- **Accept the tax** — ~3 unused reference slots per prop. No structural change.
- **Split** — introduce `CombatantActorEntity : ActorEntity` that adds `IHitboxProvider`. Garbage cans extend `ActorEntity` directly without the attack surface. Adds hierarchy depth for purity.

I recommend **accepting the tax** for now. The null references are zero-cost at runtime (null-conditionals handle them), and the extra layer isn't worth the complexity until there are many prop types.

**3. `IMoveableEntity` on `ActorEntity` means all subclasses declare movement intent.**

A garbage can carries unused `MovementDirection = default` and `Speed = 200f`. Same analysis as #2 — accept the tax.

## Proposed Changes

### Phase A: Add `IDamageable` Interface

| Interface | Members | Consumed By |
|-----------|---------|-------------|
| `IDamageable` | `void TakeDamage(int)` | ActorEntity (base impl), GameLoop hit resolution |

- `ICombatant : IDamageable` (inherits `TakeDamage`, keeps `Health`/`MaxHealth`/`IsAlive`/`Died`)
- `ActorEntity` implements `IDamageable` (the empty virtual satisfies it)
- `GameLoop` checks `is IDamageable` instead of `is ICombatant`

```csharp
// GameLoop hit resolution — one check covers combatants + props
if (hit.Target is IDamageable damageable)
{
    damageable.TakeDamage(hit.Damage);
    hit.Target.Position += hit.Knockback;
}
```

### Phase B: Garbage Can Example

```csharp
public class GarbageCanEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
    : ActorEntity(name, position, scale, sprite)
{
    private bool _isBroken;

    public override void TakeDamage(int amount)
    {
        if (_isBroken) return;
        _isBroken = true;
        Sprite.SetAnimation("break");
        // Optionally: disable collision, remove from update loop, etc.
    }
}
```

That's it — 0 new interfaces needed by garbage cans beyond `IDamageable` inherited from `ActorEntity`. No `IHitboxProvider` or `IMoveableEntity` setup required (those properties stay null/default, Update() skips the hitbox block when `CurrentMove` is null).

### Phase C: Test Updates

- `TestSpatialEntity` implements `IDamageable` instead of `ICombatant` for hitbox resolution tests that test damage application (or keep `ICombatant` since it extends `IDamageable` anyway — no change needed).

### Phase D: Plan File Update

- Replace "Phase 5: Add Enemy Entity" with "Phase 5: Add Enemy + Destroyable Prop Entities"
- Update interface tables with `IDamageable` and `ICombatant : IDamageable`
- Note the "accept the tax" decision for unused `IHitboxProvider`/`IMoveableEntity` on props

## Files to Modify

| File | Change |
|------|--------|
| `MonoGameLearning.Core/Entities/Interfaces/IDamageable.cs` | **New** — `void TakeDamage(int)` |
| `MonoGameLearning.Core/Combat/ICombatant.cs` | Add `: IDamageable`, remove `TakeDamage` declaration (inherited) |
| `MonoGameLearning.Core/Entities/ActorEntity.cs` | Add `IDamageable` to interface list |
| `MonoGameLearning.Game/GameLoop/GameLoop.cs:137` | Change `is ICombatant` to `is IDamageable` |
| `MonoGameLearning.Game.Tests/HitboxTests.cs` | Update `TestSpatialEntity` if needed |
| `.kilo/plans/entity-composition-analysis.md` | Update plan status |

## Test Plan

1. Existing tests pass unchanged (backward compatible — `ICombatant : IDamageable` and `PlayerEntity.TakeDamage` satisfies both)
2. New test: `IDamageable` check in GameLoop passes for non-`ICombatant` entities
3. Verify garbage can with `is IDamageable` receives damage without implementing `ICombatant`

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| `ICombatant` inheriting `TakeDamage` from `IDamageable` changes interface implementation pattern | PlayerEntity already has `TakeDamage` as a public method — satisfies both interfaces. No explicit interface implementation needed. |
| Adding a layer between `SpatialEntity` and `ActorEntity` for props | Deferred. Accept the tax of null IHitboxProvider/IMoveableEntity on props. |
| Super types needing different state machine patterns | Handled by existing Stateless pattern — add states/triggers, no new classes. |