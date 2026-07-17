# Audio Engine — Instance Injection

## Goal

Replace the `GameCore.Audio` static singleton with a constructor-injected `AudioManager` instance, matching the pattern used by every other engine service in this codebase (`InputManager`, `HitboxService`, `HudService`, `CameraController`, `LevelDirector`). Removes 12 hard-coded `GameCore.Audio.PlaySfx(...)` call sites in entities, consolidates the dual pause-wiring paths, and adds tests for the multiplicative pause-mute contract.

## Decisions

1. **Constructor injection, concrete type.** `AudioManager` injected directly into `PlayerEntity`/`EnemyEntity`. No `IAudio` interface — the codebase has no DI interfaces; concrete injection matches existing style.
2. **Single pause source of truth.** Drop the `_audioWasPaused` polling in `GameLoop.Update`. The `OnTransitioned` callback becomes the only place that calls `SetPaused`.
3. **Test seams → `internal` + `InternalsVisibleTo`.** No more `*ForTest` public accessors on `AudioManager`.
4. **`Update(GameTime)` → `Update()`.** Drop the unused `GameTime` parameter.
5. **Pure helper for the ducking math.** Extract `ComputeMusicVolume(base, isPaused)` so the multiplicative contract is unit-testable without a real `SoundEffectInstance`.

## Affected Files

| File | Change |
|---|---|
| `MonoGameLearning.Core/GameCore.cs` | Remove `public static AudioManager Audio` (L22) and assignment (L50) |
| `MonoGameLearning.Game/GameLoop/GameLoop.cs` | Add `_audio` field; construct in `Initialize`; thread into `PlayerEntity`, `LevelDirector`, `MenuManager` ctors; replace 5 inline call sites; drop `_audioWasPaused` polling (L210-215); add `case GameState.Paused: _audio.SetPaused(true)` to `OnTransitioned` |
| `MonoGameLearning.Game/Entities/Player/PlayerEntity.cs` | Add `AudioManager audio` ctor param; store as `_audio`; replace 6 `GameCore.Audio.PlaySfx(...)` calls |
| `MonoGameLearning.Game/Entities/Enemy/EnemyEntity.cs` | Same shape — 6 call sites |
| `MonoGameLearning.Game/Levels/LevelDirector.cs` | Add `AudioManager` ctor param (L49); pass to `EnemyPool` |
| `MonoGameLearning.Game/Levels/EnemyPool.cs` | `DefaultFactory` receives audio and passes to `EnemyEntity` ctor |
| `MonoGameLearning.Core/Audio/AudioManager.cs` | Make `*ForTest` accessors `internal`; add `MusicPauseDuck = 0.3f` const; extract `ComputeMusicVolume` helper; drop `GameTime` parameter from `Update` |
| `MonoGameLearning.Core/AssemblyInfo.cs` (or existing csproj) | Add `[assembly: InternalsVisibleTo("MonoGameLearning.Game.Tests")]` |

## Implementation Tasks

### Task 1 — Drop the static on `GameCore`

- Delete `public static AudioManager Audio { get; private set; }` (`GameCore.cs:22`).
- Delete `Audio = new AudioManager();` (`GameCore.cs:50`).

### Task 2 — Construct `_audio` in `GameLoop.Initialize`

- Add `private AudioManager _audio;`.
- In `Initialize`, after `_input = new InputManager();`: `_audio = new AudioManager();`.
- In `_gameState.StateMachine.OnTransitioned`: replace `Audio.*` references with `_audio.*`. Add `case GameState.Paused: _audio.SetPaused(true); break;` to the existing switch.
- In `LoadContent`: replace `Audio.LoadContent(Content)` with `_audio.LoadContent(Content)`.
- In `Update`: replace `Audio.Update(gameTime)` with `_audio.Update()`. Delete the `_audioWasPaused` field and the polling block at `GameLoop.cs:210-215` (now redundant with the transition handler).
- In `Update`: replace `Audio.PlaySfx(SfxId.GoPromptBell)` with `_audio.PlaySfx(SfxId.GoPromptBell)`.
- In `Initialize`: change `sfx => Audio.PlaySfx(sfx)` to `_audio.PlaySfx` (method group) on the `MenuManager` ctor call (L92).

### Task 3 — Inject `AudioManager` into `PlayerEntity`

- Add `private readonly AudioManager _audio;`.
- Add `AudioManager audio` parameter to ctor (after `sprite`).
- Assign `_audio = audio;`.
- Replace every `GameCore.Audio.PlaySfx(...)` with `_audio.PlaySfx(...)` in `OnAttackingEntry`, `OnHurtEntry`, `OnKnockdownEntry`, `OnDyingEntry`.
- `Reset()` (L174-178) needs no change — `_audio` is `readonly` and survives state-controller recreation.

### Task 4 — Inject `AudioManager` into `EnemyEntity`

- Same shape as Task 3.
- Add `AudioManager audio` ctor param (after `sprite`, before `director`).
- Update `EnemyPool.DefaultFactory` (`EnemyPool.cs:105-114`) to receive `AudioManager audio` and pass it to `new EnemyEntity(...)`.
- Update `EnemyPool` ctor to accept and store an `AudioManager` (alongside existing `entityManager`, `director`).
- Update `LevelDirector` ctor (`LevelDirector.cs:49`) to accept `AudioManager audio`; store and pass to `EnemyPool`.

### Task 5 — Test seam cleanup on `AudioManager`

- Change `GetMusicInstanceForTest`, `IsPausedForTest`, `RawMusicVolumeForTest` from `public` to `internal`.
- Add `[assembly: InternalsVisibleTo("MonoGameLearning.Game.Tests")]` to `MonoGameLearning.Core`. Check whether `MonoGameLearning.Core/AssemblyInfo.cs` exists or whether `csproj` already declares it before adding a duplicate.

### Task 6 — Extract pure helper for music volume math

- Add `private const float MusicPauseDuck = 0.3f;`.
- Add `internal static float ComputeMusicVolume(float baseVolume, bool isPaused) => isPaused ? baseVolume * MusicPauseDuck : baseVolume;`.
- Rewrite `ApplyMusicVolume` to: `_musicInstance.Volume = ComputeMusicVolume(_musicVolume, _isPaused);`.
- `Update` signature: drop `GameTime` parameter. Update `GameLoop` caller.

### Task 7 — New tests in `AudioManagerTests.cs`

- `ComputeMusicVolume_Paused_MultipliesByDuck` — `ComputeMusicVolume(0.8f, true) == 0.24f`
- `ComputeMusicVolume_Unpaused_ReturnsBase`
- `ComputeMusicVolume_ZeroBase_StaysZero`
- `ComputeMusicVolume_FullBasePaused_ReturnsDuck` — `ComputeMusicVolume(1f, true) == 0.3f`
- `Update_WithoutLoadedContent_DoesNotThrow` (replaces existing `Update_DoesNotThrow_WithoutMusic`)
- Preserve existing: `PlaySfx_WithoutLoadedContent_IsNoOp`, `PlayMusic_Null_DoesNotThrow`, `PlayMusic_SameTrack_DoesNotRestart`, `SfxVolume_ClampsZeroToOne`, `MusicVolume_ClampsZeroToOne`, `SfxVolume_Set_StaysInRange`, `MusicVolume_Set_StaysInRange`, `PauseMuting_Multiplicative_NotOverride` (still verifies `_isPaused` toggles — the actual volume math is now covered by `ComputeMusicVolume` tests).

### Task 8 — Build + test

- `dotnet build` (0 warnings, 0 errors)
- `dotnet test` (existing + new tests pass)

## Test Strategy

The previous review identified two coverage gaps. Both are addressed:

| Gap | Approach |
|---|---|
| "Pause muting is multiplicative" — old test only toggled `_isPaused` flag, never asserted the actual volume value | `ComputeMusicVolume` is a pure helper — the multiplication contract is now asserted directly with 4 test cases |
| `LevelComplete` sting self-cleans in `Update` | Public behavior (no-throw, idempotent) is re-asserted. Full state-transition simulation requires a real `SoundEffectInstance`, which is impractical in unit tests; document this as integration coverage |

`SfxPool` round-robin behavior cannot be exercised without content loading — accept and skip.

## Risks

- **Ripple through constructor signatures.** `EnemyEntity` ctor change cascades to `EnemyPool.DefaultFactory` → `EnemyPool` ctor → `LevelDirector` ctor → `GameLoop.InitLevelSystems`. All must change in one commit.
- **Order-of-construction in `GameLoop`.** `_audio` must exist before any code path that reads `Audio.*` (state transitions in `Initialize`, content load in `LoadContent`). Constructing it first in `Initialize` after `_input` is safe.
- **`InternalsVisibleTo` may already be declared.** Verify before adding.
- **Pause wiring consolidation could regress music ducking.** The polling block exists as a safety net; removing it means the transition handler is the only path. The `PauseMuting_Multiplicative_NotOverride` test still verifies the flag flips on `SetPaused`, so wiring regressions surface immediately.

## Validation

1. `dotnet build` — 0 warnings, 0 errors.
2. `dotnet test` — all existing tests pass, all new `ComputeMusicVolume` tests pass.
3. Manual smoke (developer machine): start the game, verify SFX plays for swing/hurt/death/menu, verify music transitions on state changes, verify pause ducks music volume, verify level-complete sting plays once and stops.

## Out of Scope

- Music fade-in/fade-out transitions
- 3D positional audio
- Master volume / per-bus volume
- Audio event hooks for game logic
- Refactor of magic-number SFX registration (the "edit 4 places" issue from the review)
