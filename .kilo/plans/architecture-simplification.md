# Architecture Simplification Plan

## Finding 1: `EnemyEntity._director` nullable with silent null guard

**Location:** `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs:48,96,149`
**Category:** Leaky Abstraction / Hidden Precondition

### Problem — Nullable director creates a silent dead path

`EnemyEntity` constructor accepts `LevelDirector director = null`. In production, a director is always provided. The null guard in `Update()` (`if (_director is null) return;`) causes the enemy to silently skip all AI, movement, and world queries — producing a non-functional entity with no diagnostic feedback. The null path only exists to accommodate tests that construct enemies via `FormatterServices.GetUninitializedObject` (bypassing the constructor entirely).

### Impact — Hidden precondition and misleading API

- Hidden precondition: enemies without a director compile but don't work
- The nullable parameter signals "optional" when it's actually required in production
- Tests use `FormatterServices` + override `OnRentEnemy` to avoid the dependency, but the pool's `DefaultFactory` always provides a director

### Suggestion — Make director required

Make `LevelDirector` a required (non-nullable) constructor parameter. Remove the null guard in `Update()` and `DrawDebug()`. Update pool-based tests to route enemy construction through `DefaultFactory` (or pass a test `LevelDirector` stub) instead of using `FormatterServices.GetUninitializedObject`.

### Files to change — Finding 1

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

- `MonoGameLearning.Core/Entities/Interfaces/IDamageable.cs` — split into two interfaces (or make `Died` optional via default interface implementation)
- `MonoGameLearning.Core/Entities/PropBase.cs` — implement `IDamageRecipient` only, remove `Died` suppression
- `MonoGameLearning.Core/Entities/CombatActorBase.cs` — implement both interfaces
- `MonoGameLearning.Core/Entities/EntityManager.cs` — check `IDamageRecipient` instead of `IDamageable` where appropriate
- `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs` — update base interface if needed
- Test files referencing `IDamageable` on props — verify compatibility

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

- `MonoGameLearning.Game/Levels/LevelDirector.cs` — add prop management (spawn, register, destroy)
- `MonoGameLearning.Game/GameLoop/GameLoop.cs` — remove `RegisterOilDrum`, `OnOilDrumDestroyed`, delegate prop setup to `LevelDirector`
- `MonoGameLearning.Game/Levels/Level.cs` or new file — optional prop pool class if needed

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

- Move `MonoGameLearning.Core/Combat/OilDrumDamage.cs` → `MonoGameLearning.Game/Entities/Props/OilDrumDamage.cs`
- `MonoGameLearning.Game/Entities/Props/OilDrumEntity.cs` — update using directives
- `MonoGameLearning.Game.Tests/OilDrumStateTests.cs` — update using directives
