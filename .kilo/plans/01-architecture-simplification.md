# Architecture Simplification Plan

## Finding 1: `EnemyEntity._director` nullable with silent null guard

**Location:** `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs:48,96,149`
**Category:** Leaky Abstraction / Hidden Precondition

### Problem — Nullable director creates a silent dead path

`EnemyEntity` constructor accepts `LevelDirector director = null`. In production, a director is always provided. The null guard in `Update()` (`if (_director is null) return;`) causes the enemy to silently skip all AI, movement, and world queries — producing a non-functional entity with no diagnostic feedback. The null path only exists to accommodate `TestEnemyEntity` which passes `null!` directly to the constructor.

### Impact — Hidden precondition and misleading API

- Hidden precondition: enemies without a director compile but don't work
- The nullable parameter signals "optional" when it's actually required in production
- `TestEnemyEntity` passes `null!` for both `sprite` and `director`, and its callers in test code (`EnemyPoolTests.MockFactory`, `TestEnemyPool`) never provide a director either

### Suggestion — Make director required

Make `LevelDirector` a required (non-nullable) constructor parameter. Remove the null guard in `Update()` and `DrawDebug()`. Update `TestEnemyEntity` to accept an optional `LevelDirector` via its constructor so callers can pass a test `LevelDirector` stub. In `DefaultFactory` the director is always provided, so production code is unaffected.

### Files to change — Finding 1

- `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs` — make `director` required (non-nullable), remove `= null`, remove null guards in `Update()` and `DrawDebug()`
- `MonoGameLearning.Game.Tests/TestEnemyEntity.cs` — add `LevelDirector director = null` parameter, pass it to base
- `MonoGameLearning.Game.Tests/EnemyPoolTests.cs` — `DirectorStub` exists for these tests; pass it through `TestEnemyEntity`
- `MonoGameLearning.Game.Tests/LevelDirectorTests.cs` — `TestEnemyEntity` used for "outside wave" test; provide director stub there too

---

## Finding 2: `PropBase` implements `IDamageable` with non-functional `Died` event

**Location:** `MonoGameLearning.Core/Entities/PropBase.cs:22-25`
**Category:** Leaky Abstraction — Interface contract bending

### Problem — Props carry an interface contract they can't fulfill

`PropBase` implements `IDamageable`, which requires `event EventHandler Died`. Props never raise `Died` (they use `Destroyed` instead), so the event is suppressed with `#pragma warning disable CS0067`. `Faction => Faction.Neutral` is also hardcoded — every prop is always neutral. The interface contract is broader than what `PropBase` actually fulfills.

### Impact — Silent correctness trap

- The `CS0067` suppression is a signal that the interface doesn't fit the type
- Any code that subscribes to `Died` on a prop silently never fires — a correctness trap
- With future prop types planned, every prop will carry a dead event field

### Suggestion — Split IDamageable into two interfaces

Split `IDamageable` into two interfaces:

- `IDamageRecipient` — `TakeDamage`, `CanTakeDamage`, `ReduceHealth`, `Health`/`MaxHealth`/`IsAlive`, `Faction`, `OnDeath`/`OnKnockdown`/`OnHit`
- `IDamageNotifier` — `event EventHandler Died`

`CombatActorBase` implements both. `PropBase` implements only `IDamageRecipient`. This removes the CS0067 suppression and makes the prop contract honest.

### Files to change — Finding 2

- `MonoGameLearning.Core/Entities/Interfaces/IDamageable.cs` — split into `IDamageRecipient` (damage/methods) + `IDamageNotifier` (``Died`` event)
- `MonoGameLearning.Core/Entities/PropBase.cs` — implement `IDamageRecipient` only, remove `#pragma warning disable CS0067` and ``Died`` event
- `MonoGameLearning.Core/Entities/CombatActorBase.cs` — implement both `IDamageRecipient` + `IDamageNotifier`
- `MonoGameLearning.Core/Entities/EntityManager.cs` — update `_damageables` and `_combatants` lists to track `IDamageRecipient` instead of `IDamageable`
- `MonoGameLearning.Game/GameLoop/GameLoop.cs` — update `is IDamageable` checks to `is IDamageRecipient`
- `MonoGameLearning.Game.Tests/HitboxTests.cs` — update `TestSpatialEntity` and `TestPropForHit` to implement `IDamageRecipient` + optionally `IDamageNotifier`
- `MonoGameLearning.Game.Tests/LevelDirectorTests.cs` — verify `OilDrumEntity` compatibility (if used in prop path)

---

## Finding 3: Oil drum prop lifecycle managed in `GameLoop`

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:274-286`
**Category:** Excessive Coupling / Feature Envy

### Problem — Prop lifecycle lives in the wrong layer

`GameLoop` directly manages oil drum creation (`RegisterOilDrum`), destruction (`OnOilDrumDestroyed`), and lifecycle wiring. This is prop lifecycle logic living in the top-level game loop, while enemy lifecycle was recently extracted into `LevelDirector` + `EnemyPool`. As more level and prop types are added, every new prop requires editing `GameLoop`.

### Impact — Asymmetric lifecycle management

- Asymmetry: enemies flow through `LevelDirector`, props flow through `GameLoop`
- Adding a new prop type requires modifying the game loop
- `ReinitLevel` iterates props inline instead of delegating to a level system

### Suggestion — Move prop lifecycle into LevelDirector

Move prop lifecycle into `LevelDirector`, following the same pattern as enemy management. Add a prop pool (or reuse the `Build`/`Rent`/`Return` pattern) so `LevelDirector` owns prop spawning, registration, and cleanup. `GameLoop.ReinitLevel` passes prop defs to the director instead of managing them directly.

### Files to change — Finding 3

- `MonoGameLearning.Game/Levels/LevelDirector.cs` — add prop lifecycle methods (e.g., `SpawnProps`, `OnPropDestroyed`), prop pool or tracking list
- `MonoGameLearning.Game/GameLoop/GameLoop.cs` — remove `RegisterOilDrum()` and `OnOilDrumDestroyed()`, replace `foreach (var prop in _currentLevel.Props) RegisterOilDrum(prop)` with `_levelDirector.SpawnProps(_currentLevel.Props)`
- `MonoGameLearning.Game/Levels/Level.cs` — `Props` remains the level data source
- `MonoGameLearning.Game/Levels/PropSpawnDef.cs` — no change needed, already a generic record

---

## Finding 4: Oil-drum-specific logic lives in `MonoGameLearning.Core.Combat`

**Location:** `MonoGameLearning.Core/Combat/OilDrumBehavior.cs`, `OilDrumDamage.cs`
**Category:** Leaky Abstraction — Game-specific logic in library layer

### Problem — Game-specific code leaks into the core library

`OilDrumBehavior` (stun state machine) and `OilDrumDamage` (strength-to-damage mapping) are in `MonoGameLearning.Core.Combat` but are only used by `OilDrumEntity` in the Game project. They are game-specific prop implementations leaking into the core library. With many level and prop types planned, each new prop type would add its behavior/damage classes to Core, blurring the separation boundary.

### Impact — Core accumulates game-specific logic

- Core accumulates game-specific logic, violating the library/game separation documented in AGENTS.md
- Every new prop type pollutes Core.Combat with single-use classes
- Test files for these classes (`OilDrumStateTests.cs`) reference `MonoGameLearning.Core.Combat` — but the logic itself is game-specific

### Suggestion — Move oil drum classes into the Game project

Move `OilDrumBehavior` and `OilDrumDamage` into the Game project (e.g., `MonoGameLearning.Game.Entities.Props` alongside `OilDrumEntity`). Core should only contain abstractions and generic combat utilities. `OilDrumEntity` already references its own namespace — these classes belong next to it.

### Files to change — Finding 4

- Move `MonoGameLearning.Core/Combat/OilDrumDamage.cs` → `MonoGameLearning.Game/Entities/Props/OilDrumDamage.cs` — update namespace to `MonoGameLearning.Game.Entities.Props`
- Move `MonoGameLearning.Core/Combat/OilDrumBehavior.cs` → `MonoGameLearning.Game/Entities/Props/OilDrumBehavior.cs` — update namespace to `MonoGameLearning.Game.Entities.Props`
- `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs` — update `using MonoGameLearning.Core.Combat` to `using MonoGameLearning.Game.Entities.Props`
- `MonoGameLearning.Game.Tests/OilDrumStateTests.cs` — update `using MonoGameLearning.Core.Combat` to `using MonoGameLearning.Game.Entities.Props`
