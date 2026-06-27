# MonoGame.Extended 5.3.1 → 6.0.0 Upgrade Plan

## Goal
Bump `MonoGame.Extended` and `MonoGame.Extended.Content.Pipeline` from `5.3.1` to `6.0.0` across the solution, with a full migration of the collision subsystem onto v6's new shape-based API. AnimatedSprite, ViewportAdapters, and Input APIs stay (v6 still ships them); only imports/usage need updating.

## Scope / Non-Goals
- **In scope:** package version bumps, all `using` updates, migration of collision subsystem (`CollisionComponent` / `IShapeF` / `CollisionEventArgs` / `Layer` / `QuadTreeSpace` → `CollisionWorld2D` + `CollisionShape2D` + named layers + `CollisionEventArgs2D`-style result), test suite updates, `dotnet build` + `dotnet test` green.
- **Out of scope:** Tilemap migration (we don't use Tiled; no `.tmx`/tilemap references in the codebase). ECS upgrade (we don't use the ECS). AOT/trimming work. Renderer/drawing API changes that don't touch our call sites.

## Key Facts About v6.0.0
- Released 2026-05-02. Targets `net8.0`; forward-compatible with our `net10.0` TFM.
- Removes: `MonoGame.Extended.Collisions.CollisionComponent`, `MonoGame.Extended.Collisions.Layers.Layer`, `MonoGame.Extended.Collisions.QuadTree.QuadTreeSpace`, the `IShapeF`-driven `ICollisionActor.OnCollision(CollisionEventArgs)` flow.
- Replaces with: `CollisionWorld2D` (named layers, broadphase queries, narrowphase `CollisionResult2D` / `TryGetCollision`), per-actor `CollisionShape2D` (built from the new `BoundingBox2D` / `BoundingCircle2D` / etc.). Legacy `RectangleF` / `CircleF` / `IShapeF` types and `MonoGame.Extended.Graphics` (AnimatedSprite, SpriteBatch draw helpers, ViewportAdapters, Input) remain available.
- ECS `BitVector32` → `ComponentBits` (not used here).
- Tilemap rewrite is in a separate feature doc — irrelevant to us (no Tiled assets).

## Affected Files

### Package files (version bumps)
- `MonoGameLearning.Core/MonoGameLearning.Core.csproj` — bump `MonoGame.Extended` 5.3.1 → 6.0.0.
- `MonoGameLearning.Game/MonoGameLearning.Game.csproj` — bump `MonoGame.Extended` 5.3.1 → 6.0.0 and `MonoGame.Extended.Content.Pipeline` 5.3.1 → 6.0.0.
- `MonoGameLearning.Game/Content/Content.mgcb` — verify `/reference:../pipeline-references/MonoGame.Extended.Content.Pipeline.dll` still resolves (path-based local reference; the DLL name should be unchanged).

### Core (collision subsystem rewrite)
- `MonoGameLearning.Core/Entities/EntityManager.cs` — replace `CollisionComponent`-based insert/remove with `CollisionWorld2D.AddActor` / `RemoveActor`. Wrap world in `EntityManager(CollisionWorld2D world, ...)` via primary constructor.
- `MonoGameLearning.Core/Entities/Entity.cs` — no collision-specific changes; `RectangleF Frame` from `MonoGame.Extended` is preserved in v6 (geometry types live alongside new `BoundingBox2D`).
- `MonoGameLearning.Core/Entities/CombatActorBase.cs` — drop `IShapeF Bounds`, drop `OnCollision(CollisionEventArgs)`. Implement new collision interface (`ICollisionActor2D` or equivalent from `MonoGame.Extended.Collisions`) supplying a `CollisionShape2D`. Replace `Position -= penetration` with reading `CollisionResult2D` from the world's per-frame event/callback (collisions are dispatched by the world rather than pushed via a penetration vector).
- `MonoGameLearning.Core/Entities/PropBase.cs` — same: drop `IShapeF Bounds`, drop `OnCollision(CollisionEventArgs)`; expose `CollisionShape2D` for the world's broadphase.
- `MonoGameLearning.Core/Entities/TriggerEntity.cs` — same pattern; trigger callbacks will need a new shape (likely subscribe to world collision events filtered by trigger's actor).
- `MonoGameLearning.Core/Combat/HitboxService.cs` — keep `RectangleF` (still available); no collision API dependency. May need using-directive touch-ups only.

### Game
- `MonoGameLearning.Game/GameLoop/GameLoop.cs` — replace `CreateCollisionComponent(RectangleF bounds)` with a factory that constructs `CollisionWorld2D`, configures the "actors" and "props" named layers with their broadphase (`SpatialHash` or `QuadTreeSpace` v6 equivalent — confirm in docs at implementation time; v6 still exposes both behind `CollisionWorld2D`'s broadphase contract), and exposes a per-frame `Update(GameTime)` that drives `world.Update(...)`.
- `MonoGameLearning.Game/Levels/Level.cs` — unchanged contract; `MovementBounds` is still `RectangleF`.
- `MonoGameLearning.Game/Levels/LevelDirector.cs` — unchanged contract.

### Animations / Graphics / Input (import-only)
These keep working; only `using` lines need adjustment if v6 moved namespaces:
- `MonoGameLearning.Core/Entities/CombatActorBase.cs` — `MonoGame.Extended.Animations` still exists (verify `IAnimationController` / `AnimationEventTrigger` unchanged in v6 release notes; if moved, update using statements).
- `MonoGameLearning.Core/Input/InputManager.cs` — `MonoGame.Extended.Input` unchanged in v6 surface.
- `MonoGameLearning.Core/GameCore/GameCore.cs` — `MonoGame.Extended.ViewportAdapters` unchanged.
- `MonoGameLearning.Core/Entities/HealthDisplay.cs`, `Entity.cs`, `RenderContext.cs`, `TriggerEntity.cs`, `CombatActorBase.cs` — `using MonoGame.Extended;` for `RectangleF`/`Vector2`/etc. remains valid (or split to `MonoGame.Extended.Primitives` if v6 reorganized).
- `MonoGameLearning.Game/AnimatedSprites/PlayerSprite.cs`, `EnemySprite.cs`, `OilDrumSprite.cs` — `MonoGame.Extended.Graphics` for `AnimatedSprite`; rename namespaces if needed.
- `MonoGameLearning.Game/Rendering/BackgroundRenderer.cs` — `RectangleF` + `SpriteBatch` draw helpers; using-statement touch-ups.
- `MonoGameLearning.Game/GameLoop/CameraController.cs` — `RectangleF`; touch-ups.

### Tests
- `MonoGameLearning.Game.Tests/CollisionLayerTests.cs` — rewrite to use `CollisionWorld2D`. Reproduce the same four scenarios (actor-actor pass-through, actor-prop push-out, enemy-prop block, prop-prop no-movement) plus the fully-separated assertion. Use named layers ("actors", "props"). Drop `Layer`/`QuadTreeSpace` setup; rely on world's narrowphase `TryGetCollision` results.
- `MonoGameLearning.Game.Tests/ActorCollisionTests.cs` — convert from unit tests that synthesize `CollisionEventArgs` into unit tests that drive the actor's collision response against a real `CollisionShape2D` result (or, if the per-actor handler now takes a `CollisionResult2D`, test that handler directly). Keep `ClampToBounds` tests verbatim (no collision API change).
- `MonoGameLearning.Game.Tests/EnemyEntityTests.cs`, `OilDrumStateTests.cs`, `HitboxTests.cs`, `LevelDirectorTests.cs` — drop `IShapeF Bounds` / `OnCollision(CollisionEventArgs)` from test actors/props; add the new shape/handler members. `LevelDirectorTests.cs` swaps `CollisionComponent` setup for `CollisionWorld2D`.
- Add new tests: `CollisionWorld2D_LayerFiltering_DoesNotPropagateBetweenLayers`, `CollisionShape2D_Box_ProducesExpectedMTV` (use the new `BoundingBox2D.Intersects` / collision result normal). Cover the regression-prone failure modes called out in AGENTS.md (collision failures, out-of-bounds, state-machine interruptions triggered by collision).

## Tasks (Ordered)

1. **Version bumps.** Update both `.csproj` files; rebuild with `dotnet restore && dotnet build` to surface every compile error from the collision overhaul in one pass.
2. **Fix imports namespace-wide.** Sweep every file listed in the "Affected Files" import-only section so the project compiles minus the collision API changes.
3. **Define the collision abstraction in Core.** Introduce (or update) `ICollisionActor` in `MonoGameLearning.Core/Entities/Interfaces/` to expose a `CollisionShape2D Shape { get; }` and a collision callback that takes the v6 result (e.g., `OnCollision(CollisionResult2D result)` or whatever the v6 callback signature is — confirm during implementation). Keep the callback decoupled from penetration-vector mutation; position update happens inside the world loop.
4. **Rewrite `EntityManager`.** Take `CollisionWorld2D` instead of `CollisionComponent`. On `Register`/`Destroy`, call `world.AddActor` / `world.RemoveActor` only for entities implementing the new `ICollisionActor`.
5. **Migrate `CombatActorBase`, `PropBase`, `TriggerEntity`.** Implement new `ICollisionActor` (returning `new BoundingBox2D(...)` built from `Frame`). Remove `OnCollision(CollisionEventArgs)` body. Subscribe to the world's collision event in `OnAddedToWorld` and unsubscribe on `OnRemovedFromWorld` (use whatever lifecycle hook v6 exposes — confirm in docs).
6. **Rewrite `GameLoop.CreateCollisionComponent`.** Replace with `CreateCollisionWorld(RectangleF bounds)` returning a configured `CollisionWorld2D` with named layers for actors and props and chosen broadphase. Drive `world.Update(gameTime)` each frame.
7. **Update `GameLoop.Update`** to invoke the new collision world instead of `cc.Update(gameTime)`.
8. **Rewrite collision tests** (`CollisionLayerTests`, `ActorCollisionTests`, `LevelDirectorTests`) against the new world. Keep assertion intent identical where possible (positions, MTV direction, prop immobility).
9. **Update test fakes** (`TestProp`, `PassThroughActor`, `TestActorEntity`, `CollisionPushEntity`) to the new `ICollisionActor` shape.
10. **Add new shape/layer tests** as listed above.
11. **Run `dotnet build`** — fix any remaining v6 namespace/member moves.
12. **Run `dotnet test`** — fix all regressions.
13. **Update `ROADMAP.md` / `TODO.md`** to note the v6 upgrade and any deferred follow-ups (e.g., AOT trimming of content readers, which v6 enables).

## Risks
- **v6 collision API shape not fully documented in blog.** The blog describes the world/layer/query model but does not enumerate exact `ICollisionActor` member names or the world-update callback signature. Implementation agent must read the v6.0.0 source (or `MonoGame.Extended.xml` shipped with the NuGet) for the precise surface before coding.
- **`RectangleF` / `Vector2` namespace may have moved.** Confirm by inspecting the v6 package contents once restored; if `MonoGame.Extended` is now an umbrella that requires `MonoGame.Extended.Primitives` etc., sweep usings accordingly.
- **CombatActorBase subscribe-to-`Sprite.Controller.OnAnimationEvent` pattern still relies on `SetAnimation()` replacing the controller.** AGENTS.md calls this out; unchanged by v6. Do not regress the pattern during the import sweep.
- **No Tiled assets in the project**, so the removed `MonoGame.Extended.Tiled` namespace does not affect us. Confirm no `.mgcb` references to Tiled importers.
- **Content pipeline DLL name unchanged** in v6 (`MonoGame.Extended.Content.Pipeline.dll`); `Content.mgcb` reference should resolve.

## Validation
1. `dotnet build` → zero errors, zero new warnings in our code.
2. `dotnet test` → all tests pass, including the four rewritten collision-layer scenarios and any new shape tests.
3. Manual smoke (optional, out of plan scope): `dotnet run --project MonoGameLearning.Game` to confirm sprite/animation/movement still behave end-to-end.

## Open Questions
None blocking. Two implementation-time confirmations needed (resolve during implementation by reading the v6 NuGet XML doc):
- Exact `ICollisionActor2D` (or equivalent) member names in `MonoGame.Extended.Collisions` v6.
- Exact `CollisionWorld2D.Update` / `AddActor` / collision-event-callback signature in v6.