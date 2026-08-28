# [NOT IMPLEMENTED] Decouple AudioService from Game Content Asset Paths

**Verdict: VALID** — but scoped. Fix what is genuinely leaking, don't over-engineer.

## Concern

`AudioService` lives in Core (`MonoGameLearning.Core/Audio/AudioService.cs`) but
hardcodes the concrete asset manifest in `LoadContent`:

- `AudioService.cs:55-72` — 17 `"audio/..."` SFX paths baked into tuples.
- `AudioService.cs:74-76` — 3 `"audio/music_*"` music paths.
- `AudioService.cs:156-180` (`OnGameStateChanged`) — hardcodes the `GameState` → music track mapping.

A second game with different content cannot reuse the service as-is; it would
inherit this game's asset paths and audio IDs. This violates the AGENTS.md rule:
"Keep generic, reusable logic in Core and specific game implementation in Game."

## What is reusable vs. game-specific

- **Reusable (keep in Core)**: sound-instance pooling, per-instance volume,
  `PlaySfx`/`PlayMusic`, loop + pause-duck logic, `Update`.
- **Game-specific (move to Game)**: the asset path manifest and the state→music
  mapping.

## Bounds / decision (keeps the change small)

Keep `SfxId` (`SfxId.cs`) and `MusicId` (`MusicId.cs`) enums in Core. They are
referenced throughout Core (`MoveData.AttackSfx`/`ImpactSfx`, `CombatActorBase`,
`LevelDirectorCore.GoPromptBell`, `DamageInfo`). Moving them to Game would force
Core to reference Game types and trigger a large ripple — out of scope. The
reusability problem is the **content paths**, not the enum identifiers.

## Tasks

1. Change `AudioService.LoadContent` to take the manifest instead of hardcoding it:
   ```csharp
   public void LoadContent(ContentManager content,
       IReadOnlyList<(SfxId Id, string Path)> sfxAssets,
       IReadOnlyList<(MusicId Id, string Path)> musicAssets)
   ```
   Loop over the supplied lists in the existing `LoadSfxGroup`/`LoadMusic` spots.
2. Add `MonoGameLearning.Game/Audio/AudioManifest.cs` (static class) that owns the
   17 SFX + 3 music `(Id, Path)` entries currently hardcoded in Core.
3. Update `GameLoop.LoadContent` (`GameLoop.cs:117`) to
   `_audio.LoadContent(Content, AudioManifest.SfxAssets, AudioManifest.MusicAssets)`.
4. *(Optional, separate commit)* Move the `OnGameStateChanged` state→music mapping
   (`AudioService.cs:156-180`) into `GameLoop`'s existing `OnTransitioned` handler
   (`GameLoop.cs:88-96`), replacing `_audio.OnGameStateChanged(t.Source, t.Destination)`
   with a Game-side call to `_audio.PlayMusic` / `_audio.SetPaused`. This removes the
   `GameState` coupling from `AudioService` entirely.

## Risks

- Tuple/list construction happens once at load (not per-frame) — no GC concern.
- `AudioServiceTests` (`AudioServiceTests.cs`) exercise `OnGameStateChanged`. If
  task 4 is done, delete/relocate those tests; if not, leave them. Do not break them.

## Validation

- `dotnet build --warnaserror` (0 warnings).
- `dotnet test` (all green).

## Assumptions

- The user wants reusability without moving `SfxId`/`MusicId` out of Core (a much larger change).

## Follow-up questions (for a dedicated planning session)

- Do you want `OnGameStateChanged` removed from `AudioService` in the same change, or kept (task 4 optional)?
- Should a future game use a different audio ID model (strings / own enums), implying `SfxId` should eventually leave Core too?