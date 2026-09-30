# Architecture Audit — Remaining Fixes

## Smell: God Class — `GameLoop` orchestrates 15+ service domains in 388 lines

**Location:** `MonoGameLearning.Game/GameLoop/GameLoop.cs:31-388`

**Problem:** `GameLoop` directly manages input, audio, state-machine setup, menu construction,
content loading (fonts, sprites, weapons, pickups, HUD), level creation, background rendering,
collision world setup, entity service creation, projectile service creation, camera updates,
movement bounds, entity updates, hitbox resolution, projectile updates, collision resolution,
pickup resolution, HUD proximity tracking, respawn/lives management, debug overlay, and the entire
rendering pipeline (two separate `SpriteBatch.Begin/End` blocks).

**Impact:** Any new system (particles, second player, weather, AI director, save system) requires
modifying this single class. The `Update` method's service ordering is implicit — the wrong order
produces bugs that are hard to diagnose.

**Suggestion:** Extract into separate classes:

- **`GameUpdatePipeline`**: encapsulates the ordered `Update` sequence (entity lifecycle → level
  director → camera → input → movement → snapshot → entity update → hit resolution → projectiles
  → collisions → pickups → HUD). `GameLoop` calls `pipeline.Update(gameTime)`.
- **`LevelBootstrapper`**: encapsulates `ReinitLevel` + `InitLevelSystems` — creates level data,
  background, collision world, entity service, projectile service, level director, camera.
- **`GameRenderPipeline`**: encapsulates the camera-space and screen-space render passes.

---

## Smell: Misplaced Core Class — `SettingsService` is a static class with mutable global state

**Location:** `MonoGameLearning.Core/Settings/SettingsService.cs:12-124`

**Problem:** All members are `public static`: `AudioSettings`, `CurrentResolution`, `Load`,
`Save`, `Apply`. Tests that read `SettingsService.AudioSettings` after another test wrote to it
get dirty state (order-dependent failures). The static state also prevents using Core in a
headless context (e.g., a server-side replay processor).

**Impact:** Tests cannot be parallelized or run in arbitrary order.

**Suggestion:** Convert to an instance class with an `ISettingsProvider` interface for the
file-I/O part. Inject where needed (`GameLoop`, `MenuService`).
