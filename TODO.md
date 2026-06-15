# TODO
- Is `SpatialEntity` the right base for non-rendering entities (trigger zones, etc.) or should there be a lighter-weight interface?
  - `TestCollisionEntity` and `TestActor` extend `SpatialEntity` and bolt on `ICollisionActor` / `TakeDamage`, effectively re-implementing parts of `ActorEntity`. This suggests the split between "has a frame" and "is an actor" might not be clean.
  - Consider whether the test pattern (extending `SpatialEntity` directly) indicates the hierarchy should be flatter, or whether the base classes are well-factored and the test stubs are just pragmatic.

- `OilDrumEntity` extends `ActorEntity` but is a simple prop with no state machine, no movement, and no combat logic. Review whether a lighter base (e.g., directly extending `SpatialEntity` with `ICollisionActor` and `IDamageable`) would be more appropriate for non-combatant breakable props.

- `ActorEntity.TakeDamage` is a `virtual` no-op that is never called by `PlayerEntity.TakeDamage` or `OilDrumEntity.TakeDamage` (neither calls `base.TakeDamage()`). Decide whether: (a) the base should be `abstract` instead, (b) overrides should call `base.TakeDamage()` for shared logic, or (c) the current pattern is fine.
