# TODO

1. When enemies spawn they can't be hit when they run in.  Shorten?
1. When Pickup health at 100% the sound effect doesn't play?
1. If enemy is at left edge of screen and player is moving right the enemy gets warped to keep enemy on-screen
1. Simplify Go entity to flash using animation instead of all code
1. Per frame hurtboxes?
1. Global game speed setting so we can do slowmotion and stuff

## Drop System Follow-ups

1. Strongly type `PropBase.Destroyed` as `Action<PropBase>` and change `LevelDirector.OnPropDestroyed(PropBase)`. Update `StubPropDropperEntity` (and any other Entity-based test stubs that simulate prop destruction) to extend `PropBase` via a sprite-less constructor overload on `PropBase`.
2. Rename `PickupSpawnDef` → `DropDef` (or `ItemSpawnDef`) when weapon drops land. Update `Level.Pickups`, `PropSpawnDef.Drops`, `LevelDirector.SpawnPickups` switch, and all test fixtures.
3. Wire enemy drops: in `LevelDirector.SpawnWave`, set `enemy.Drops` per `EnemySpawnDef` (add a `Drops` field) and call `CreateDrops()` from `OnEnemyDied`, forwarding through `SpawnPickups`. Extend `SpawnPickups`'s switch to include weapon types (`"Knife"`, `"Bat"`, etc.) when those entities exist.
4. Investigate whether `Level.Pickups` (level-start pickups) should merge into a generic level-item-spawn list now that prop drops share the same DTO shape — eliminates the parallel `Level.Pickups` and `LevelDirector.SpawnPickups(IReadOnlyList<PickupSpawnDef>)` initial-spawn path.
