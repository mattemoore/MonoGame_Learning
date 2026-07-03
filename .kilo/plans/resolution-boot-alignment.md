# Plan: Resolution Boot Alignment Bug

## Problem

When a non-default resolution is saved (e.g., 1280x960) and the game is restarted:

- `Window.ClientBounds` reports 1280x785 instead of 1280x960
- `BoxingViewportAdapter` calculates viewport as 1047x785 (letterboxed within the 1280x785 window)
- Game world (background) renders within the viewport — has black bars on left/right
- Gum UI fills the **entire** window, so menu is centered in 1280x785 while background is centered in 1047x785 → misalignment

This causes three visible symptoms:

1. **Title screen menu**: off-center relative to the background (background is in letterboxed viewport, menu fills full window)
2. **Pause menu during gameplay**: off-center relative to the game world behind it (same letterbox mismatch)
3. **GO arrow indicator**: not pushed to the right edge of the visible area — positioned relative to the wrong viewport dimensions

`Graphics.PreferredBackBufferWidth/Height` is correct (1280x960). The backbuffer IS 1280x960. But `Window.ClientBounds` is 1280x785 — they differ. On DesktopGL/SDL2 the window client area IS the backbuffer, so these should always match.

## What We've Tried (didn't fix it)

All three changes below are still correct improvements, but the window size mismatch persists:

1. **`GameLoop.cs:26-27`**: `RESOLUTION_WIDTH/HEIGHT` changed from `const` (1024x768) to `static readonly` initialized from `ResolutionSettings.Load()`. Ensures `GraphicsDeviceManager.PreferredBackBufferWidth/Height` are set before device creation.

2. **`GameCore.cs:42-48`**: Removed `Graphics.ApplyChanges()` from constructor. Device doesn't exist during construction; call was a no-op.

3. **`GameCore.cs:59-68`**: Reordered `Initialize()` to: `base.Initialize()` → `Graphics.ApplyChanges()` → viewport/camera/SpriteBatch setup → `Gum.Initialize(this)`. Ensures device and window exist before Gum initializes.

## Root Cause Hypotheses

### Hypothesis A: SDL2 window not resized to `PreferredBackBuffer` on Linux

On Linux with SDL2, `Window.AllowUserResizing = true` + `GraphicsDeviceManager.CreateDevice()` may create the window at a system-determined size instead of the preferred backbuffer size. `Graphics.ApplyChanges()` after `base.Initialize()` should fix this — but apparently doesn't.

**Investigation**: Add a debug log reading actual SDL2 window dimensions immediately after `base.Initialize()`. Check if `GraphicsDevice.PresentationParameters.BackBufferWidth/Height` matches `Window.ClientBounds.Width/Height`.

### Hypothesis B: Title bar / window decorations consume client area

If the window is created at 1280x960 but the title bar + borders consume 175px of vertical space, the client area (Window.ClientBounds) would be 1280×785. SDL2 client area should NOT include decorations — but some X11 window managers might lie about this.

**Investigation**: Print `Window.ClientBounds` vs `GraphicsDevice.PresentationParameters.BackBufferHeight`. Check if the window position is near the bottom of the screen (WM might be shrinking to fit).

### Hypothesis C: `AllowUserResizing = true` allows the WM to ignore the preferred size

On X11 with a resizable window, the window manager may use hints or ignore `SDL_SetWindowSize` if the window is already mapped at a different size.

**Investigation**: Temporarily set `Window.AllowUserResizing = false` and test. If that fixes it, the WM is ignoring the resize hint on resizable windows.

### Hypothesis D: `HardwareModeSwitch = false` interacts badly with SDL2 window sizing

`HardwareModeSwitch = false` creates a borderless (or "fake fullscreen") window via SDL_WINDOW_FULLSCREEN_DESKTOP flag. In windowed mode this flag might cause SDL to report wrong dimensions.

**Investigation**: Check what `Sdl.Window.GetFlags()` returns after creation. Try `HardwareModeSwitch = true`.

### Hypothesis E: `Svc.EnableExpandToWindow(1f)` in Gum overrides window size

Gum's `EnableExpandToWindow(1f)` might resize the underlying window or set its own canvas dimensions based on a stale value read during initialization.

**Investigation**: Comment out `Svc.EnableExpandToWindow(1f)` and manually set Gum root dimensions. Check if `Window.ClientBounds` changes after `Gum.Initialize(this)`.

## How to Reproduce

1. Launch game at default 1024x768
2. Go to Settings → change resolution to 1280x960 → confirm
3. Exit game
4. Relaunch game
5. Check debug overlay for Viewport (Virtual/Actual), Screen Buffer, Window dimensions
6. Observe: Window should be 1280x960 but shows 1280x785

## Debugging Steps (next)

1. **Add immediate logging**: Print `Window.ClientBounds`, `GraphicsDevice.PresentationParameters.BackBufferWidth/Height`, and `Graphics.PreferredBackBufferWidth/Height` right after `base.Initialize()` and right after `Graphics.ApplyChanges()` in `GameCore.Initialize()`.

2. **Isolate `AllowUserResizing`**: Test with `Window.AllowUserResizing = false` to see if the WM is ignoring the resize.

3. **Check SDL2 flags**: Use `Sdl.Window.GetFlags(this.Window.Handle)` to inspect the window creation flags.

4. **Force explicit window resize**: After `Graphics.ApplyChanges()`, try SDL2's `SDL_SetWindowSize` directly with the preferred dimensions.

5. **Test fullscreen**: Try `IsFullScreen = true` to see if the issue is specific to windowed mode.

6. **Test borderless window**: Remove `HardwareModeSwitch = false` and try borderless windowed mode via other means.

## Files Involved

| File | Role |
|---|---|
| `MonoGameLearning.Core/GameCore/GameCore.cs` | Initialization order, GraphicsDeviceManager setup |
| `MonoGameLearning.Game/GameLoop/GameLoop.cs` | Static initializer loading saved resolution |
| `MonoGameLearning.Core/Settings/ResolutionSettings.cs` | Resolution persistence |
| `MonoGameLearning.Core/UI/GumManager.cs` | Gum canvas sizing |

## Acceptance Criteria

- `Window.ClientBounds` == `GraphicsDevice.PresentationParameters.BackBufferWidth/Height` after initialization
- No letterboxing when window and backbuffer match (both 4:3)
- Menu (Gum) and background (world) are aligned at all supported resolutions on boot
