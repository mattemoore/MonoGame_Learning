# [NOT IMPLEMENTED] Collapse GameStateService into StateMachineController

## Verdict: VALID (contained cleanup)

`GameStateService` (`Core/GameStateService.cs`) is a thin Stateless wrapper whose
`Fire` guard duplicates `StateMachineController<TState,TTrigger>.Fire`
(`Core/StateMachines/StateMachineController.cs:24-32`) — a parallel structure the
actor code already collapsed (the now-hoisted `StateMachineController` replaced
per-entity `Fire` logic in `PlayerStateMachine`/`EnemyStateMachine`).

The only behavioral difference is that `GameStateService.Fire` is *silent* on
illegal triggers, while `StateMachineController.Fire` logs via `Debug.WriteLine`.
All 11 `Fire` call sites in `GameLoop.cs` / `MenuService.cs` are state-guarded
first, so no dev-time log spam is expected in normal flows. The logging upgrade
matches the project's debug-diagnostics convention.

## Current shape

- `GameStateService` exposes `StateMachine` (raw Stateless machine), `State`,
  and a silent `Fire` — exactly what `StateMachineController` already provides.
- Consumers:
  - `GameLoop.cs:89` — `new GameStateService()`; `:90` —
    `.StateMachine.OnTransitioned(t => …)`; plus `.State` (lines 139–146, 162,
    167, 225, 260) and `.Fire` (146, 336, 424).
  - `MenuService.cs` — constructor + field typed `GameStateService`; `.State`
    and `.Fire` usages throughout.
  - `GameStateTests.cs` — `new GameStateService()` in `[SetUp]` (line 11).

## Tasks

1. **Add `Core/GameStateMachine.cs`**: a `static class GameStateMachine` with
   `public static StateMachineController<GameState, GameTrigger> Create()`
   (mirroring `EnemyStateMachine.Create`/`PlayerStateMachine.Create`). No
   `onInitialEntry` callback needed — `GameStateService` has none today, and the
   initial-state entry is a no-op (Stateless never fires it; the explicit-call
   pattern exists only for animations/SFX, which the game state machine lacks).
2. **Move the config**: transfer the six `sm.Configure(...)` blocks from
   `GameStateService`'s constructor into a private
   `Configure(StateMachine<GameState, GameTrigger> sm)` helper. Transitions are
   unchanged in full.
3. **Delete `Core/GameStateService.cs`**.
4. **Update `GameLoop.cs`**: change the field type to
   `StateMachineController<GameState, GameTrigger>` (add
   `using MonoGameLearning.Core.StateMachines;`), assign via
   `GameStateMachine.Create()`. `StateMachine.OnTransitioned` and `.State`/
   `.Fire` continue to work unchanged.
5. **Update `MenuService.cs`**: constructor parameter and readonly field typed
   `StateMachineController<GameState, GameTrigger>`; same using addition; pass
   `_gameState` as today.
6. **Update `GameStateTests.cs`**: field type and `Setup` → `GameStateMachine
   .Create()`. All existing assertions remain valid — every tested transition
   is permitted from its state; `InvalidTransition_ShouldBeIgnored` (line 67)
   still holds (ignored trigger simply logs via `Debug.WriteLine`).

## Behavior delta

- Illegal `Fire` calls now emit `Debug.WriteLine` instead of being silent.
  This is the *only* runtime change and is compiled out in Release.

## Validation

- `dotnet build --warnaserror` — 0 warnings.
- `dotnet test` — `GameStateTests` green unchanged plus no regressions.
- Optional manual: launch game, walk Title → Play → Pause → Settings → Title,
  and confirm no per-frame `Ignored …` Debug output during normal play.

## Assumptions

- Keeping the raw `StateMachine` property exposed on the controller is fine
  (`StateMachineController.StateMachine` is public); `OnTransitioned` in
  `GameLoop.cs:90` continues to work.
- No entry/exit callbacks are desired on the game states (none exist today;
  game-flow side effects live in the `OnTransitioned` handler).

## Follow-up questions

- None expected; this is a straight-line collapse. If a reason emerges to keep
  `GameStateService` as a named type (e.g., DI registration), the alternative is
  making it a thin factory — but the static `GameStateMachine` shape is the
  established pattern here.