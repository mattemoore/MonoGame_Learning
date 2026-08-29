# Manual Test Plan

Scope: behavior that REQUIRES a running window and interactive input. Everything listed here is
NOT meaningfully covered by `dotnet test` (no headless `GraphicsDevice`, `SoundEffect`, real
keyboard, or human visual judgement available in the automation layer).

Automated coverage boundary — the following are already green under `dotnet test` and are NOT
repeated here:

- State machines (GameState/Player/Enemy), health/damage/knockdown math, hitbox frame registration
- Collision world resolution, entity pooling, LevelDirector wave/spawn logic, prop drop defs
- Settings persistence and the resolution list, audio volume math (incl. pause duck constant)
- GO indicator anchor math and HUD target-selection logic, weapon anchor/frame math
- Boot sequence ordering (`GameCore.Initialize`)

## Prerequisites

- Build: `dotnet run --project MonoGameLearning.Game/MonoGameLearning.Game.csproj`
- Debug build so the `~` overlay and `K`/`C` debug keys work.

## Controls reference

| Key | Action |
| --- | --- |
| W/A/S/D or Arrows | Move (gameplay) / menu navigation (menus) |
| U / I / O | Attack 1 / 2 / 3 |
| Enter / Space | Confirm menu selection |
| Escape | Back (pause/resume, close settings, quit from title) |
| `~` | Toggle debug overlay + debug drawing |
| K (debug) | Kill the player |
| C (debug) | Complete the level |

## 1. Boot and window behavior

| # | Test | Expected |
| --- | --- | --- |
| 1.1 | Cold start with no settings file | Boots to title screen at default 1024x768, no crash |
| 1.2 | Set a non-default resolution (e.g. 1280x960) in Settings → Exit → relaunch | Window client size and backbuffer match the saved resolution; world and Gum menus align (see known issue below) |
| 1.3 | Resize the window by dragging (window is resizable) | Letterboxed viewport re-computes; world, HUD, GO indicator, and Gum menus all stay aligned inside the visible region; nothing clips outside it |
| 1.4 | Resize to a non-4:3 aspect (tall or wide) | Content letterboxes correctly; HUD stays top-left, GO stays top-right within the letterbox, Gum menus remain centered |

> Known issue — resolution boot alignment: with a non-default saved resolution, `Window.ClientBounds`
> can differ from the backbuffer (e.g. 1280x785 vs 1280x960), letterboxing the world while Gum fills
> the whole window. Symptoms: title/pause menus off-center relative to the background, GO indicator
> not at the visible right edge. Tracked in
> `.kilo/plans/[NOT IMPLEMENTED] resolution-boot-alignment.md`.

## 2. UI scaling (regression check)

| # | Test | Expected |
| --- | --- | --- |
| 2.1 | Play at the default 800x600 window | GO indicator and HUD look as before (identity scale baseline) |
| 2.2 | Play at 1024x768 and 1600x1200 (via Settings) | GO indicator, player HP bar, enemy HP bar scale uniformly with the world; nothing off-screen, no overlap |
| 2.3 | Debug `[GO]` label in debug mode at a scaled resolution | Renders above the GO indicator at the right edge, on-screen |

## 3. Visual quality and animation

| # | Test | Expected |
| --- | --- | --- |
| 3.1 | Player idle / run / attack1 / attack2 / attack3 / hurt / fall / getup / die | Each animation plays its full loop/sequence at a natural frame rate; no freezing, skipping, or wrong frames |
| 3.2 | Equip a bat, then attack | Bat sprite overlays the armed animation in sync (swing apex around attack frames 2-3); bat does not lag or desync from the player's arm |
| 3.3 | Scroll through the level | The 3 background panels tile seamlessly — no visible seam, gap, or color mismatch while the camera moves |
| 3.4 | Stand above/below an oil drum at different Y positions | Player renders in front of or behind the drum correctly (Y-sort) |
| 3.5 | Wave cleared | GO indicator pulses/flashes lime green top-right, phases out when the next wave spawns |
| 3.6 | Destroy oil drums | Explosion visual plays; dropped food/bat pickups appear at the drum location and are visible/readable |
| 3.7 | HUD legibility at 1600x1200 | Player name, lives, health bar, mugshot letter, and enemy bar are readable; no text/bars overlap |

## 4. Audio

| # | Test | Expected |
| --- | --- | --- |
| 4.1 | Title screen and Settings | Title menu music plays |
| 4.2 | Start game | Music switches to gameplay track |
| 4.3 | Level complete | Level-complete track plays once and stops (non-looping); game stays responsive |
| 4.4 | Game over | Music stops |
| 4.5 | Pause (Escape during play) | Music ducks to ~30% volume; Resume restores it |
| 4.6 | Attacks / hits / knockdown / death | Correct SFX per action: attack swings 1-3, hit light/heavy/metal, player hurt/death, enemy hurt/death, knockdown |
| 4.7 | Rapid repeated attacks | Overlapping SFX play without cutting each other off or crackling (3-instance pool per SFX) |
| 4.8 | Settings SFX / Music sliders | Audible immediate change; 0 = silent; persists after restart; both sliders independent |
| 4.9 | Pick up food or a bat | Pickup SFX plays for both; volume consistent |
| 4.10 | Big combat moment (hit + bell + explosion together) | No clipping/crackle; volumes stay balanced |

## 5. Input and controls

| # | Test | Expected |
| --- | --- | --- |
| 5.1 | WASD and Arrows | Both move the player; diagonal moves are normalized (no speed boost) |
| 5.2 | Hold a movement key | No repeated state flapping; movement is continuous, edge-triggered actions (U/I/O) do not auto-repeat |
| 5.3 | U / I / O | Each triggers attack1/2/3 with its animation, SFX, and range |
| 5.4 | Escape | Playing → Pause; Pause → Resume; Settings → back to prior screen; Title → exits the game |
| 5.5 | Enter / Space in menus | Activates selected item; both work |
| 5.6 | Menu Up/Down and cursor | Cursor (`>` prefix) moves and clamps at the first/last item |
| 5.7 | Settings Left/Right | Cycles resolution / adjusts SFX / music volume only on the focused row |
| 5.8 | During gameplay, menu keys (W/S) | Do NOT navigate menus or move cursor (input mode is gameplay) |

## 6. Menus and settings

| # | Test | Expected |
| --- | --- | --- |
| 6.1 | Title screen | Shows Start Game / Settings / Exit; visual layout centered and readable |
| 6.2 | Pause menu | Resume / Settings / Quit to Title; choosing Quit to Title returns to title |
| 6.3 | Game Over | Retry restarts a fresh level (see 7.10); Quit to Title returns to title |
| 6.4 | Level Complete | Return to Title returns to the title screen |
| 6.5 | Settings back-navigation | From Pause → Settings → ESC returns to Pause; from Title → Settings → ESC returns to Title |
| 6.6 | Settings apply | Choosing a resolution resizes the window immediately; bars/values update live |
| 6.7 | Menu remember state | Opening Settings from Pause and leaving keeps the pause flow consistent on return |

## 7. Gameplay flow

| # | Test | Expected |
| --- | --- | --- |
| 7.1 | Wave trigger | Approaching ~x=800 triggers wave 1 spawn; camera scroll-locks at wave EndX |
| 7.2 | Enemy spawn-walk | Enemies walk in from off-screen edges toward the player; the player stays on-screen (camera dead-zone works) |
| 7.3 | Enemy AI | Enemies chase when in range, idle otherwise, attack at ~70px range, back off / avoid the player properly; no walking through the player or world bounds |
| 7.4 | Scroll lock | Camera stays locked until every wave enemy is dead; then GO bell rings, GO indicator flashes, and scroll resumes |
| 7.5 | Mashing attacks | Hitbox connects during swing apex frames; enemies flinch (hurt animation) when struck |
| 7.6 | Attack 3 (heavy) | Knocks enemies down; they fall, pause, then get up; no double-damage or re-hit during knockdown |
| 7.7 | Pick up the bat (x≈350) | Player equips it; attacks swap to bat swing with bat visuals and reach; swinging arrows start to register hits at the bat's reach instead of the fist's |
| 7.8 | Pick up food | Player health increases (watch HUD); pickup is removed from the world |
| 7.9 | Oil drums | Solidity (player can't walk through); destroying a drum that has drop defs spawns its food pickup |
| 7.10 | Player death cycle | Health depletion → death animation → lives decrement → respawn with temporary invincibility (blink) → repeat until lives exhausted → GAME OVER |
| 7.11 | Retry from game over | Lives reset to 3, health full, level rebuilt, camera reset — no leftover enemies, hitboxes, or pickups from the previous run |
| 7.12 | Level completion | Reaching the level end (~3 backgrounds wide) fires LEVEL COMPLETE; level-complete music plays once |
| 7.13 | Player containment | Player cannot leave the level left/right bounds or the walkable band; no falling off-screen |
| 7.14 | Pause during a wave | World freezes, camera frozen, input mode switches to menu; resume continues cleanly (no double wave spawn, no stuck lock) |

## 8. Performance and stability

| # | Test | Expected |
| --- | --- | --- |
| 8.1 | Combat chaos (both waves + drums + explosions together) | Steady framerate; no visible stutter or hitch when SFX/hitboxes/pickups fire |
| 8.2 | FPS counter (debug overlay) | Shows stable values around the expected rate; virtual/actual/viewport sizes look correct |
| 8.3 | Repeated respawns and retries | No slowdown over time (enemy pool reused — no leak); animations still play |
| 8.4 | Long session | Title→play→pause→settings→title loops stay smooth; memory stays flat (watch debug overlay) |

## 9. Debug/dev tooling

| # | Test | Expected |
| --- | --- | --- |
| 9.1 | `~` during play | Debug overlay appears: FPS, state, wave (x/y, active count, locked), viewport virtual/actual, screen buffer, window size, BGs/entities drawn |
| 9.2 | `K` during play | Player dies instantly (debug); respects world behavior (game over after lives exhausted) |
| 9.3 | `C` during play | Level complete flow fires |
| 9.4 | Debug overlays | Player frame (blue/yellow when invincible), enemy AI frames colored by dominant force + distance rings + force label, active hitboxes (red rects during attack frames), wave trigger/end/level-end/walkable lines, weapon anchor marker + name |
| 9.5 | Debug overlay in menus/other states | No crash; text reflects the current game state |
