# Plan: Create `LEARNING.md` + wire it into `AGENTS.md`

## Goal

Add a root-level `LEARNING.md` that doubles as (1) a study guide for the design
patterns actually used in the code and (2) a glossary of the architecture-analysis
terminology used in review sessions. Then add one short rule to `AGENTS.md` telling
future sessions to keep `LEARNING.md` current when they add/remove patterns or
restructure the code.

This is documentation-only. No source code changes, no `dotnet build`/`dotnet test` needed.

## Decisions (confirmed)

- **Coverage:** both the constructive design patterns AND the analysis vocabulary.
- **Format:** a study guide, not a bare glossary. Each entry has four parts:
  1. **Concept** — one plain-language sentence.
  2. **Why it's used here** — the specific problem it solves in this game.
  3. **Where** — concrete `file:line` pointer(s) into the repo.
  4. **How to spot it** — a one-line recognition cue (and, for review terms, the
     companion "test" used to detect it).
- **Label** every entry as `[Pattern]` (thing we build with) or `[Review term]`
  (thing we critique with) so the two sets stay clearly separated.

## Content inventory (from the full codebase scan)

The implementing agent should translate each row below into one entry. File paths
are already verified; line numbers are approximate anchors — reconfirm before writing.

### Patterns (constructive)

1. **Composition root / dependency injection via delegates** — `GameLoop` wires
   everything (`GameLoop.cs:411-428`); `LevelDirectorCore` takes `createProp`/
   `createPickup`/`getWeapon`/`createEnemy`/`onEnemySpawned`/`getCameraView`
   delegates (`LevelDirectorCore.cs:59-86`) instead of referencing Game types.
   Same seam for `Func<WorldSnapshot>` (`EntityPool.cs:12`, `EnemyEntity.cs:75`).
2. **Capability interfaces (interface segregation, adjective-named)** —
   `IUpdatable`, `IRenderable`, `IDebugDrawable`, `IMoveable : ISpatial`,
   `IDamageable`, `IPickup`, `IPickupDropper`, `IHitboxProvider`, `IWeaponWielder`,
   `ICollisionLayer`, `IScreenRenderable`, `ISpatial`. Registered by capability
   probe in `EntityService.AddToTypedLists` (`EntityService.cs:116-130`).
3. **State pattern via Stateless FSM** — `StateMachineController<TState,TTrigger>`
   wrapper with guarded `Fire` + `Debug.WriteLine` (`StateMachines/
   StateMachineController.cs:24-32`); `GameStateMachine` (arcade shell),
   `PlayerStateMachine`, `EnemyStateMachine`; `ActorPhase` + `FirePhaseCompleted`
   (`CombatActorBase.cs:195-196`) bridge animation-completion back into the FSM.
4. **Object pooling (rent/return/sentinel)** — `EntityPool<TEnemy>`
   (`EntityPool.cs`): per-type stacks, sentinel position `(-99999,-99999)`, `Build`
   pre-allocation, `OnRentEnemy`/`OnReturnEnemy` hooks. Audio reuses
   `SoundEffectInstance` pools (`AudioService.cs:12` `PoolSize = 3`).
5. **Snapshot pattern (consistent per-frame world view)** — `WorldSnapshot`/
   `ActorSnapshot` readonly record structs (`AI/`); `EnemyAI.Update(in WorldSnapshot)`
   reads a stable snapshot; `LevelDirectorCore.PopulateSnapshots` fills pre-allocated
   `_enemyBuf`/`_propBuf` (`LevelDirectorCore.cs:123-145`).
6. **Steering behaviors** — `EnemyAI` seek/separation/avoidance/bounds forces,
   weighted + normalized + capped; `DominantForce` surfaced for debug
   (`EnemyAI.cs:111-168`).
7. **Strategy via delegate seam** (strategy without a formal interface) — the
   injected `Func`/`Action` factories above substitute for subclassing.
8. **Observer pattern (events)** — `Died`, `Destroyed`, `ActionTriggered`,
   `LevelCompleted`; the subscribe/unsubscribe symmetry around `SetAnimation`
   (documented MonoGame.Extended pitfall) in `CombatActorBase.PlayAnimation`
   (`CombatActorBase.cs:80-95`).
9. **Factory pattern** — static sprite factories `PlayerSprite.Create()` etc.
   (`AnimatedSprites/*`); `BackgroundRenderer.Create` (`BackgroundRenderer.cs:33`);
   content factories in the composition root (`GameLoop.CreateEnemy/CreatePickup/CreateProp`).
10. **Template Method / hook methods** — `CombatActorBase` abstract `Update`/
    `Phase`/`FirePhaseCompleted` + `protected virtual *Hook`/`ResetActor`/
    `TryHandleIncapacitatedUpdate`; subclasses override the hooks
    (`PlayerEntity.cs`, `EnemyEntity.cs`, `PropBase`, `PickupBase` subclasses).
11. **Generic base class with type constraint** — `LevelDirectorCore<TEnemy> where
    TEnemy : CombatActorBase, IPickupDropper`; `EntityPool<TEnemy> where TEnemy : Entity`.
12. **Capability registry / typed parallel lists (lightweight ECS)** — `EntityService`
    holds `_updatables`, `_renderables`, `_damageables`, `_movables`, `_props`,
    `_hitboxProviders`, `_debugDrawables`; `SortRenderablesByY` via `RenderableYComparer`.
13. **`ref readonly` + `in` for zero-copy large structs** —
    `LevelDirectorCore.CurrentWorld => ref _currentSnapshot` (`LevelDirectorCore.cs:57`).
14. **Zero-allocation buffers / pooled result lists** — `HitboxService._resultBuffer`/
    `_boundsBuffer` (`HitboxService.cs:15-16,42,96`); small values as
    `readonly record struct` (`DamageInfo`, `HitboxData`, `ActiveHitbox`, `AIUpdateResult`).
15. **Defensive programming (Debug.Assert / Debug.WriteLine)** — invariant checks in
    `Level.ValidateWaveDefs` (`Level.cs:29-50`), `CameraService.ComputeTargetX`
    (`CameraService.cs:33-35`), `AnimationFrameTracker.TryGetNewFrame`.
16. **Modern C#** — primary constructors (nearly every `Entity`/service),
    `required`/`init` properties (`MoveData`, `MeleeWeaponDef`), collection
    expressions (`[]` everywhere), record structs/positional records
    (`WaveDef`, `SpawnSide` enums, `ResolutionSetting`), default interface methods
    (`IDamageResponse.cs:6,9-10`), explicit interface implementation
    (`PropBase.cs:67-75`), nullable reference types, `InternalsVisibleTo`
    (`Core.csproj:7`).
17. **Virtual resolution / world-vs-screen rendering** — `GameCore` builds
    `BoxingViewportAdapter` + `OrthographicCamera` (`GameCore.cs:45-53`); `GameLoop`
    draws world space with `Camera.GetViewMatrix()` then UI with
    `ViewportAdapter.GetScaleMatrix()` (`GameLoop.cs:228,270`).

### Review terms (analysis vocabulary)

Each gets the companion detection test used in audits.

1. **God Class** — detection: "many responsibilities / references many namespaces."
   Example flagged: `GameLoop.cs` (~500 lines: wiring + frame orchestration + rendering).
2. **Feature Envy** — detection: "a method uses more of another class's members than
   its own."
3. **Law of Demeter / reach-through** — detection: "`.Foo.Bar.Baz`-style chains."
   Example flagged: `MenuService` index-casting into `ContainerRuntime.Children`
   (`MenuService.cs:69-83`).
4. **Leaky abstraction** — detection: "caller must understand the wrapped type's
   internals." Examples: `GumUiService.CreateScreen` contract; `HitboxService.ResolveHits`
   returning its mutable buffer.
5. **Circular dependency** — detection: "A references B and B references A (project or
   namespace)." This repo: verified none (`Game → Core`, `Tests → both`).
6. **Inheritance smells** — deep hierarchies overriding one method; base members
   existing for one subclass; abstract methods left empty; `new` hiding. Example
   flagged: `CombatActorBase` entry `*Impl` methods re-inlined by subclasses.
7. **Misplaced Core Class** — detection test from AGENTS.md: 'would I copy this file
   verbatim into a new 2D sidescroller?' / 'does Core reference Game?'.
8. **Coupling vs Cohesion** — the axis the whole audit grades on.
9. **Composition over inheritance** — delegates/factories instead of subclass plumbing.
10. **Duplication / single source of truth** — e.g. `INITIAL_LIVES` vs
    `PlayerEntity.InitialLives` (flagged).
11. **Dead code** — e.g. `EntityService._damageables` (written, never read) (flagged).

The learn-doc should also note that the audit vocabulary above is documented with
findings in `.kilo/plans/1787965629820-architecture-audit-findings.md` and cross-link it.

## LEARNING.md structure (recommended)

- Title + one-paragraph purpose ("reference for reviewing and learning the patterns
  and terms used in this codebase and in review sessions").
- `## Patterns` — one `### N. <name> [Pattern]` section per item above (the four-part
  template).
- `## Review terminology` — one `### N. <term> [Review term]` section per item above,
  including the detection test.
- `## How this file stays current` — short pointer to the AGENTS.md rule.
- Keep it terse; file pointers (`path:line`) over prose. Target ~2-4 lines history per
  entry; no exhaustive API listing.

## AGENTS.md update

Insert a bullet under `## Development Conventions` (near the end, after the
`MANUAL_TESTING.md` bullet):

```markdown
* **Learning Reference (`LEARNING.md`)**: `LEARNING.md` at the repo root is the
  living study guide of the design patterns used in the code and the
  architecture-analysis vocabulary used in reviews. When a session introduces a new
  pattern, adds a review term, or restructures the code in a way that invalidates an
  existing entry, update `LEARNING.md` in the same change so it stays a current,
  accurate guide.
```

## Execution steps

1. Create `LEARNING.md` at the repo root using the structure and inventory above.
   Reconfirm each `file:line` reference against the current code before writing
   (the codebase moves quickly); prefer stable anchors (type/method names) over
   brittle line numbers where a line number would go stale.
2. Cross-link the audit findings plan and `AGENTS.md` where relevant inside
   `LEARNING.md`.
3. Edit `AGENTS.md` to add the bullet above to `## Development Conventions`.
4. (Optional) Run the repo's Markdown linter if configured
   (`.markdownlint-cli2.jsonc` exists) — no `dotnet build`/`dotnet test` needed for a
   docs-only change.

## Validation

- `LEARNING.md` exists at repo root and every entry has all four parts (concept /
  why / where / how-to-spot) with at least one verified `file:line`.
- `AGENTS.md` contains the new `LEARNING.md` maintenance bullet.
- No source files changed; `git status` shows only `LEARNING.md` and `AGENTS.md`.

## Out of scope

- No code, no build/test runs, no restructure of the actual `file:line` smells
  flagged in the prior audit.
