# [NOT IMPLEMENTED] StateMachineController to Core; Keep ActorStateMachineCallbacks

**Verdict: MIXED**

- (a) "Callbacks still needed after phases?" → **REJECT removing them.** They are not obsolete.
- (b) "StateMachineController in Core?" → **VALID.** It is generic and should be hoisted.

## (a) Why ActorStateMachineCallbacks is still needed

`ActorStateMachineCallbacks` (`Game/StateMachines/ActorStateMachineCallbacks.cs`)
and `ActorPhase` (`Core/Entities/Actor/ActorPhase.cs`) serve **different, non-overlapping
purposes**:

- `ActorPhase` is a *normalized projection* of each entity's state (`EnemyState`/
  `PlayerState` mapped to `Phase` in `EnemyEntity.cs:29-38` / `PlayerEntity.cs:70-79`).
  `CombatActorBase` uses it for state-agnostic shared logic: `IsIncapacitated`,
  `IsInAttackingState` (weapon overlay + hitbox coordination), the fall/get-up
  knockdown sequence, and `FirePhaseCompleted` routing.
- `ActorStateMachineCallbacks` carries the *concrete entry/exit side effects* that
  differ per entity — which animation, which SFX, per-entry hitbox setup
  (`OnAttackingEntry` sets `CurrentMove`, `OnKnockdownEntry` drops the weapon,
  etc.). `ActorPhase` cannot express "play `EnemyAttackSwing` on attack entry."

Removing the callbacks would force entity-specific animation/SFX wiring back into
the shared base, which is exactly the coupling the abstraction prevents. Keep it.

*Optional (low priority)*: several callback members are pure pass-throughs to the
base hooks (`OnAttackingExit = OnAttackingExitHook`, `OnHurtEntry = OnHurtEntryHook`,
…). These could later be collapsed so the state machine configs call the hook
directly. Not a defect — skip unless another pass touches these files.

## (b) Tasks — hoist StateMachineController to Core

`StateMachineController<TState,TTrigger>` (`Game/StateMachines/StateMachineController.cs`)
is a generic Stateless wrapper (initial-entry invocation, `CanFire` guard +
`Debug.WriteLine` on ignored trigger, `IsInState`). It has zero game content and
parallels `GameStateService` (already in Core, already `using Stateless;`).

1. Move the file to `MonoGameLearning.Core/StateMachines/StateMachineController.cs`,
   namespace `MonoGameLearning.Core.StateMachines`.
2. Update `using` directives in `EnemyStateMachine.cs` and `PlayerStateMachine.cs`
   (`using MonoGameLearning.Core.StateMachines;` alongside the retained
   `using MonoGameLearning.Game.StateMachines;` for `ActorStateMachineCallbacks`).
3. Leave `ActorStateMachineCallbacks.cs` in Game (game wiring).
4. No `csproj` change needed (SDK-style globbing; Core already references Stateless).

## Validation

- `dotnet build --warnaserror`; `dotnet test`.
- `StateMachineControllerTests.cs` exercises the type indirectly via
  `PlayerStateMachine`/`EnemyStateMachine` — should remain green unchanged.

## Assumptions

- `StateMachineController` is intended as reusable infra (consistent with the
  project's "generic reusable core" goal), not game-specific.

## Follow-up questions (for a dedicated planning session)

- Should `GameStateService` be refactored to *use* `StateMachineController` to
  remove the duplicated `CanFire`-guard logic? (Currently `GameStateService.Fire`
  silently ignores unpermitted triggers, while `StateMachineController` logs them.)
- Do you want the optional callback-slimming (collapsing base-hook pass-throughs) done now or deferred?