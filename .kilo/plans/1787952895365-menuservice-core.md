# [NOT IMPLEMENTED] Hoist MenuService to Core

**Verdict: REJECT** — MenuService is game UI content/flow, not reusable infra.

## Analysis

`MenuService` (`Game/GameLoop/MenuService.cs`, 340 lines) is dominated by:

- Hardcoded screen content: title `"BEAT 'EM UP"`, options `["Start Game", ...]`,
  `"PAUSED"`, `"GAME OVER"`, `"LEVEL COMPLETE!"`, and a hand-built settings screen
  with literal color/layout constants (`BuildSettingsScreen`, `MenuService.cs:194-311`).
- Game-specific menu *flow*: index 0/1/2 → Start/Settings/Exit semantics per state,
  and resolution/volume adjustments hardwired to `SettingsService`.

Its **reusable dependencies are already in Core**:

- `GumUiService` (generic screen primitive) — `Core/UI/GumUiService.cs`.
- `SettingsService`/`AudioSettings`/`ResolutionSetting` — `Core/Settings/`.
- `GameStateService`/`GameState`/`GameTrigger` — Core.

## Why hoisting is the wrong direction

A menu is exactly the kind of thing that differs per game (title, options,
settings shape). Hoisting it into Core would leak this game's copy/labels/flow
into the engine and *reduce* reusability — a different game would still write its
own menu. There is no shared generic menu engine to extract without inventing a
premature `ListMenu`/`Screen` abstraction that only this game uses.

## Decision

Leave `MenuService` in Game. The engine boundary is already correct: generic
Gum/Settings/GameState primitives in Core, concrete screens in Game.

## Assumptions

- The user is applying a "hoist every service to Core" heuristic; that heuristic
  is correct for infra services (Settings, GameState, Audio*) but not for
  presentation/flow that is definitionally game-specific.

## Follow-up questions (for a dedicated planning session)

- If the real motivation is "more than one game will share a menu shell," then a
  generic `MenuShell` (navigate/confirm/back handling over `GumUiService`) could
  be extracted later from the actual second game — not now (premature abstraction).