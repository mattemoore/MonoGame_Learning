# Plan: Actor pass-through vs. solid prop walls via MonoGame.Extended collision layers

## Goal

Configure MonoGame.Extended's `CollisionComponent` so that:

- Player and enemy actors **pass through each other** (no collision response between any actor-actor pair).
- Prop entities behave as **immovable solid walls** that block both players and enemies.

The solution must use the framework's collision system (layers), **not** manual intersection methods.

## Verified framework facts (MonoGame.Extended 5.3.1)

- No physics engine (Aether.Physics/Farseer) is referenced. `MonoGame.Extended.ECS` namespace exists but is an unrelated entity-component-system not used by this project. The correct tool is `CollisionComponent` **layers**.
- From `CollisionComponent` source:
  - `DEFAULT_LAYER_NAME = "default"`. The default layer collides with itself and all other layers.
  - `Add(name, layer)` for a **non-default** layer registers only the pair `(default, newLayer)` via `AddCollisionBetweenLayer` — it never adds `(newLayer, newLayer)`.
  - Therefore a non-default layer **collides only with the default layer**; actors on the same non-default layer never collide with each other.
- `ICollisionActor.LayerName` is a **default interface member** (`string LayerName { get => null; }`). That is why every entity currently lands on the default layer and collides with everything.
- `CollisionComponent.Update` notifies **both** actors for each colliding pair (symmetric), so a single ordered `(default, actors)` pair is sufficient.

## Decisions (confirmed)

1. All actors (player + enemies) pass through each other. Achieved by placing all actors on one non-default `"actors"` layer (no self-collision).
2. Props are immovable: `PropEntity.OnCollision` becomes a no-op; the actor absorbs the full penetration vector and is fully blocked. The prop never moves.

## Files to change

### `MonoGameLearning.Core/Entities/ActorEntity.cs`

- Override the `ICollisionActor.LayerName` property to route actors off the default layer:

  ```csharp
  public string LayerName => "actors";
  ```

  This covers `PlayerEntity` and `EnemyEntity` (both inherit `ActorEntity`). No change to `OnCollision` (still `Position -= collisionInfo.PenetrationVector`); it will now only be invoked for prop collisions.

### `MonoGameLearning.Core/Entities/PropEntity.cs`

- Make props immovable walls. Change `OnCollision` to a no-op:

  ```csharp
  public virtual void OnCollision(CollisionEventArgs collisionInfo) { }
  ```

  (Props keep `LayerName` as the default `null`, so they remain on the default layer and collide with the `"actors"` layer via the registered `(default, actors)` pair.)

### `MonoGameLearning.Game/GameLoop/GameLoop.cs`

- Add usings:

  ```csharp
  using MonoGame.Extended.Collisions.Layers;
  using MonoGame.Extended.Collisions.QuadTree;
  ```

- Extract the collision bounds so both layers share the same space, and add the actors layer wherever a `CollisionComponent` is constructed:
  - In `Initialize` (after `_collision = new CollisionComponent(...)`):

    ```csharp
    _collision.Add("actors", new Layer(new QuadTreeSpace(new RectangleF(0, 0, GAME_WIDTH * 2, GAME_HEIGHT))));
    ```

  - In `ResetGame` (same, after the new `CollisionComponent` is created there).
- No other insertion changes needed: existing `_collision.Insert(entity)` / `Insert(enemy)` / `Insert(prop)` calls route automatically via `LayerName` (actors → `"actors"`, props → default).

## Tests to add

New file `MonoGameLearning.Game.Tests/CollisionLayerTests.cs` (NUnit, mirroring `ActorCollisionTests` style). Use `CollisionComponent` end-to-end (no manual intersection), covering the critical failure modes:

1. `ActorActor_SameLayer_PassThrough` — two `TestActorEntity` on the `"actors"` layer, overlapping, after `_collision.Update` both positions are **unchanged** (no push-apart).
2. `ActorProp_ActorPushedOutOfProp` — actor on `"actors"`, prop on default, overlapping; after update the actor is moved out of the prop and the prop position is **unchanged**.
3. `EnemyProp_EnemyBlockedByProp` — `EnemyEntity`-equivalent (or `TestActorEntity`) vs prop: same as #2 for an enemy.
4. `Prop_Prop_NoMovement` — two props overlapping on default: both remain in place (props immovable), guarding against prop-prop shoving.
5. `ActorProp_ActorFullySeparated` — assert actor frame no longer intersects prop frame after update (blocks, doesn't tunnel).

Notes for tests:

- Construct `CollisionComponent` with a `RectangleF` bounds large enough for the fixtures, then `_collision.Add("actors", new Layer(new QuadTreeSpace(bounds)))`.
- Use the existing `TestActorEntity` helper in `ActorCollisionTests.cs` (or a shared test prop) — keep test entities' `LayerName` defaulting via `ActorEntity` override.
- Tests must reflect the new behavior; existing `ActorCollisionTests` still pass because they call `OnCollision` directly (method unchanged).

## Validation checklist (mandatory, in order)

1. `dotnet build` — compiles.
2. `dotnet test` — all existing tests + new `CollisionLayerTests` pass, no regressions.
3. Manual sanity (optional): run game; player and enemies overlap freely; player/enemy cannot walk through oil drums.

## Risks / notes

- `ResetGame` rebuilds `_collision`, so the `"actors"` layer MUST be re-added there or post-reset actors throw `UndefinedLayerException` on `Insert`.
- `LayerName` is a default interface member; overriding it on `ActorEntity` is the only routing change — no interface re-implementation needed elsewhere.
- Existing `ActorCollisionTests` test the push-apart math directly; they remain valid (method behavior unchanged) even though actor-actor push-apart is no longer triggered by the framework.
- Props colliding with props is now a no-op (immovable), so initial prop placement overlap is harmless.
