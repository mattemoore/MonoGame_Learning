# TODO

## Knockback Should Lerp Instead of Instant Position Change

Currently `actor.Position += hit.Knockback` applies an instant teleport. Knockback should smoothly push the target over a short duration (e.g., using `Vector2.Lerp` over ~100–200ms). This likely means the game needs a simple physics/velocity system on `ActorEntity` (velocity, drag, integration in `Update`), so knockback sets a velocity that decays each frame rather than directly manipulating position.

`LogicalEntity` → `SpatialEntity` represents an entity with a `Frame` (position + size) but no rendering. `ActorEntity` extends it with an `AnimatedSprite`, movement, and collision.

Questions to revisit:

- Is `SpatialEntity` the right base for non-rendering entities (trigger zones, etc.) or should there be a lighter-weight interface?
- `TestCollisionEntity` and `TestActor` extend `SpatialEntity` and bolt on `ICollisionActor` / `TakeDamage`, effectively re-implementing parts of `ActorEntity`. This suggests the split between "has a frame" and "is an actor" might not be clean.
- Consider whether the test pattern (extending `SpatialEntity` directly) indicates the hierarchy should be flatter, or whether the base classes are well-factored and the test stubs are just pragmatic.
