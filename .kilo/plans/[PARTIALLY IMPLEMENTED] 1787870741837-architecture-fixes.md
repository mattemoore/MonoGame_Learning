# Architecture Fixes Plan (phases 1-5 complete)

Goal: resolve the 17 architecture smells from the audit, phased by risk so each
phase stays behavior-preserving (432 tests green + `dotnet build --warnaserror`
clean). Dead-code and localized coupling first; large structural changes last.

Bounds: Core must never reference `MonoGameLearning.Game`. Every phase ends with
`dotnet build --warnaserror` (0 warnings) and `dotnet test` (432 + any new tests).

---

## Phase 1 — Dead-code removal (no behavior change) [COMPLETE]

1. Delete `MonoGameLearning.Game/Entities/Pickups/WeaponPickupEntity.cs:11`
   (`public MeleeWeaponDef Weapon` property) — no reader in Game or Tests.
2. Delete `MonoGameLearning.Game/Entities/Props/OilDrumBehavior.cs:30`
   (`Reset()`) — called only by `OilDrumStateTests`; if that test depends on
   Reset, rewrite the test to construct fresh behavior instances instead.
3. Delete `MonoGameLearning.Core/UI/HudLayoutConstants.cs` unused `MUGSHOT_TEXT_OFFSET`;
   replace the inline magic `6f` vertical offset in `EnemyBar.Render` with the
   surviving named constant (or a new `ENEMY_BAR_TEXT_OFFSET`) so the layout has
   one source of truth.

## Phase 2 — Low-risk localized refactors (behavior-preserving) [COMPLETE]

1. **IMoveable : ISpatial.** Change `MonoGameLearning.Core/Movement/IMoveable.cs:6`
   to `public interface IMoveable : ISpatial`. Delete the two `(ISpatial)`
   casts at `MonoGameLearning.Game/GameLoop/GameLoop.cs:208` and `:306`, including
   the `Debug.Assert(actor is ISpatial)` at 304.
2. **HitResult → DamageInfo consolidation.** In `MonoGameLearning.Core/Combat/`,
   add `IDamageable? Target` to `DamageInfo`; delete `HitResult.cs`. Change
   `HitboxService.ResolveHits` to return `IReadOnlyList<DamageInfo>` (the pooled
   buffer) populated with the target; delete the manual remap at
   `GameLoop.cs:192` and the `HitResult`→`DamageInfo` field copy.
3. **Magic layer strings.** Introduce `const string` layer-name constants (Core),
   replacing the scattered literals `"actors"` (`CombatActorBase.cs:23`),
   `"pickups"` (`PickupBase.cs:12`, `EntityService.PickupCollidables`),
   `"props"` (`PropBase.cs:17`), and the `CreateCollisionWorld` wiring in
   `GameLoop.cs:422`.
4. **BackgroundRenderer asset path.** `BackgroundRenderer.Create` hardcodes
   `"backgrounds/background1"` inside Core. Add a `string assetPath` parameter
   to `Create`, passed by `Level1.CreateBackgroundRenderer` (`Level1.cs:38`).

## Phase 3 — Duplication collapses (mechanical, larger files) [COMPLETE]

1. **Generic state controller.** Collapse
   `MonoGameLearning.Game/Entities/Player/PlayerStateController.cs` and
   `.../Enemy/EnemyStateController.cs` into one generic
   `StateMachineController<TState, TTrigger>` (shared guarded `Fire`, the
   optional-callback ctor, post-build idle entry). Keep the two enum/config
   tables. Delete the Enemy controller file; near-identical config DTOs
   collapse too. Guarded `Fire` must NOT be a silent swallow — when the trigger
   is not permitted/ignored in the current state, emit
   `Debug.WriteLine("...Ignored {trigger} in state {state}")` (project's
   diagnostic-warning convention) so future illegal triggers surface during
   development instead of crashing release builds. Note: this makes the enemy's
   `Fire` guarded where it was previously unguarded (`EnemyStateController.cs:167-170`);
   the only observable change is that an illegal trigger becomes a no-op instead
   of `InvalidOperationException` — no illegal trigger is fired today (base
   `OnAnimationCompleted` checks state first, and `CanFire` returns true for
   `.Ignore`d triggers), so behavior is identical in practice and all tests stay
   green.
2. **Texture sprite singletons.** `GoIndicatorSprite`, `BatPickupSprite`,
   `FoodPickupSprite` are token-identical static `Texture2D` singers whose
   `_loaded` is set `true` before `content.Load` (failure poisons the flag).
   Collapse into one reusable `StaticTextureAsset` helper (name+path), and set
   the flag only after a successful load.
3. **Damage-interface adapters.** In `Core/Combat/IDamageable.cs` drop `Faction`
   (only consumed by same-faction hit filtering on actors) and remove the
   duplicated explicit-interface `IDamageable`/`IDamageResponse` adapter blocks
   in `CombatActorBase.cs:80-93` and `PropBase.cs:68-79` — give `IDamageResponse`
   `OnHit`/`OnKnockdown` default no-op implementations so `PropBase` stops
   carrying dead hooks, and stop hard-coding `Faction.Neutral` on props
   (remove the `Faction` prop from `PropBase`).

## Phase 4 — Coupling & circularity breaks [COMPLETE]

1. **EnemyEntity ↔ LevelDirector.** `EnemyEntity` only reads
   `_director.CurrentWorld` (`EnemyEntity.cs:171`). Replace the `LevelDirector
   _director` field with a `Func<WorldSnapshot> _getWorld` (injected at ctor /
   pool `Reset`), deleting the entity→director edge. `EnemyPool.DefaultFactory`
   returns the enemy with `() => _director.CurrentWorld`. Direction becomes
   entity→Core only.
2. **EnemyEntity ↔ EnemyAI.** Move the idle cooldown decay
   (`AttackCooldown -= deltaSeconds` when `Target == null`, `EnemyEntity.cs:195`)
   into `EnemyAI.Update`; expose one consolidated result instead of the entity
   reading five `_ai.*` knobs each frame.
3. **Move weapon-render helpers.** Move the three
   `internal static` helpers (`ResolveWeaponAnchorAndFrame`, `ApplyWeaponFacing`,
   `WeaponFacingEffect`) from `CombatActorBase.cs:179,190,193` onto
   `MeleeWeaponDef` (or a static weapon-render helper), leaving the actor base
   with damage/hitbox/state only.
4. **Settings circularity.** Fold `ResolutionSettings` into `SettingsService`
   (single static settings owner); have `SettingsService`/`ResolutionSettings`
   path flow through `SettingsData` instead of each reading the other's static
   global. Fix the sibling bug: `LoadSettings` returns null when only audio is
   present, silently dropping persisted audio (`SettingsService.cs:43-44`) —
   always return the wrapper and default each missing section.
5. **CombatActorCallbacks + actor-phase collapse.** Replace the
   `CombatActorCallbacks` basket of 8 `required Action` delegates with a smaller
   set of overridable hooks; replace the 5 abstract bool predicates
   (`IsIncapacitated`/`IsInKnockedDownState`/`IsInHurtState`/`IsInDyingState`/
   `IsInAttackingState` at `CombatActorBase.cs:234-238`) plus 3 abstract
   `Fire*Completed` methods with a single `protected abstract ActorPhase Phase`
   (Core enum) that each subclass maps from its state enum in one switch, and a
   single `FirePhaseCompleted()` where the base already fires. Re-wire
   `PlayerEntity`/`EnemyEntity` mappings and the test doubles
    (`StubCombatActor`, `TestPlayerEntity`, `TestEnemyEntity`) to the new surface.

**Phase 4 implementation notes:**

- New Core files: `AI/AIUpdateResult.cs` (consolidated `EnemyAI` result struct),
  `Entities/Actor/ActorPhase.cs` (Core phase enum), `Settings/ResolutionSetting.cs`
  (record split out of the deleted `ResolutionSettings.cs`).
- Deleted: `Entities/Actor/CombatActorCallbacks.cs`, `Settings/ResolutionSettings.cs`.
- `EnemyAI.Update` returns `AIUpdateResult` (public `MovementDirection`/`FacingChanged`/
  `NewFacingX`/`Force` knobs removed); idle cooldown decay moved to `EnemyAI.UpdateIdle`.
  Weapon-render helpers now live on `MeleeWeaponDef`. `CombatActorBase` exposes 8
  `protected virtual` hooks plus `Phase`/`FirePhaseCompleted` (2 abstract members
  replacing the former 5 predicates + 3 `Fire*` methods).
- Validation: `dotnet build --warnaserror` clean; 447 tests pass (3 skipped);
  no `MonoGameLearning.Game` references under `MonoGameLearning.Core/`.

## Phase 5 — God-class / registry cleanup [COMPLETE]

1. **EntityService.** Remove the hidden `IHitboxProvider.HitboxService` settable
   slot property-injection (`EntityService.cs:175`); assign `HitboxService` at
   construction instead. Move the nested `RenderableYComparer` (`:51`) to a
   namespace-level type (and `HitboxService.ActiveHitbox` to a top-level
   internal record) to comply with the no-nested-classes rule.
2. **UiBase de-Entity.** Change `UiBase` (`Core/UI/UiBase.cs:7`) to stop inheriting
   `Entity`; make it a plain widget base implementing `IUpdatable` +
   `IScreenRenderable` + `IDebugDrawable`. Delete `IsScreenSpace`. Register UI
   through a dedicated screen-renderables list rather than `EntityService`
   (update `GameLoop.cs:407` and the registration path), and decouple
   `GoIndicatorEntity` from `GameCore.ViewportAdapter` by passing viewport size
   (or the camera) into it instead of reading the global.
3. **GameLoop split.** Move the per-state music mapping (`GameLoop.cs:68-96`)
   into `AudioService` (a `GameState`→track map). Give `CameraService` single
   ownership of `WaveEndX`/wave-cleared (subscribe inside its ctor), deleting
   `GameLoop`'s manual copy-back at `169-171`.
4. **LevelDirector split.** Separate the encounter sequencer (wave gating,
   scroll-lock, snapshot feeding) from content instantiation (oil drums,
   pickups, enemy equips): replace stringly-typed dispatch `"Food"`/`"Bat"`
   (`LevelDirector.cs:100-105`) and `"Grunt"` (`EnemyPool.cs:114`) with typed
   factories/consts injected, and de-duplicate the camera-edge + spawn-position
   computation shared between `SpawnWave` and `DrawDebug`.

**Phase 5 implementation notes:**

- `EntityService` now takes `HitboxService?` via its constructor (the settable
  property-injection slot is gone) and no longer routes `IScreenRenderable`s —
  UI widgets register through `GameLoop`'s dedicated `_screenRenderables` list.
  `RenderableYComparer` moved to `Core/Entities/RenderableYComparer.cs`;
  `HitboxService.ActiveHitbox` moved to `Core/Combat/ActiveHitbox.cs`.
- `UiBase` is now a plain widget base (`IUpdatable` + `IScreenRenderable` +
  `IDebugDrawable`, `Visible` + `Position`), no longer an `Entity`;
  `IsScreenSpace` deleted. `GoIndicatorEntity` takes a `Func<Point> getViewportSize`
  instead of reading `GameCore.ViewportAdapter`.
- `GameState`/`GameTrigger` enums moved to Core (`Core/GameState.cs`,
  `Core/GameTrigger.cs`) so `AudioService.OnGameStateChanged(previous, current)`
  can own the per-state music/pause mapping. `CameraService` now owns
  `WaveEndX`/wave-cleared via an injected `Func<float?>` getter, detecting the
  non-null→null transition inside `Update` (GameLoop's manual copy-back deleted).
- `LevelDirector` receives injected content factories (`createProp`,
  `createPickup`, `getWeapon`, `createEnemy`) and a `Func<RectangleF> getCameraView`;
  `EnemyPool` takes `Func<WorldSnapshot>` + a factory that receives the
  world-getter. Stringly-typed dispatch lives in `GameLoop` (composition root)
  and `LevelContent` consts; `SpawnWave`/`DrawDebug` share `GetSpawnContext()`.
- Test doubles updated (`TestLevelDirector`, `TestEnemyPool`, `DirectorStub`,
  `TestUiEntity`, `StubCombatActor` ctor paths); new tests cover
  `AudioService.OnGameStateChanged` mapping and pool world-getter injection.
- Validation: `dotnet build --warnaserror` clean; 446 tests pass (3 skipped);
  no `MonoGameLearning.Game` references under `MonoGameLearning.Core/`.

## Phase 6 — Structural (largest; later milestone)

1. **GameCore de-static (single injected context).** Remove the 7 static
   singletons and the `new GraphicsDevice`/`new Content` shadowing
   (`GameCore.cs:13-26`). Introduce one context object holding
   `GraphicsDevice`/`SpriteBatch`/`Camera`/`Content`, constructed once and passed
   by ctor to `GameLoop` and the services that need it. Phase 5 already removed
   the `GameCore.Camera`/`ViewportAdapter` reads in `LevelDirector` and
   `GoIndicatorEntity`, clearing the path. Verify no `GameCore.X` reads remain.
2. **Promote `GameStateService`** (arcade-shell FSM, `GameLoop/GameStateService.cs`)
   to Core — no game-IP, no deps change needed.
3. **Promote generic `EnemyPool`** Rent/Return/Sentinel machinery to Core,
   de-welding the game `EnemyEntity`/`EnemySprite`/`"Grunt"` factory behind an
   injected `Func<string,int,TEnemy>`.
4. **Promote `LevelDirector` encounter core** to Core, keeping game content
   instantiation (drums/pickups/weapons) behind an injected factory/delegate so
   Core never references Game.

Each promotion must keep the invariant "Core never references Game" (verify with
a grep for `MonoGameLearning.Game` under `MonoGameLearning.Core/`).

---

## Cross-cutting rules

- No new types/layers unless they replace a removed smell (prefer removing over
  adding abstraction). Exceptions already approved above: the injected context
  object, `StaticTextureAsset`, the generic state controller, `Func<WorldSnapshot>`
  (no interface needed), and the layer-name constants.
- Behavior is preserved unless a bug is being fixed explicitly (the settings
  audio-drop fix and the poisoned `_loaded` flag).
- Test tails: every change that touches `CombatActorCallbacks`, actor hooks,
  `EntityService` registration, `LevelDirector`/`EnemyPool` ctor signatures, or
  the state controllers must update the affected test doubles; add a regression
  test for the settings audio-drop fix.
- Commit per phase (or per coherent sub-task); keep `dotnet build --warnaserror`
  and `dotnet test` green at every commit.

## Validation

- After every phase: `dotnet build --warnaserror` and `dotnet test`.
- After Phase 6: `grep -rn "MonoGameLearning.Game" MonoGameLearning.Core --include=*.cs`
  returns nothing.
- Final: full suite green, zero warnings, no dead members reintroduced.

## Open decisions (finalize during implementation, keep behavior identical)

- Exact `Func<WorldSnapshot>` vs a small `IWorldSnapshotProvider` — default to the
  `Func` to avoid a new type. *(Resolved in Phase 4: `Func<WorldSnapshot>` chosen.)*
- `ActorPhase` enum member set — derive from the union of the five current
  predicates (`Idle/Moving/Attacking/Hurt/KnockedDown/Dying/Dead`), mapped once in
  each subclass. *(Resolved in Phase 4: implemented as specified.)*
