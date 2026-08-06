# TODO

1. When enemies spawn they can't be hit when they run in.  Shorten?
1. When Pickup health at 100% the sound effect doesn't play?
1. If enemy is at left edge of screen and player is moving right the enemy gets warped to keep enemy on-screen
1. Simplify Go entity to flash using animation instead of all code
1. Per frame hurtboxes?
1. Global game speed setting so we can do slowmotion and stuff

## Drop System Follow-ups

1. Rename `PickupSpawnDef` → `DropDef` (or `ItemSpawnDef`) when weapon drops land. Update `Level.Pickups`, `PropSpawnDef.Drops`, `LevelDirector.SpawnPickups` switch, and all test fixtures.
