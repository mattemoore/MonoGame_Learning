# PropEntity Refactor: Move Common OilDrum Fields Up to Base Class

## Goal

Move shared fields and methods from `OilDrumEntity` up into `PropEntity` (Core), mirroring the pattern used by `ActorEntity`/`PlayerEntity`. After the refactor, `PropEntity` provides sprite/health infrastructure and `OilDrumEntity` only owns OilDrum-specific logic.

---

## Changes

### 1. `MonoGameLearning.Core/Entities/PropEntity.cs`

Add fields, constructors, and methods to match the `ActorEntity` pattern:

- **Properties**: `AnimatedSprite Sprite { get; private set; }`, `float Scale { get; private set; }`, `int Health { get; protected set; }`, `int MaxHealth { get; protected set; }`, `bool IsAlive { get; protected set; } = true`
- **Event**: `event Action<PropEntity> Destroyed`
- **Constructor overload** (mirrors `ActorEntity`'s sprite constructor):
  `(string name, Vector2 position, float scale, AnimatedSprite sprite, float rotation = 0f)` — computes width/height from sprite dimensions
- **`Update`** override: calls `Sprite?.Update(gameTime)` + `base.Update(gameTime)`
- **`Draw`** virtual: draws `Sprite` at `Position` with `Rotation` and `Scale`
- **`DrawDebug`** override: base rectangle + blue rectangle
- **`TakeDamage`** virtual with default implementation:
  - Guard: `if (!IsAlive) return;`
  - `Health = Math.Max(0, Health - amount);`
  - If `Health <= 0`: `IsAlive = false; Destroyed?.Invoke(this);`

### 2. `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs`

**Remove** (redundant with base):
- Fields `_sprite`, `_scale`, `_health`
- Property `IsAlive` (use base)
- Event `Destroyed` (use base's `Action<PropEntity>`)
- Override `Draw` (base handles it)
- Override `DrawDebug` (base handles it)
- `_sprite.Update(gameTime)` call in `Update` (base handles it)

**Simplify constructor**:
```csharp
public OilDrumEntity(string name, Vector2 position, float scale, AnimatedSprite sprite)
    : base(name, position, scale, sprite)
{
    Health = 6;
    MaxHealth = 6;
    Sprite.Color = Color.White;
    _stateController = new(/* ... same config ... */);
}
```
(Entries still reference `Sprite`, `Health` — now inherited.)

**Simplify `TakeDamage`**:
- Remove duplicate `if (!IsAlive ...) return;` guard and `IsAlive = false; Destroyed?.Invoke(this);` (base handles this)
- Keep: damage mapping switch + `_stateController.Fire(OilDrumTrigger.Hit)`
- The death branch becomes just `Health <= 0` → no-op (base handles it)

**Simplify `Update`**:
- Keep `if (!IsAlive) return;`
- Keep hit-stun countdown logic
- Remove `_sprite.Update(gameTime)` (base does it)
- Keep `base.Update(gameTime)` call

### 3. `MonoGameLearning.Game/GameLoop/GameLoop.cs`

- Change `OnOilDrumDestroyed(OilDrumEntity drum)` → `OnOilDrumDestroyed(PropEntity drum)` to match base event type
- Body stays identical — `PropEntity` is sufficient for `_props.Remove(drum)` and `_collision.Remove(drum)`

### 4. `MonoGameLearning.Game.Tests/OilDrumStateTests.cs`

**`TestDamageableEntity` refactor**:
- Remove own `Health`, `IsAlive`, `Destroyed` (inherit from `PropEntity`)
- Constructor calls `base(name, position, width, height)` then sets `Health = 6; MaxHealth = 6;`
- `TakeDamage` override stays (same damage mapping logic)
- `UpdateHitStun` stays
- `CanTakeDamage` stays: `=> IsAlive && _stateController.State != OilDrumState.HitStun`
- Tests remain unchanged — they access `.Health`, `.IsAlive`, etc. which now resolve from base

---

## Verification

1. `dotnet build` — must compile cleanly
2. `dotnet test` — all existing tests must pass (OilDrum state tests, behavior tests)
3. No behavioral changes — OilDrum remains Normal→HitStun→Normal cycle, damage scaling, death

---

## What Stays in OilDrumEntity

- `OilDrumStateController _stateController` (+ `OilDrumStateController` class itself)
- `_hitStunTimer` field, `HitStunDuration` constant
- Damage mapping in `TakeDamage` override (amount switch with thresholds 12/8)
- Hit-stun countdown logic in `Update` override
- Health-to-animation mapping in state entry callbacks