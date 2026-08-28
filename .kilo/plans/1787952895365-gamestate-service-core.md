# [NOT IMPLEMENTED] Hoist GameStateService to Core

**Verdict: REJECT (already done)** — nothing to do.

## Evidence

`GameStateService` is already in Core:

- `MonoGameLearning.Core/GameStateService.cs` — `StateMachine<GameState, GameTrigger>` wrapper.
- `MonoGameLearning.Core/GameState.cs` — the `GameState` enum.
- `MonoGameLearning.Core/GameTrigger.cs` — the `GameTrigger` enum.

AGENTS.md already documents this:

> "GameStateService: Core-level arcade-shell FSM (GameState/GameTrigger via
> Stateless), owned by GameLoop."

`GameLoop` consumes it via `new GameStateService()` (`GameLoop.cs:88`) and
`_gameState.StateMachine.OnTransitioned(...)`.

## Decision

No plan. This TODO item is stale — the hoist has already been completed in a
prior session.

## Assumptions

- The question came from a backlog item that predates the current source layout.

## Follow-up questions (for a dedicated planning session)

- None — verify against `git log` if a record of *when* the hoist landed is desired.