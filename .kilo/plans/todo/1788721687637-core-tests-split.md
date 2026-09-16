# Plan: Split Core Tests into a `MonoGameLearning.Core.Tests` Project

**Status: NOT IMPLEMENTED — decision + design, deliberately parked.**

## Recommendation

**Do not create `MonoGameLearning.Core.Tests` right now.** Keep every test in
`MonoGameLearning.Game.Tests`, which references both `MonoGameLearning.Game` and
`MonoGameLearning.Core` today. Revisit only if a concrete trigger appears (see
"Triggers that would justify implementing this").

This mirrors the architecture audit, which documented the single test project as
the deliberate status quo rather than a smell (`.kilo/plans/implemented/1787965629820-architecture-audit-findings.md:9`: "One namespace has no
`.Core.Tests` project (the directory is empty); all tests live in
`MonoGameLearning.Game.Tests`."). It also aligns with the Solution
Simplification / audit-simplicity convention: one test project is fewer moving
parts for no loss of coverage today.

## Current State (facts)

- `MonoGameLearning.Game.Tests` contains **44** `.cs` files; **40** of them
  carry `[Test]`/`[TestCase]` (467 attribute occurrences; `dotnet test` runs
  464 passed / 3 skipped).
- The project references both Core and Game
  (`MonoGameLearning.Game.Tests/MonoGameLearning.Game.Tests.csproj:25-26`);
  therefore Core-driven logic is already tested in the same assembly.
- ~**223** of the 467 tests are *pure-Core*: their files contain **no**
  `using MonoGameLearning.Game` reference (see "Pure-Core test files" below).
  They already define their own local test doubles (e.g.
  `TestActorEntity`/`CollisionPushEntity` in `ActorCollisionTests.cs`), so they
  are self-contained and movable.

## Why a Split Is Not Worth It Today

Arguments **for**:

- Correct dependency direction: `Core.Tests → Core` proves Core compiles and tests standalone.
- Mirrors the Separation of Concerns convention (generic in Core, specific in Game).
- Resolves the flagged "no `.Core.Tests`" note in the audit plan.

Arguments **against** (why we lean against):

- Core has no consumer outside this repo today, so "standalone" verification is theoretical.
- The boundary is not clean: integration tests (player/enemy/level/weapons) must stay in `Game.Tests`, so the split yields **two** test projects for ~half the tests.
- `Game.Tests` → Core reference gives the identical coverage with one project. Splitting adds a project, a reference, namespace churn, and CI surface for no behavioral gain.

## Design (if ever implemented)

### 1. Project

New NUnit project `MonoGameLearning.Core.Tests/` (mirror
`MonoGameLearning.Game.Tests/MonoGameLearning.Game.Tests.csproj`):

- `TargetFramework net10.0`, NUnit 4.3.2, NUnit3TestAdapter, coverlet
- **Single** `<ProjectReference>` to `MonoGameLearning.Core.csproj` — Core.Tests
  must compile and pass with zero references to `MonoGameLearning.Game`.
- Add to `MonoGameLearning.Game.slnx`.

### 2. What moves

Move the pure-Core files listed below. Namespace
`MonoGameLearning.Game.Tests` → `MonoGameLearning.Core.Tests`. Each file already
carries its local doubles; audit for shared doubles first (see Risks).

### 3. What stays

Everything that uses `MonoGameLearning.Game` stays in `Game.Tests`:
`LevelDirectorTests`, `MeleeWeaponTests`, `BatSwingSyncTests`, `PlayerStateTests`,
`EnemyStateTests`, `EnemyPoolTests`, `GameLoopLivesTests`, `GameLoopMusicTests`,
`PlayerHudTests`, `GoIndicatorTests`, `OilDrumEntityTests`, `OilDrumDamageTests`,
`FoodPickupEntityTests`, `EnemyEntityTests`, `EnemyDropsOnDeathTests`,
`AudioManifestTests`, `StateMachineControllerTests`, and the test-double files
(`TestPlayerEntity`, `TestEnemyEntity`, `TestableOilDrumEntity`, `RespawnTestHelper`).

### 4. Verification

- `dotnet build --warnaserror` → 0 warnings across the new 4-project solution.
- `dotnet test` → total count unchanged (464 passed / 3 skipped), now spread
  across two test assemblies.
- `dotnet test --project MonoGameLearning.Core.Tests` passes standalone and
  proves the dependency direction.
- `MANUAL_TESTING.md` untouched (no runtime behavior changes).

## Pure-Core test files (~223 tests)

| File | Tests |
| --- | --- |
| `ActorCollisionTests.cs` | 23 |
| `LifecycleTests.cs` | 29 |
| `EntityServiceRegistrationTests.cs` | 25 |
| `HitboxTests.cs` | 21 |
| `OilDrumCollisionTests.cs` | 15 |
| `AudioServiceTests.cs` | 15 |
| `HudServiceTests.cs` | 14 |
| `GameStateTests.cs` | 11 |
| `ResolutionSettingsTests.cs` | 8 |
| `HealthTests.cs` | 7 |
| `AudioSettingsTests.cs` | 7 |
| `AnimationFrameTrackerTests.cs` | 6 |
| `CollisionWorld2DTests.cs` | 6 |
| `CombatInterfaceTests.cs` | 6 |
| `PropDropsOnDestroyTests.cs` | 6 |
| `CollisionLayerTests.cs` | 5 |
| `PickupRegistrationTests.cs` | 5 |
| `CollisionWorldFactoryTests.cs` | 4 |
| `PickupCollisionTests.cs` | 3 |
| `PickupServiceTests.cs` | 3 |
| `StaticTextureAssetTests.cs` | 2 |
| `LevelDirectorPickupSpawnTests.cs` | 2 |

Helper only, no tests: `FacingChangeForcingEnemy.cs`.

## Boundary cases to audit before moving

- `LevelDirectorPickupSpawnTests` (2) — name suggests Game-side `LevelDirector`,
  yet has no `using MonoGameLearning.Game`; confirm it targets
  `LevelDirectorCore` (Core) vs the Game subclass.
- `OilDrumCollisionTests` / `PropDropsOnDestroyTests` — may exercise Game-side
  classes through Core interfaces; classify per file, don't split mid-file.
- Shared doubles: verify no pure-Core file references `TestEnemyEntity`,
  `TestPlayerEntity`, or `TestableOilDrumEntity` (they live in Game-side helper
  files). If one does, either duplicate the double in Core.Tests or keep the
  file in Game.Tests.

## Risks

- Namespace churn across 20+ moved files with no behavioral benefit.
- Two assemblies to build/test instead of one; `dotnet test` finds both only
  because both are in the slnx.
- Test counts split across engines can make CI output harder to read.

## Triggers that would justify implementing this

- Core is consumed by a second `Game` project (real reuse → standalone
  verification becomes valuable).
- A Core-only test needs a package reference that Game.Tests doesn't want.
- The single test project grows unwieldy enough that the split pays for itself.

## Acceptance Criteria (only if implemented)

- `MonoGameLearning.Core.Tests` builds and passes with **zero** references to
  `MonoGameLearning.Game` (proves Core stands alone).
- Full solution: `dotnet build --warnaserror` = 0 warnings;
  `dotnet test` = 464 passed / 3 skipped total across both test projects.
- Game-side integration tests remain in `Game.Tests`; no test file is split.
