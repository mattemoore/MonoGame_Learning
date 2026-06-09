# Codebase Simplification Plan

## Overview

Review of all 17 source files across 3 projects (`MonoGameLearning.Core`, `MonoGameLearning.Game`, `MonoGameLearning.Game.Tests`) to identify simplification opportunities in inheritance, event chains, state machines, and architecture.

---

## 1. Rename LogicalEntity (keep the layer)

**Current chain:** `Entity` → `LogicalEntity` → `ActorEntity` / `BackgroundEntity`

**Decision:** Keep `LogicalEntity` as the layer that adds `Frame` + debug rendering. `Entity` remains the truly abstract base (name, position, rotation, size). Entities without spatial bounds inherit `Entity`; entities that need positioned bounds inherit `LogicalEntity`.

**Action:** Rename `LogicalEntity` to a name that better communicates its role — `PositionedEntity` or `SpatialEntity`. The new name clarifies that this is the level where spatial awareness (Frame) enters the hierarchy.

**Files affected:** `LogicalEntity.cs` → rename, `ActorEntity.cs` (update base class reference), `BackgroundEntity.cs` (update base class reference).

---

## 2. Simplify PlayerStateController

**Current:** 12 states (`Dummy`, `Idling`, `Attacking`, `Attacking1`/`2`/`3`, `Moving`, `MovingLeft`/`Right`/`Up`/`Down`, `Hurt`, `Dying`, `Dead`), 11 triggers, 229 lines using Stateless with substates.

**Stateless pattern audit — all existing patterns are standard documented usage:**

| Pattern | Stateless Docs | Verdict |
|---------|---------------|---------|
| `Dummy` + `OnActivate` → `Fire(Activate)` → `Permit(Activate, Idling)` | 📄 "It is possible to work around this limitation by adding a dummy initial state, and then use Activate() to 'start' the state machine." | ✅ **Keep Dummy** — documented workaround |
| `.SubstateOf()` for Moving/Attacking hierarchies | 📄 First-class feature. "`IsInState(State)` will take substates into account." | ✅ Standard pattern, matches our `IsInState(PlayerState.Moving)` usage |
| `.Ignore()` to block irrelevant triggers in each state | 📄 "Firing a trigger that does not have an allowed transition... will cause an exception... To ignore triggers within certain states, use the Ignore(TTrigger) directive." | ✅ Standard — required to prevent exceptions |
| `.OnEntry(_ => ...)` / `.OnExit(_ => ...)` callbacks | 📄 "Entry/exit actions for states" with phone call example (StartCallTimer/StopCallTimer) | ✅ Standard |
| `.Permit()` transitions | 📄 Core feature, used throughout all examples | ✅ Standard |

**Real problems (architectural, not Stateless misuse):**
- **4 directional substates** (MovingLeft/Right/Up/Down) are nearly identical — only MovingLeft flips the sprite. The direction is data, not state behavior. Consolidating to 1 `Moving` state + `FacingDirection` enum/property reduces states by 3.
- **3 attack substates** (Attacking1/2/3) are identical — same subscribe/unsubscribe of `OnAnimationEvent`. The attack variant is data, not state behavior. Consolidating to 1 `Attacking` state + `AttackType` param reduces states by 2.
- **`PlayerStateControllerConfig`** with 16 nullable Action delegates is C#-level verbosity, not a Stateless concern. Stateless doesn't prescribe how to supply callbacks. This can be replaced with a simpler pattern (e.g., virtual methods on PlayerEntity, or passing a record with just the 3-4 unique animation callbacks).

**Proposed simplifications (keeping Stateless):**
1. Keep the `Dummy` state — documented Stateless pattern.
2. Keep `.Ignore()`, `.OnEntry/OnExit`, `.SubstateOf()`, `.Permit()` — all standard.
3. Reduce 4 directional substates → 1 `Moving` state. Sprite flip handled by a `FacingDirection` property on `PlayerEntity`, set directly in `PlayerEntity.Update()` based on input. Lose substate cross-permits (MovingLeft↔MovingRight etc.) — just set direction and stay in `Moving`.
4. Reduce 3 attack substates → 1 `Attacking` state. Store `AttackType` to pick the right animation. Entry/exit callback pattern (subscribe/unsubscribe `OnAnimationEvent`) fires once instead of 3 times.
5. Replace `PlayerStateControllerConfig` with targeted callbacks for the few truly unique behaviors (which animation to play per attack type), or virtual methods on `PlayerEntity`.

**After simplification — likely state count:** ~8 (`Dummy`, `Idling`, `Moving`, `Attacking`, `Hurt`, `Dying`, `Dead`) — a **33% reduction** from 12. Also eliminates ~30 lines of cross-permit + self-ignore boilerplate for the eliminated substates.

**Files affected:** `PlayerStateController.cs`, `PlayerEntity.cs`, `PlayerStateTests.cs`
**Risk:** Must retain equivalent `IsInState(PlayerState.Moving)` check in `PlayerEntity.Update()`. With `Moving` as a direct state (not a superstate), `IsInState` is trivial.

---

## 3. Centralize Animation Event Handling

**Current:** `PlayerEntity.CreateStateController()` subscribes to `Sprite.Controller.OnAnimationEvent` on entry of every attack/hurt/dying state (6 subscribe/unsubscribe pairs total).

**Stateless audit:** This is a MonoGame.Extended `AnimatedSprite` concern, not a Stateless one. The subscribe/unsubscribe pattern on `.OnEntry/OnExit` is a reasonable use of Stateless hooks, but it's repetitive.

**Solution:** Subscribe once in `PlayerEntity` constructor. The handler checks the current state and fires the appropriate trigger (`AttackCompleted`, `HurtCompleted`, `DeathCompleted`). Eliminates 6 subscribe/unsubscribe pairs and the associated risk of leaking subscriptions if exit callbacks are missed.

**Files affected:** `PlayerEntity.cs`

---

## 4. Unify InputManager Events

**Current:** 8 separate events (`Action1Pressed`, `Action2Pressed`, `Action3Pressed`, `BackPressed`, `DebugPressed`, `ConfirmPressed`, `DebugKillPressed`, `DebugCompletePressed`) + `MenuNavigated` Action.

**Problem:** Bloated public API surface. Consumers must subscribe to multiple events.

**Solution:** Replace all with `event Action<InputAction> ActionTriggered` where `InputAction` is an enum unifying all actions (including menu navigation). This reduces 9 subscription points to 1. The `MovementDirection` property stays separate since it's polled per-frame, not event-based.

**Files affected:** `InputManager.cs`, `GameLoop.cs`

---

## 5. Decompose GameLoop

**Current:** 326-line monolith handling input setup, camera clamping, collision, entity management, rendering, debug overlay, menu screens, state-change handling, and level management.

**Proposed extraction (2-3 classes):**
- **`CameraController`** — extract camera clamping logic (`GameLoop.cs:118-122`) + tracking.
- **`MenuManager`** — extract `BuildScreens`, `OnGameStateChanged`, `UpdateMenuCursor`, `OnMenuNavigated`, `OnConfirmPressed`, `OnBackPressed`. Encapsulates all `ContainerRuntime`/`TextRuntime` management for screens.

**Keep in GameLoop:** Root input subscription, entity lifecycle, collision update, level update dispatch, draw loop, and the main update/draw orchestration.

**Files affected:** `GameLoop.cs` + 2 new files in `MonoGameLearning.Game/GameLoop/`
**Tests affected:** None new required if behavior is preserved (existing tests only cover state machines and level validation).

---

## 6. Clean Up Level1 Constructor Pattern

**Current:** Primary constructor calls `CreateBackgrounds(content, gameWidth, gameHeight)` — a static method with 3 parameters needed because primary constructors can't reference instance members before base initialization.

**Solution:** Switch to a standard constructor body or use a `static Level1.Create(...)` factory method.

**Files affected:** `Level1.cs`

---

## 7. GameCore Singleton — Accept as-is

**Status:** Standard MonoGame pattern. Static access to `Graphics`, `SpriteBatch`, `Camera`, `Content` is idiomatic. Defer any changes until testing becomes a concrete pain point.

**No action recommended.**

---

## Implementation Order

| Order | Task | Files | Est. Effort | Test Impact |
|-------|------|-------|-------------|-------------|
| 1 | Simplify PlayerStateController (keep Stateless) + centralize animation events | `PlayerStateController.cs`, `PlayerEntity.cs`, `PlayerStateTests.cs` | High | Must update/modify ~30 test cases |
| 2 | Unify InputManager events | `InputManager.cs`, `GameLoop.cs` | Medium | None |
| 3 | Rename LogicalEntity → PositionedEntity/SpatialEntity | `LogicalEntity.cs`, `ActorEntity.cs`, `BackgroundEntity.cs` | Low | None |
| 4 | Decompose GameLoop (Camera + Menu) | `GameLoop.cs` + 2 new files | Medium | None |
| 5 | Clean up Level1 constructor | `Level1.cs` | Low | None |

**Total estimated effort:** ~300-500 lines changed/deleted across 10-13 files.