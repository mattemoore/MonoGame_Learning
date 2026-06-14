# TODO

`LogicalEntity` → `SpatialEntity` represents an entity with a `Frame` (position + size) but no rendering. `ActorEntity` extends it with an `AnimatedSprite`, movement, and collision.

Questions to revisit:

- Is `SpatialEntity` the right base for non-rendering entities (trigger zones, etc.) or should there be a lighter-weight interface?
- `TestCollisionEntity` and `TestActor` extend `SpatialEntity` and bolt on `ICollisionActor` / `TakeDamage`, effectively re-implementing parts of `ActorEntity`. This suggests the split between "has a frame" and "is an actor" might not be clean.
- Consider whether the test pattern (extending `SpatialEntity` directly) indicates the hierarchy should be flatter, or whether the base classes are well-factored and the test stubs are just pragmatic.
