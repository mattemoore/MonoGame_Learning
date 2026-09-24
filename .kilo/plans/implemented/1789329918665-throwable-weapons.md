# Throwable Weapons v1 — Knife

> **Status:** Implemented. One deviation from the sketch: `HitboxService.RegisterHitbox`
> takes an explicit `Vector2 center` (the `IHitboxProvider` interface has no position), and
> the knife placeholder PNGs are regenerated from `Utils/placeholder_gen.py` after fixing a
> buffer-corruption bug in `draw_baton` (see "Risks" below). Plus the `PendingMove` protected
> accessor on `PlayerEntity` for the headless test controller.

## Goal

Add throwable weapons to the Milestone 7 loot loop. The player picks up a throwable (knife) by overlap; while held, Attack1 throws it forward in the facing direction. The thrown entity (a projectile) flies with its own hitbox, damages the first valid target once, and despawns on hit or when it exceeds its range. Milliseconds-to-implement bat/knife feel, reusing the existing weapon overlay, attack animation, and `HitboxService` hit pipeline.

## Locked Decisions (from user)

| Decision | Answer |
| --- | --- |
| Plan location | Separate plan file; `ROADMAP.md` bullet links to it. |
| Throw trigger | Pick up to become armed; **Attack1 throws**, consuming the weapon. Attack2/Attack3 stay normal punches. |
| Projectile lifecycle | **Single-hit**: damages the first valid target once, despawns on hit or when past `MaxRange`. Not re-pickable. |
| Thrower scope | **Player only**; the projectile is `Faction`-driven and generic, so enemy throwing is a later add-on. |
| Weapon model | Introduce a `WeaponDef` base; `MeleeWeaponDef` + new `ThrowableWeaponDef` derive. **One weapon slot** — a throwable pickup replaces a held melee weapon and vice versa. |
| Throw timing | Projectile spawns on `ThrowableWeaponDef.SpawnFrame` (default 1) via the existing frame-advance hook, so it leaves the hand mid-swing. |
| Projectile collision | Reuse `HitboxService`: the projectile re-registers a fixed hitbox at its position each `Update`; add `DamageInfo.Source` so `GameLoop` can despawn the projectile that scored. |
| Knife art | Placeholder via `Utils/placeholder_gen.py` conventions (one-frame bar atlas + pickup icon); swappable later through the same atlas pipeline as the bat. |

## Key Codebase Facts (verified)

- `IWeaponWielder` (`Core/Combat/IWeaponWielder.cs:5`) currently takes/returns `MeleeWeaponDef`; `CombatActorBase.EquippedWeapon` (`CombatActorBase.cs:51`) and both `Attack1Move`/`AttackMove` swap seams (`PlayerEntity.cs:40`, `EnemyEntity.cs:75`) assume melee.
- The weapon overlay is region-keyed, not time-driven: `CombatActorBase.RenderWeaponOverlay` (`CombatActorBase.cs:118-151`) calls `MeleeWeaponDef` statics for anchor/frame/region; throwables reuse the same carry overlay and need these to become polymorphic.
- Frame-advance seam: `CombatActorBase.AdvanceFrameAndRegisterHitboxes` (`CombatActorBase.cs:211-220`) owns `FrameTracker.TryGetNewFrame`; there is no subclass hook yet.
- `HitboxService` (`Core/Combat/HitboxService.cs`) registers `ActiveHitbox` (owner + `RectangleF` computed once at registration) and dedups per owner+target. `ResolveHits` returns `DamageInfo` without a source (`HitboxService.cs:64-71`).
- `EntityService` probes capabilities on `Register` and injects `HitboxService` into any `IHitboxProvider` (`EntityService.cs:114-127,154-166`); it has no layer requirement for a hitbox-only entity.
- `DamageInfo` is a `readonly record struct` (`Core/Combat/DamageInfo.cs`), all `init`.
- `GameLoop.Update` order: `ProcessPending` → input handlers → updatables → `ResolveHits`/apply damage → prop collision → pickups → clamps. Input events arrive **after** `ProcessPending`, so a projectile returned to a pool on the previous frame is already deregistered before the next `Spawn`.
- Weapon pickup path: `PickupBase`/`IPickup` → `WeaponPickupEntity` (`Game/Entities/Pickups/WeaponPickupEntity.cs`) → `LevelDirectorCore.SpawnPickups` → `LevelEntityFactory.CreatePickup` (`LevelEntityFactory.cs:53-58`); `LevelContent` keys select the def.
- `AnimationFrameTracker.Reset()` sets frame 0 and marks it registered, so frame-0 hitboxes never fire via the live path; attack1 hitboxes live on frames 1–2. `SpawnFrame = 1` is safe.
- `placeholder_gen.py` emits `monogame-extended` atlas frames named `<slug>-NN` (`Utils/placeholder_gen.py:102`), matching `SpriteSheetAnimationExtensions.DefineFrames` naming, plus `<slug>-pickup.png`. A `SpriteSheetAsset` with no animation defs still loads the atlas regions.
- No new `SfxId` assets are required: reuse `SfxId.AttackSwing1` for the throw and `SfxId.HitLight`/`HitHeavy` for impact; pickups already chime via `SfxId.PickupHeal`.

## Architecture

```text
Core/Combat/WeaponDef.cs            NEW  abstract base: Name/Scale/FrameCenter/CarryRegion/CarryAnchor/
                                         CarryHandleOffset/Texture/Sheet; virtual ResolveRegion/
                                         ResolveAnchorAndFrame/ResolveHandleOffset/HasHandleOffsets; shared
                                         facing/handle static helpers (moved from MeleeWeaponDef)
Core/Combat/MeleeWeaponDef.cs       EDIT : WeaponDef; keeps SwingMove/SwingPrefix/SwingAnchors/
                                         SwingHandleOffsets, overrides the resolvers with swing logic
Core/Combat/ThrowableWeaponDef.cs   NEW  : WeaponDef; ThrowMove, SpawnFrame, ProjectileHitbox, Damage,
                                         Knockdown, Strength, ImpactSfx, ProjectileRegion, ProjectileScale,
                                         Speed, MaxRange, SpinDegreesPerSecond, SpawnOffset,
                                         ResolveProjectileRegion()
Core/Combat/ProjectileEntity.cs     NEW  Entity + IUpdatable/IRenderable/IDebugDrawable/IHitboxProvider;
                                         Launch/MarkHit/Expired; moves, re-registers its hitbox each frame
Core/Combat/ProjectileService.cs    NEW  lazy pool (rent/return), Spawn/Update/Despawn/Clear
Core/Combat/HitboxService.cs        EDIT add RegisterHitbox(...) overload; ResolveHits sets DamageInfo.Source
Core/Combat/DamageInfo.cs           EDIT add IHitboxProvider? Source
Core/Combat/IWeaponWielder.cs       EDIT EquipWeapon(WeaponDef)
Core/Entities/Actor/CombatActorBase EDIT EquippedWeapon : WeaponDef?; virtual OnFrameAdvanced hook;
                                         overlay uses virtual resolvers
Game/AnimatedSprites/KnifeSprite.cs NEW  SpriteSheetAsset("knife", "images/knife")
Game/Weapons/KnifeWeapon.cs         NEW  static ThrowableWeaponDef + Load(content)
Game/Levels/LevelContent.cs         EDIT add Knife
Game/Levels/LevelEntityFactory.cs   EDIT CreateProjectile(); CreatePickup Knife case; knifeWeapon param
Game/Levels/Level1.cs               EDIT add a Knife pickup (and/or a drum drop)
Game/Entities/Player/PlayerEntity.cs EDIT HeldThrowable/PrimaryAttack/Thrown event/OnFrameAdvanced override
Game/Entities/Enemy/EnemyEntity.cs  EDIT AttackMove melee-only type check
Game/Entities/Pickups/WeaponPickupEntity.cs EDIT ctor takes WeaponDef
Game/GameLoop/GameLoop.cs           EDIT subscribe Thrown, Action1→PrimaryAttack, ProjectileService wiring
Content/Content.mgcb + images/      EDIT knife.json, knife-texture.png, knife-pickup.png
```

Throw data flow:

1. Pickup overlap → `WeaponPickupEntity.OnPickup` → `IWeaponWielder.EquipWeapon(ThrowableWeaponDef)`.
2. Attack1 pressed → `GameLoop` calls `PlayerEntity.PrimaryAttack()` → `Throw(def)` sets `_pendingThrowable`, calls `Attack(def.ThrowMove)` (`AnimationKey = attack1`, no frame hitboxes).
3. On the frame hook at `frameIndex >= def.SpawnFrame`: `PlayerEntity` raises `Thrown(def, origin, Direction)`, then `UnequipWeapon()`.
4. `GameLoop.OnPlayerThrown` → `ProjectileService.Spawn(def, origin, facing, Faction.Player)` → `ProjectileEntity.Launch` + `EntityService.Register`.
5. Each projectile `Update`: move, `HitboxService.Clear(this)`, `RegisterHitbox(...)` at the new position, `Expired` when past `MaxRange`.
6. `GameLoop` resolves hits; for a hit whose `DamageInfo.Source is ProjectileEntity`, call `MarkHit()`. `ProjectileService.Update()` then despawns expired/hit projectiles and returns them to the pool.

## Design details

### `WeaponDef` base

Move to the base (from `MeleeWeaponDef`): `Name`, `Scale`, `FrameCenter`, `CarryRegion`, `CarryAnchor`, `CarryHandleOffset`, `Texture`, `Sheet` (with the region-cache reset on set), the `ApplyWeaponFacing`/`ComputeHandleScreenPoint`/`WeaponFacingEffect` statics, and the carry-region cache. Provide overridable members:

```csharp
public virtual bool HasHandleOffsets => CarryHandleOffset != Vector2.Zero;
public virtual Texture2DRegion? ResolveRegion(bool isAttacking, int frameIndex);      // default: CarryRegion
public virtual (Vector2 anchor, int frame) ResolveAnchorAndFrame(bool isAttacking, int frameIndex); // default: (CarryAnchor, 0)
public virtual Vector2 ResolveHandleOffset(bool isAttacking, int frameIndex) => CarryHandleOffset;
```

`MeleeWeaponDef` keeps swing-only members (`SwingMove`, `SwingPrefix`, `SwingAnchors`, `SwingHandleOffsets`), `ResolveSwingFrame`, and overrides the three resolvers with the existing swing logic plus `HasHandleOffsets` including `SwingHandleOffsets`. `ThrowableWeaponDef` uses the base carry defaults, so the held knife renders with zero new overlay code.

`CombatActorBase.RenderWeaponOverlay`/`DrawDebug` switch to the virtual members (drop the `MeleeWeaponDef.` prefixes). Keep the existing Debug.Assert that the drawn region half-size equals `FrameCenter`; the knife def sets `FrameCenter = region size / 2`.

### `ThrowableWeaponDef`

- `ThrowMove`: `AnimationKey = PlayerSprite.AnimationAttack1`, `AttackSfx = SfxId.AttackSwing1`, empty `FrameHitboxes` (the player lands no hit; the projectile does).
- `SpawnFrame = 1`; `ProjectileHitbox` (Offset ~zero, Size ~18×10); `Damage`, `Knockdown`, `Strength`, `ImpactSfx`.
- `ProjectileRegion` (atlas region, e.g. the single placeholder region), `ProjectileScale`, `Speed` (~650), `MaxRange` (~550), `SpinDegreesPerSecond` (~720), `SpawnOffset` (actor units, mirrored by facing; ~(20, -4)).
- `ResolveProjectileRegion()` caches `Sheet.TextureAtlas[ProjectileRegion]`.

### `ProjectileEntity`

- `Entity(name, Vector2.Zero, 16, 16)` fixed frame (collision uses `ProjectileHitbox`, not `Frame`).
- `IHitboxProvider` with `CurrentMove = null` and injected `HitboxService` (no new interface; the projectile is a hitbox owner).
- `Launch` resets `Expired`, stores def/origin/facing/faction, sets `Position = origin`.
- `Update`: no-op when `Expired`; advance position by `Direction × Speed`; advance spin; `HitboxService.Clear(this)` then `RegisterHitbox(this, Faction, def.ProjectileHitbox, def.Damage, def.Knockdown, def.Strength, def.ImpactSfx, Direction)`; set `Expired` when `Vector2.Distance(origin, Position) >= MaxRange`.
- `Render`: early-return when `Expired` (avoids a one-frame ghost while destruction is pending); draw `def.ResolveProjectileRegion()` centered at `Position`, rotated by the spin, face-flipped for left.
- `DrawDebug`: frame rectangle + region marker; no collision layer is registered.

### `ProjectileService`

- Lazy pool: `Stack<ProjectileEntity> _free`; `Spawn` pops or uses the injected `Func<ProjectileEntity>` factory; `Launch`, `Register`, add to `List<ProjectileEntity> _active`.
- `Update` (called by `GameLoop` after hit resolution): despawn every `Expired` active projectile in reverse.
- `Despawn`: `HitboxService.Clear` + `ClearAttackDedup`, `EntityService.Destroy`, remove from `_active`, push to `_free`.
- Ordering invariant to document in a comment: spawns originate from input (after `ProcessPending`), so a projectile returned last frame is already deregistered before the next rent.
- `Clear()` for level reset (also re-created by `ReinitLevel`).

### `HitboxService` / `DamageInfo`

- Add `RegisterHitbox(IHitboxProvider owner, Faction ownerFaction, HitboxData hitbox, int damage, bool knockdown, AttackStrength strength, SfxId? impactSfx, FacingDirection facing)`; make `RegisterFrameHitboxes` loop and call it. Existing melee paths unchanged.
- `ResolveHits` sets `Source = active.Owner` on each result.

### Throw flow in `PlayerEntity` / `GameLoop`

- `PlayerEntity`: `HeldThrowable => EquippedWeapon as ThrowableWeaponDef`; `PrimaryAttack()` (throw when held, else `Attack1Move`); `Thrown` event; `_pendingThrowable` set in `Throw`, cleared on attacking exit and `Reset`; `OnFrameAdvanced` override fires the event once when `CurrentMove == _pendingThrowable.ThrowMove && frameIndex >= SpawnFrame`, then unequips. Keep the `CurrentMove` guard so a stale `_pendingThrowable` can never fire on a later punch.
- `CombatActorBase`: add `protected virtual void OnFrameAdvanced(int frameIndex) { }`, called in `AdvanceFrameAndRegisterHitboxes` before hitbox registration.
- `GameLoop`: field `_projectileService`; `[Action1]` handler → `_player.PrimaryAttack()`; subscribe `_player.Thrown` in `LoadContent`; create the service in `ReinitLevel` (`new(_entityManager, _hitboxService, _entityFactory.CreateProjectile)`); call `_projectileService.Update()` after damage application; in the damage loop call `MarkHit()` when `hit.Source is ProjectileEntity`. `ResetGame` reassigns the service via `ReinitLevel` (old instances fall away with the old `EntityService`).

## Ordered Task List

1. **Art/Content**: run `python3 Utils/placeholder_gen.py knife MonoGameLearning.Game/Content/images 1 12 40`; add the three build entries (`images/knife.json`, `images/knife-texture.png`, `images/knife-pickup.png`) to `Content.mgcb`, mirroring the bat block. Verify the atlas frame key is `knife-00`.
2. **Core — `WeaponDef`**: add the base class; port the shared fields/statics/region cache from `MeleeWeaponDef`; make `MeleeWeaponDef : WeaponDef` and keep only swing members + resolver overrides.
3. **Core — `IWeaponWielder`/`CombatActorBase`**: change the interface and `EquippedWeapon`/`EquipWeapon` to `WeaponDef`; update the overlay and debug draw to virtual resolvers; add the `OnFrameAdvanced` hook.
4. **Core — projectile pipeline**: add `ThrowableWeaponDef`, `ProjectileEntity`, `ProjectileService`; add `HitboxService.RegisterHitbox` + `DamageInfo.Source`.
5. **Game — content wiring**: add `KnifeSprite`, `KnifeWeapon`, `LevelContent.Knife`; generalise `WeaponPickupEntity` to `WeaponDef`; add `CreateProjectile()` and the `Knife` pickup case + `knifeWeapon` ctor param in `LevelEntityFactory`; add a knife pickup (and optional drum drop) to `Level1`.
6. **Game — throw flow**: `PlayerEntity` (`HeldThrowable`, `PrimaryAttack`, `Thrown`, `OnFrameAdvanced`, reset clear); `EnemyEntity.AttackMove` melee-only type check; `GameLoop` wiring (`Action1`, subscription, service, hit-source despawn).
7. **Tests** (see below).
8. **Docs/skill**: the `ROADMAP.md` Milestone 7 `Throwable Weapons` bullet already links to this plan; add `MANUAL_TESTING.md` rows; refresh `LEARNING.md` (WeaponDef base + projectile pooling pattern) and the AGENTS.md weapon/projectile architecture note; and update `.kilo/skills/create-weapon/SKILL.md` to state melee-only scope and point throwables at this plan.
9. Run `dotnet build --warnaserror`, `dotnet test`, then the markdown linter on every changed `.md`.

## Tests (mandatory per AGENTS.md)

New `MonoGameLearning.Game.Tests/ThrowableWeaponTests.cs` + `ProjectileServiceTests.cs`, plus edits to `MeleeWeaponTests.cs`:

1. Weapon model: `ThrowableWeaponDef` reports the carry region/anchor for both idle and attacking; `MeleeWeaponDef` still resolves swing regions per frame.
2. `WeaponPickupEntity` with a throwable equips any `IWeaponWielder` (update `WeaponWielderTrackerEntity` to `EquipWeapon(WeaponDef)`).
3. `PlayerEntity.PrimaryAttack` while holding a throwable fires `AttackStart`, sets `CurrentMove` to `ThrowMove`, and does **not** swap `Attack1Move`; while unarmed it attacks `Attack1Move`.
4. Throw spawn: advancing the attack animation to `SpawnFrame` raises `Thrown` exactly once, unequips the weapon, and does not raise on earlier frames or on a later normal attack1.
5. `ProjectileEntity.Update` moves in the facing direction, face-flips left, and sets `Expired` at `MaxRange`.
6. `HitboxService`: a projectile-registered hitbox yields a hit whose `DamageInfo.Source` is the projectile; the same target is not returned twice; a player projectile does not damage the player (faction guard) but does damage an enemy.
7. `ProjectileService`: `Spawn`/`Despawn`/`Update` reuse pooled instances and clear hitbox + dedup state on return; `Active` reflects registration.
8. Boundary/deadlock: a `MaxRange`-expired projectile does not reappear in `Active` after `Update`, and re-renting after `Update` returns the same instance.
9. Type-check regressions: `EnemyEntity.EquipWeapon(MeleeWeaponDef)` still swaps `AttackMove`; a throwable equipped on an enemy falls back to the default attack (no `SwingMove` cast throw).
10. Existing suite: `MeleeWeaponTests`, `HitboxTests`, `PickupServiceTests`, `LevelDirector*` all pass after the `WeaponDef` signature change.

## Risks / Pitfalls

- **Region/FrameCenter assert**: the knife def must set `FrameCenter` to the placeholder region half-size or `RenderWeaponOverlay`'s Debug assert fires. Regenerate art with matching dimensions if it changes.
- **One-frame ghost**: the destroyed projectile stays in `_renderables` until the next `ProcessPending`; the `Expired` guard in `Render` is required.
- **Pool re-rent ordering**: relies on spawns occurring after `ProcessPending`. Document it; if a future non-input spawn is added, flush pooled returns at the top of `ProjectileService.Update`.
- **Stale throw**: guard the frame hook on `CurrentMove == ThrowMove` and clear `_pendingThrowable` on attacking exit/reset so an interrupted throw cannot fire later.
- **`MoveData.AnimationKey` is required** but unused by the projectile (it only supplies `ThrowMove` for the player animation); the projectile uses `RegisterHitbox`, not a `MoveData`, so there is no fake projectile move.
- **`IHitboxProvider.CurrentMove` is null** for projectiles — acceptable reuse of the existing interface; do not add a new hitbox-owner interface for one call site.
- **Friendly fire / props**: the faction guard only skips `CombatActorBase`; a player knife can break oil drums. Confirm that is desired during the manual pass.

## GC / Performance

- Steady state adds zero per-frame allocations: pooled projectiles, cached `Texture2DRegion`, reused `HitboxService` buffers, struct `DamageInfo`.
- Throwables add one `spriteBatch.Draw` for the held overlay plus one per live projectile per frame; `KnifeWeapon`/`KnifeSprite` are static singletons like the bat.

## Validation

1. `dotnet build --warnaserror` — zero warnings.
2. `dotnet test` — new + existing tests pass.
3. Manual playtest (tracked in `MANUAL_TESTING.md`): pick up the knife and confirm it renders in hand; Attack1 throws it visibly and spins; a thrown knife damages the first enemy (impact sound), knocks down when configured, and disappears; throwing left travels left; Attack2/Attack3 still punch; being knocked down drops the knife; throwing into an oil drum breaks it; no ghost frame after impact; equipping the bat replaces the knife and vice versa.

## Out of Scope (deferred)

Enemy-thrown projectiles; throwable destruction on impact (bottle shatter) vs despawn; projectile arcs/gravity; ground re-pickup of a thrown weapon; multiple throwable types in one level (the pool is def-agnostic, so this is a content add); real knife art; durability/timer expiry; HUD weapon icon; online/co-op implications.
