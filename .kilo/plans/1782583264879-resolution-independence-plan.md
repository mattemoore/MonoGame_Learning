# Plan: Resolution-Independent Rendering Fix + Resolution Setting

## Goal

Fix two resolution-dependent rendering bugs found in the prior review, and add a user-selectable resolution setting.

## Findings Recap (from prior review)

The codebase is correctly resolution-independent for **game world** rendering (entities, background, camera, collision, hitboxes, debug lines inside `SpriteBatch.Begin(transformMatrix: Camera.GetViewMatrix())`). All of that uses virtual 800x600 coordinates and stays correct at any resolution.

The two bugs are both **outside** that camera-transform block:

1. **"GO ->" prompt**: Drawn with `SpriteBatch.Begin()` (no transform), using `ViewportAdapter.VirtualWidth/Height` in virtual coordinates. At non-4:3 aspect ratios this is mispositioned.
2. **Gum UI overlays**: Use full-window coordinates with `PixelsFromMiddle` positioning. At non-4:3 ratios they offset from the letterboxed game area.

## Decisions Resolved (with user)

- **GO ->** becomes a **world-space entity** (static PNG sprite) tracked to the right edge of the camera viewport. Renders through `Camera.GetViewMatrix()`, so it auto-scales. Eliminates bug #1 by removing the no-transform `SpriteBatch.Begin()` block entirely.
- **Gum UI** stays **full-window overlay** — standard UX for game menus, backdrop covers letterbox bars. No code change needed for bug #2.
- **Resolution list**: all `GraphicsAdapter.SupportedDisplayModes`.
- **Apply timing**: immediately on selection (Graphics.ApplyChanges()).
- **Persistence**: JSON file in app data dir.
- **Visual style**: static PNG sprite for GO indicator.

## Affected Files

| File | Change |
|---|---|
| `MonoGameLearning.Game/GameLoop/GameLoop.cs` | Remove `SpriteBatch.Begin()` GO block; wire new GO entity + settings screen |
| `MonoGameLearning.Game/GameLoop/MenuManager.cs` | Add `SettingsScreen` (Gum), navigation handlers, resolution change handler |
| `MonoGameLearning.Game/Entities/GoIndicator/GoIndicatorEntity.cs` (new) | World-space entity tracking camera right edge |
| `MonoGameLearning.Game/AnimatedSprites/GoIndicatorSprite.cs` (new) | Static AnimatedSprite loader |
| `MonoGameLearning.Core/GameCore/GameCore.cs` | Apply `ResolutionSettings` on startup, add window resize hook to recompute (defensive) |
| `MonoGameLearning.Core/Settings/ResolutionSettings.cs` (new) | Load/save JSON, default to 1024x768 |
| `MonoGameLearning.Core/Settings/SettingsService.cs` (new) | Static helper for settings path + apply-to-Graphics |
| `MonoGameLearning.Game/Content/Content.mgcb` | Add new `go_indicator.png` sprite asset |
| `MonoGameLearning.Core.Tests/ResolutionSettingsTests.cs` (new) | Load/save round-trip, missing-file default |
| `MonoGameLearning.Game.Tests/GoIndicatorTests.cs` (new) | Position tracks camera right edge |
| `MonoGameLearning.Game/Menus/SettingsScreen.cs` (new) or inline in MenuManager | Resolution picker UI |

## Implementation Steps

### Step 1: Add resolution settings persistence (Core)

Create `MonoGameLearning.Core/Settings/ResolutionSettings.cs`:
- Record `ResolutionSetting(int Width, int Height)`.
- Class `ResolutionSettings` with `Current`, `AvailableResolutions`, `Load()`, `Save()`.
- JSON in `%APPDATA%/MonoGameLearning/settings.json` (cross-platform via `Environment.SpecialFolder.LocalApplicationData`).
- Default: 1024x768 if file missing or invalid.
- `AvailableResolutions`: populated from `GraphicsAdapter.DefaultAdapter.SupportedDisplayModes`, deduplicated by (width, height), filtered to reasonable bounds (>= 640x480, <= 7680x4320).

Create `MonoGameLearning.Core/Settings/SettingsService.cs`:
- `static void Apply(GraphicsDeviceManager g, ResolutionSetting s)` → sets `PreferredBackBufferWidth/Height`, `IsFullScreen = false`, calls `ApplyChanges()`.

### Step 2: Add GO indicator entity (Game)

Create `MonoGameLearning.Game/Entities/GoIndicator/GoIndicatorEntity.cs`:
- `class GoIndicatorEntity(string name, Vector2 position, float scale, AnimatedSprite sprite) : Entity(name, position, (int)(sprite.Size.X * scale), (int)(sprite.Size.Y * scale)), IUpdatable, IRenderable, IDebugDrawable`.
- `Update(GameTime)`: no-op (static position).
- `Render(RenderContext)`: `context.SpriteBatch.Draw(Sprite, Position, 0f, new Vector2(Scale))` — matches existing `PropBase.Render` pattern.
- Constructor takes `(OrthographicCamera camera, int virtualHeight)` or exposes a `Update(OrthographicCamera camera)` method that sets `Position = new Vector2(camera.BoundingRectangle.Right - margin, virtualHeight / 2f)`.

Create `MonoGameLearning.Game/AnimatedSprites/GoIndicatorSprite.cs`:
- Load `images/go_indicator` atlas texture.
- Single animation, looping.

Update `Content.mgcb` to include the new `go_indicator.png` (and any atlas JSON it requires).

### Step 3: Wire GO indicator into GameLoop (Game)

In `MonoGameLearning.Game/GameLoop/GameLoop.cs`:
- Add field `_goIndicator` and `_goSprite`.
- In `LoadContent()`: load sprite, create entity, register with `_entityManager` (it'll auto-cull via camera frustum check at render time — but `ShowGoPrompt` gating is needed).
- In `Update()` when state is `Playing` and `_levelDirector.ShowGoPrompt`:
  - Update GO indicator position: `_goIndicator.Position = new Vector2(Camera.BoundingRectangle.Right - 30, GAME_HEIGHT / 2f)`.
- In `_entityManager.Renderables`, gate visibility via `Visible` flag (set/clear based on `ShowGoPrompt`).
- **Delete** lines 216-226 (`if (_levelDirector.ShowGoPrompt) { SpriteBatch.Begin(); ... SpriteBatch.End(); }`).
- Also remove `_debugFont` usage if GO was its only consumer in the no-transform block (verify).

### Step 4: Add SettingsScreen + resolution picker (Game)

In `MonoGameLearning.Game/GameLoop/MenuManager.cs`:
- Add `ContainerRuntime _settingsScreen`.
- Add `BuildSettingsScreen()` method (similar to `BuildScreen()`, lists resolutions as menu items).
- Add `OnGameStateChanged()` handling for `GameState.Settings`.
- Extend `GameState` enum (in `GameStateController.cs`) with `Settings`.
- Extend `GameStateController` state machine:
  - From `TitleScreen`: `Permit(GameTrigger.OpenSettings, GameState.Settings)`.
  - From `Paused`: `Permit(GameTrigger.OpenSettings, GameState.Settings)`.
  - From `Settings`: `Permit(GameTrigger.ReturnToTitle, GameState.TitleScreen)`, `Permit(GameTrigger.PauseToggle, GameState.Paused)`.
- Add `HandleMenuNavigation` and `HandleConfirm` cases for `Settings`.
- On selection of a resolution item:
  - Call `SettingsService.Apply(GraphicsDeviceManager, selected)`.
  - Call `ResolutionSettings.Save()`.
- Add navigation: from `TitleScreen` and `Paused` menus, add a "Settings" item that fires `GameTrigger.OpenSettings`.

### Step 5: Apply settings on startup (Core)

In `MonoGameLearning.Game/GameLoop/GameLoop.cs` (primary constructor or Initialize):
- Change primary constructor from hardcoded constants to instance fields populated from `ResolutionSettings.Load()`:
  ```csharp
  public class GameLoop() : GameCore("Game Demo",
      ResolutionSettings.Current.Width,
      ResolutionSettings.Current.Height,
      GAME_WIDTH, GAME_HEIGHT, IS_FULL_SCREEN)
  ```
- Note: primary constructors evaluate RHS at construction, so this works only if `ResolutionSettings.Load()` is static and called eagerly. Alternative: keep constants as defaults and apply actual setting in `Initialize()` *before* `base.Initialize()`:
  ```csharp
  protected override void Initialize()
  {
      var settings = ResolutionSettings.Load();
      Graphics.PreferredBackBufferWidth = settings.Width;
      Graphics.PreferredBackBufferHeight = settings.Height;
      Graphics.ApplyChanges();
      base.Initialize();
      // ... existing init ...
  }
  ```
  This is the correct approach — `base.Initialize()` reads `Graphics.PreferredBackBufferWidth/Height` to create the `BoxingViewportAdapter`.

### Step 6: Tests

**`MonoGameLearning.Core.Tests/ResolutionSettingsTests.cs`**:
- `Load_FileMissing_ReturnsDefault1024x768`
- `SaveThenLoad_RoundTrips`
- `AvailableResolutions_DeduplicatesSameResolutionDifferentRefreshRates`
- `Apply_SetsPreferredBackBufferWidthAndHeight` (uses a mocked `GraphicsDeviceManager` or integration test against a real one — verify approach).

**`MonoGameLearning.Game.Tests/GoIndicatorTests.cs`**:
- `Position_TracksCameraRightEdge` — create entity, set `Camera.BoundingRectangle`, verify `Position.X == Right - margin`.
- `Render_DrawsSpriteAtPosition` — verify `SpriteBatch.Draw` called with expected parameters (mock or integration).

### Step 7: Build verification

Run `dotnet build` to verify compilation. Run `dotnet test` to verify no regressions.

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| `Graphics.ApplyChanges()` mid-frame causes flicker | Acceptable per user decision; tests ensure no state corruption |
| `BoxingViewportAdapter` doesn't update on `Graphics.PreferredBackBufferWidth` change | Verify by inspection: adapter listens to `Window.ClientSizeChanged`, which fires on backbuffer change |
| Gum's `GumService.Initialize` registers a fixed viewport on startup | Re-initialize Gum if needed, or accept full-window behavior (user chose this) |
| New PNG asset missing from `Content.mgcb` | Verify with `ls Content/bin` post-build |
| Settings menu persists after game close | Intentional per user decision |

## Out of Scope

- Fullscreen mode toggle (separate feature)
- Windowed/borderless/fullscreen mode picker
- VSync / frame rate cap settings
- Audio / volume settings
- Localization
- UI scaling for accessibility (the virtual 800x600 resolution already serves this)

## Validation Checklist

1. `dotnet build` succeeds with no warnings.
2. `dotnet test` passes with new tests.
3. Game launches and renders identically at 1024x768 (default) and a 4:3 alternative (e.g., 1280x960).
4. Game launches and renders correctly at 16:9 (e.g., 1920x1080) — game world letterboxed, menus cover full window.
5. GO -> indicator appears at right edge of camera view when wave is cleared, renders through camera transform, scales correctly at all resolutions.
6. Resolution selection persists across game restarts (verified by editing JSON, restarting, observing).
7. Debug HUD still shows correct Virtual/Actual/Buffer/Window dimensions (existing functionality unchanged).