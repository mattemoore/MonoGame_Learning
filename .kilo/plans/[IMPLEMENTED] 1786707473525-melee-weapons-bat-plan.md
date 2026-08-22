# Melee Weapons v1 — Baseball Bat

> **Status:** Implemented. The weapon system shipped as specified below, except the render approach — the static bat-overlay/`Texture` mechanics in this v1 document were superseded by the `1787178079168-bat-swing-sync` plan (AnimatedSprite frames + per-frame anchors + apex-only hitboxes). See that plan for the authoritative rendering/sync design.

## Goal

Add a melee weapon system with one concrete weapon (a **bat**). Bats spawn pre-placed in the level, the player picks them up by overlap, and enemies can spawn already armed. While armed, the holder's attack1 is replaced by a weapon swing: same attack1 animation (reused), damage similar to attack1, but a longer hitbox. The bat is rendered as a **separate overlaid sprite** anchored to the holder — never baked into entity animation sheets.

## Locked Decisions (from user)

| Decision | Answer |
| --- | --- |
| Weapon sources (v1) | Level-placed pickups + enemies armed at spawn. Barrel/enemy drops later. Enemy pickup later. |
| Losing the weapon | Cleared on **knockdown or death** (destroyed, not re-dropped). |
| Attack2/Attack3 while armed | Unchanged; only attack1 becomes the weapon swing. |
| Swing animation | Entity plays existing `attack1` anim (4 frames, 0.4s). Bat overlay stays vertical, single copied frame — no bat art in entity sheets. |
| Held visual | Bat drawn anchored to holder while idle/moving AND during the swing (two static anchor poses: carry / swing). |
| Bat art | Agent-generated placeholder PNG (~12×40 vertical brown bat), added to mgcb. Swappable later. |
| Bat stats | Damage 6, Light, no knockdown; hitboxes on frames 1–2, offset (45,0), size 70×45 (reach ~10–80px vs attack1's 12–58). Swing sfx `AttackSwing1`, impact sfx `HitHeavy` (both reused). |
| Armed enemy AI | `AttackRange` stays 70f — swings just connect more reliably. No AI changes. |
| Level 1 placement | 1 bat pickup at ~(350, 556); 1 armed Grunt in wave 2 (TriggerX 1600). |
| Re-pickup while armed | Pickup is consumed, weapon re-equipped (refresh, no stack). |

## Key Codebase Facts (verified)

- Attacks are `MoveData`-driven; `GameLoop._actionHandlers` calls `_player.Attack(_player.Attack1Move)`; enemy `OnAttackingEntry` uses `AttackMove`. Both fields are the swap seams.
- `MoveData` (Core/Combat): init-only — `AnimationKey`, `Damage`, `Strength`, `Knockdown`, `AttackSfx`, `ImpactSfx`, `FrameHitboxes` (frame index → `List<HitboxData>`).
- Player and enemy sprite sheets both define key `"attack1"` from the same adventurer atlas — one `AnimationKey` works for both.
- Hitbox pitfall: `AnimationFrameTracker.Reset()` means frame-0 hitboxes never fire through the live path. Reusing `attack1` (4 frames) with hitboxes on frames 1–2 avoids this entirely. **No new animation definitions are needed anywhere.**
- Pickup path exists: `PickupBase` (Core, texture-rendered) → `FoodPickupEntity`; `Level.Pickups` → `LevelDirector.SpawnPickups` → `CreatePickup` string-switch factory; `GameLoop.ResolvePickupOverlaps` is player-only AABB → `OnPickup(_player)` → existing pickup chime → `Destroy`.
- `EnemySpawnDef(string Type, SpawnSide Side, SpawnVertical Vertical, IReadOnlyList<PickupSpawnDef>? Drops = null)` — extend with weapon.
- Enemy pooling: `EnemyPool.Return` + `EnemyEntity.Reset` must leave no stale weapon.
- `IPickup.OnPickup(IDamageable target)` — equipping needs more than `IDamageable`; cast to `CombatActorBase` (Core type, both PlayerEntity and EnemyEntity inherit it). No new interface (keep it simple).
- Knockdown entry uses shared cached callbacks (`CombatActorBase.Callbacks`) for both player and enemy — one hook point clears weapons for both.

## Architecture

```text
Core/Combat/MeleeWeaponDef.cs        data: Name, SwingMove (MoveData), CarryOffset, SwingOffset, Texture2D? Texture
Core/Entities/Actor/CombatActorBase  EquippedWeapon, EquipWeapon(), UnequipWeapon(), clear-on-knockdown/dying/ResetActor,
                                     weapon overlay draw, IsInAttackingState hook, debug drawing
Game/Weapons/BatWeapon.cs            static: builds the one MeleeWeaponDef; Load(content) sets Texture ("images/bat")
Game/Entities/Pickup/WeaponPickupEntity.cs   PickupBase subclass; OnPickup → (target as CombatActorBase)?.EquipWeapon(def)
Game.AnimatedSprites                 unchanged (entity swing reuses "attack1")
Content/images/bat.png + .mgcb       placeholder texture
```

**Move swap pattern** (no GameLoop / state-controller changes):

- `PlayerEntity`: `Attack1Move` readonly field → private `_attack1Move` + property `Attack1Move => EquippedWeapon?.SwingMove ?? _attack1Move;`
- `EnemyEntity`: same for `AttackMove` → `EquippedWeapon?.SwingMove ?? _attackMove;` (`OnAttackingEntry` body unchanged)

**Weapon overlay draw** (in `CombatActorBase.Draw`, immediately after actor sprite — keeps actor/weapon ordering atomic):

- If `EquippedWeapon?.Texture != null`: draw texture centered at `Position + anchor`, anchor = `IsInAttackingState ? SwingOffset : CarryOffset`, X negated when facing left. Vertical bat is symmetric — no `SpriteEffects` flip needed.
- Starting anchor values (tune visually; frame is 48×60, center origin): Carry `(20, 0)`, Swing `(35, -10)`.

**Weapon loss** — three clear points:

1. Shared knockdown-entry callback (`Callbacks.OnKnockdownEntry`) → `UnequipWeapon()` (covers player + enemy).
2. Dying entry → `UnequipWeapon()` (bat gone before death anim finishes).
3. `ResetActor()` → `UnequipWeapon()` (covers player `Reset`/respawn and enemy pool `Return`/`Reset` — belt and suspenders).

## Ordered Task List

1. **Content**: generate `Content/images/bat.png` placeholder (~12×40 vertical brown bat; python3-PIL or ImageMagick script); add build entry to `Content.mgcb` mirroring `food_apple.png`.
2. **Core**: create `Combat/MeleeWeaponDef.cs` — init-only class mirroring `MoveData` style: `required string Name`, `required MoveData SwingMove`, `Vector2 CarryOffset`, `Vector2 SwingOffset`, `Texture2D? Texture` (settable, assigned once at load).
3. **Core**: extend `CombatActorBase`:
   - `public MeleeWeaponDef? EquippedWeapon { get; private set; }`, `EquipWeapon(MeleeWeaponDef)`, `UnequipWeapon()`.
   - Abstract `bool IsInAttackingState { get; }` alongside existing state hooks.
   - Clear weapon in shared knockdown-entry callback, dying entry, and `ResetActor`.
   - Draw weapon overlay after actor sprite (above rules); `Debug.Assert` guard not needed, but `Debug.WriteLine` if `EquippedWeapon != null && Texture == null` at draw (sentinel for forgotten `Load`).
   - `DrawDebug`: when armed, draw weapon name text + a small cross marker at the active anchor point.
4. **Game**: create `Weapons/BatWeapon.cs` — static `MeleeWeaponDef Bat` (MoveData: `AnimationKey = PlayerSprite.AnimationAttack1`, Damage 6, Light, `AttackSfx = SfxId.AttackSwing1`, `ImpactSfx = SfxId.HitHeavy`, `FrameHitboxes` frames 1–2 → `HitboxData { Offset = (45,0), Size = (70,45) }`; anchors above; Name "Bat") + `Load(ContentManager)` setting `Bat.Texture = content.Load<Texture2D>("images/bat")`. Call `BatWeapon.Load(Content)` in `GameLoop.LoadContent` next to other sprite loads.
5. **Game**: create `Entities/Pickup/WeaponPickupEntity.cs` — `PickupBase` subclass holding the `MeleeWeaponDef`; `OnPickup(IDamageable target)` → `(target as CombatActorBase)?.EquipWeapon(BatWeapon.Bat)`; sprite-less test ctor overload mirroring `PickupBase` conventions.
6. **Game**: `PlayerEntity` — `_attack1Move` private + swapping `Attack1Move` property; implement `IsInAttackingState`.
7. **Game**: `EnemyEntity` — `_attackMove` private + swapping `AttackMove` property; implement `IsInAttackingState`.
8. **Core**: `EnemySpawnDef` — add `string? Weapon = null` (trailing optional param, existing call sites unaffected).
9. **Game**: `LevelDirector` — `CreatePickup` switch: `"Bat" => new WeaponPickupEntity(def.Type, def.Position, BatWeapon.Bat)`; in `SpawnWave` after `Rent`: `if (def.Weapon != null) enemy.EquipWeapon(ResolveWeapon(def.Weapon))` with `"Bat" => BatWeapon.Bat, _ => throw ArgumentException`.
10. **Game**: `Level1` — add `new PickupSpawnDef("Bat", new Vector2(350f, 556f))` to `Pickups`; add `Weapon: "Bat"` to one wave-2 `EnemySpawnDef`.
11. **Docs**: check off the relevant ROADMAP.md melee-weapon sub-items that now exist; add a one-line weapon note to AGENTS.md architecture section if structure changed.

## Tests (mandatory per AGENTS.md — write before marking complete)

New `MonoGameLearning.Game.Tests/MeleeWeaponTests.cs` (+ small additions to `LevelDirectorTests` patterns), using existing fakes (`PlayerEntityTester`, `TestEnemyEntity`, `TestLevelDirector`/`TestEnemyPool`, `HitboxService` direct pattern from `HitboxTests`):

1. `EquipWeapon_SwapsPlayerAttack1Move` / `UnequipWeapon_RestoresAttack1Move`.
2. `WeaponPickup_OnPickup_EquipsActor`; `OnPickup_NonActorTarget_NoOp`.
3. `Knockdown_ClearsWeapon` (armed player tester + armed enemy tester, fire `TakeKnockdown`).
4. `Reset_ClearsWeapon` (player `Reset`; enemy `Reset` — pool rental leaves no stale weapon).
5. `ArmedEnemy_AttackingEntry_UsesWeaponMove` (fire `AttackStart`, assert `CurrentMove == Bat.SwingMove`).
6. Reach integration: register armed frame-1 hitboxes via `HitboxService` — target at ~75px is hit; unarmed attack1 at same distance misses.
7. Dedup: one swing hits a given target exactly once.
8. `SpawnWave_WithWeaponDef_SpawnsArmedEnemy`; `SpawnWave_UnknownWeapon_Throws`.
9. `CreatePickup_Bat_ReturnsWeaponPickupEntity` (and existing "Unknown" throw still passes).
10. No deadlock: armed attack completes → `Idling`, weapon still equipped (swing does not consume weapon).

## Risks / Pitfalls

- **AnimatedSprite.Controller resubscribe pitfall** — no new animations means the existing `PlayAnimation` unsub/set/sub pattern covers the swing unchanged.
- **Enemy pool staleness** — covered by `ResetActor` clear; test #4 guards it.
- **Facing flip mid-swing** — existing behavior; weapon anchor uses the same `Direction`, so bat mirrors consistently with hitboxes.
- **Knockdown interrupting an armed swing** — existing `TakeKnockdown` path aborts the attack; weapon clears via knockdown entry. Assert no lingering hitboxes (exit callbacks already clear them).
- **Null texture in headless tests** — draw path guards `Texture != null`; test double defs leave it null.

## GC / Performance

Weapon defs are static singletons; equipping is a reference assignment; swing path adds zero allocations (reuses existing `MoveData`/hitbox pipeline); armed draw adds exactly one `spriteBatch.Draw` per armed actor per frame.

## Validation

1. `dotnet build --warnaserror` — zero warnings.
2. `dotnet test` — all new + existing tests pass.
3. Manual playtest: pick up bat at x≈350 → swing reaches farther than punch; get knocked down → bat gone; wave 2 armed grunt visibly carries bat and swings with extended reach.

## Out of Scope (deferred)

Barrel/enemy weapon drops; enemy weapon pickup; weapon re-drop on loss; durability/timer expiry; throwables; additional weapon types (design supports them: new `MeleeWeaponDef` + factory case); real bat art / bat rotation animation; per-frame anchor tables; HUD weapon icon; attack2/3 weapon variants.
