# TODO

`HitboxService._resolvedThisFrame` prevents the same hitbox definition from hitting the same target twice within a single animation frame, but multi-frame attacks (e.g., Attack1 with hitboxes on frames 1 and 2) still land separate hits per frame. `OilDrumEntity` uses a 0.3s `_hitCooldown` in `TakeDamage` to prevent double-dipping within one attack.  Consider adding a state controller like we do in `PlayerEntity` to stop double hitting so that it is consistent and we can implement double hit attacks in the future as opposed to handling it in `TakeDamage`

- Is `SpatialEntity` the right base for non-rendering entities (trigger zones, etc.) or should there be a lighter-weight interface?
  - `TestCollisionEntity` and `TestActor` extend `SpatialEntity` and bolt on `ICollisionActor` / `TakeDamage`, effectively re-implementing parts of `ActorEntity`. This suggests the split between "has a frame" and "is an actor" might not be clean.
  - Consider whether the test pattern (extending `SpatialEntity` directly) indicates the hierarchy should be flatter, or whether the base classes are well-factored and the test stubs are just pragmatic.

- `OilDrumEntity` extends `ActorEntity` but is a simple prop with no state machine, no movement, and no combat logic. Review whether a lighter base (e.g., directly extending `SpatialEntity` with `ICollisionActor` and `IDamageable`) would be more appropriate for non-combatant breakable props.

- `ActorEntity.TakeDamage` is a `virtual` no-op that is never called by `PlayerEntity.TakeDamage` or `OilDrumEntity.TakeDamage` (neither calls `base.TakeDamage()`). Decide whether: (a) the base should be `abstract` instead, (b) overrides should call `base.TakeDamage()` for shared logic, or (c) the current pattern is fine.
