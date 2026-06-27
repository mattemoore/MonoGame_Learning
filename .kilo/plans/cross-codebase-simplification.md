# Cross-Codebase Simplification Plan (19 Candidates)

## Goal

Identify and document 19 simplifying constraints across the codebase (excluding items in `TODO.md`/`ROADMAP.md` and the previously-executed level/camera/combat plan in `.kilo/plans/level-camera-simplification.md`). For each, capture the **constraint**, the **code change**, and explicit **impact on current gameplay** vs. **impact on future extensions**. This document is a planning reference; each item is independently actionable and self-contained.

---

## How to Read This Plan

Each item is structured as:

- **Constraint** — the behavioral or structural assumption we'd adopt.
- **Code change** — what gets deleted/replaced.
- **Impact on current gameplay** — behavior delta for the existing skeleton.
- **Impact on future extensions** — how it helps or constrains adding features later (new enemy types, new prop types, new attack moves, new game states, new resolutions, etc.).

Items are grouped by area, with a summary table at the end. A consolidated implementation batch order appears at the bottom for the implementing agent.

---

## B1. HitboxService dedup consolidation

- **Constraint**: A single attack-instance produces exactly one hit per target. The service can dedup with a single map, not two parallel sets.
- **Code change**: Drop `_resolvedThisFrame` and `_attackResolvedTargets`. Track an `AttackInstanceId` (incrementing int) on each `ActiveHitbox`; dedup with `Dictionary<Entity, Dictionary<Entity, int>>` mapping owner → target → last attack instance that hit them. The `Clear` / `ClearAttackResolveState` / `ClearAll` collapse to a single `Clear(Entity owner)`.
- **Impact on current gameplay**: Identical observable behavior. `AdvanceFrameAndRegisterHitboxes` already calls `HitboxService.Clear(this)` before registering a new frame, which preserves the per-frame dedup semantics.
- **Impact on future extensions**: New attack types (multi-hit, piercing, DOT) become a single flag on the `ActiveHitbox` rather than a new dedup set. Adding `IFaction`-only targets (allies, summons) doesn't require juggling multiple sets.

## B2. IDamageable / ICombatant / IHasHealth consolidation

- **Constraint**: All damageable entities share one contract: take damage, expose health, react to hit/knockdown/death. The "two paths" (prop vs combatant) are an accident of history.
- **Code change**: Collapse `IDamageable` and `ICombatant` into one interface (`IDamageable` with `Faction`, `IsAlive`, `CanTakeDamage`, `OnHit`, `OnKnockdown`, `OnDeath`). `IHasHealth` becomes a `Health` accessor on `IDamageable`. `GameLoop` damage-application path becomes a single loop: `if (hit.Target is IDamageable d) CombatService.ApplyDamage(d, info);`. `CombatService.ApplyDamage` becomes the only entry point (currently `ICombatant.ReduceHealth` is called inside it AND `PropBase.TakeDamage` is called directly).
- **Impact on current gameplay**: No behavior change. Props and combatants follow identical hit → reduce → react flow.
- **Impact on future extensions**: Adding a new damageable entity type (breakable wall, NPC, ally NPC, boss) requires zero new interfaces. Faction-based hit routing (ally AI doesn't hurt ally NPCs) is centralized.

## B3. Make "props are always hittable" explicit

- **Constraint**: All `IDamageable` entities can be hit unless they explicitly opt out (e.g., during invincibility frames).
- **Code change**: Remove the `if (src is ICombatant && tgt is ICombatant && src.Faction == tgt.Faction)` check from `HitboxService.ResolveHits`. Move faction filtering to the source side: `ICombatant` declares `IReadOnlyList<Faction> HostileFactions`; `ResolveHits` skips a target if its faction isn't in that list. Or: pass an `IHostilityPolicy` into the service.
- **Impact on current gameplay**: No change. Player attacks hit props and enemies as before; oil drum is hittable.
- **Impact on future extensions**: Adding ally NPCs, summons, or neutral NPCs (shopkeepers) becomes declarative. No more "is this an ICombatant or not" branching.

## B4. Animation-event subscription wrapper

- **Constraint**: A single helper in `CombatActorBase` (or a `SpriteAnimation` wrapper class) ensures `OnAnimationEvent` is always wired to the *current* `Sprite.Controller` after every `SetAnimation()` call. Subscribers never re-subscribe manually.
- **Code change**: Introduce `protected void PlayAnimation(string key)` on `CombatActorBase` that wraps `Sprite.SetAnimation(key)` and re-subscribes `OnAnimationCompleted` to `Sprite.Controller.OnAnimationEvent` (idempotent: unsubscribe first). Replace every `Sprite.SetAnimation(...)` followed by manual `Subscribe/UnsubscribeToAnimationEvent` with `PlayAnimation(...)`. The 12+ `Subscribe/Unsubscribe` call sites in `PlayerEntity`, `EnemyEntity`, `OilDrumEntity`, and the entry/exit lambdas in `CombatActorBase` collapse. The `AGENTS.md` "MonoGame.Extended pitfall" section becomes obsolete.
- **Impact on current gameplay**: Identical behavior. Animation completion callbacks fire as today.
- **Impact on future extensions**: New states requiring animation completion (charge-up, special move, parry) are 1-line additions. The pitfall no longer lurks for future contributors.

## B5. KnockdownPhase → substate enum

- **Constraint**: Knockdown is a two-phase composite state (Falling → GettingUp), not a state with an internal integer counter.
- **Code change**: Replace `int KnockdownPhase` with `enum KnockdownPhase { Falling, GettingUp }` in `CombatActorBase`. `OnKnockdownEntry` sets `KnockdownPhase = Falling` and plays `FallAnimation`. The `OnAnimationCompleted` switch checks `KnockdownPhase == Falling` to transition to `GettingUp` (and plays `GetUpAnimation`). When the GetUp animation completes, fires `KnockdownCompleted`. The `KnockdownPhase = 0` reset in `ResetActor` and `KnockdownExit` becomes `KnockdownPhase = Falling`.
- **Impact on current gameplay**: No behavior change. Same two-animation flow with the same completion semantics.
- **Impact on future extensions**: Adding a third phase (e.g., a brief "stunned" hold after landing) is a one-line enum addition. Debug overlay can show current phase without a magic-number check.

## B6. Decouple animation name from MoveData lookup

- **Constraint**: The `CurrentMove` is set by the entity, not looked up by animation string. `MoveData` carries a `List<HitboxData>` keyed by frame index, not animation name.
- **Code change**: `MoveData` is constructed explicitly by the entity when starting an attack. Replace `CurrentMove = PlayerMoves.All[animKey]` with `CurrentMove = new MoveData { FrameHitboxes = ..., Damage = ..., Knockdown = ..., Strength = ... }` (or a factory `MoveData.LightPunch()`). `PlayerMoves`/`EnemyMoves` static dictionaries are deleted. Each attack button maps to a method that constructs its own `MoveData`.
- **Impact on current gameplay**: No behavior change. Same damage values, same hitbox placements per frame.
- **Impact on future extensions**: Adding a new attack move is a single method that constructs a `MoveData`. No risk of animation-name collisions silently swapping hit data. Designer-friendly: data lives next to the attack that uses it.

## B7. Drop `Entity.Rotation`

- **Constraint**: No entity needs rotation in the current skeleton.
- **Code change**: Remove `Rotation` property from `Entity`. Update `CombatActorBase.Render` and `PropBase.Render` to call `spriteBatch.Draw(Sprite, position, 0f, scale)` directly (drop the `MathHelper.ToRadians(Rotation)` call).
- **Impact on current gameplay**: Identical. No sprite is rotated today.
- **Impact on future extensions**: If rotation is ever needed (thrown barrel, spinning weapon), add it back as a `protected set` on a derived class (e.g., `RotatableEntity`) rather than a universal `Entity` field.

## B8. Entity.Width/Height immutability

- **Constraint**: Entity dimensions are set once at construction and never change.
- **Code change**: Change `Entity.Width/Height` from `public set` to `init`. Search and remove any runtime assignments (none expected after a grep).
- **Impact on current gameplay**: Identical (no runtime mutations exist).
- **Impact on future extensions**: Catches accidental dimension mutation in code review. If a "grow" effect is ever needed (power-up), it becomes a deliberate API addition (`Resize(newWidth, newHeight)` method) rather than silent field mutation.

## B9. Cache `Entity.Frame`

- **Constraint**: `Frame` is read often (per-frame, per-target, per-hitbox). The RectangleF should be cached and invalidated only when `Position`, `Width`, or `Height` changes.
- **Code change**: Add `private RectangleF _frame;` and `private bool _frameDirty = true;` to `Entity`. Override the `Position` setter and `Width/Height` setters (or wrap in `SetPosition`/`SetSize` methods). `Frame` getter returns `_frame` if clean, else recomputes. Alternative: use an `OnPositionChanged` event if a more reactive model fits.
- **Impact on current gameplay**: Identical externally; faster internally (`Frame` is read in `Render`, `DrawDebug`, `OnCollision`, `HitboxService.ResolveHits`, `Mover.ClampToBounds` — easily 10+ reads per frame per entity).
- **Impact on future extensions**: Free perf for any feature that reads `Frame` frequently (spatial queries, particle systems, AI sight checks). No code change required at call sites.

## B10. Drop `IHasHealth` and route through `Health` directly

- **Constraint**: `Health` is the single source of truth for entity health.
- **Code change**: Delete `IHasHealth`. Replace explicit interface impls `int IHasHealth.Health => HealthComponent.Value` with a public `Health HealthComponent { get; }` (currently protected). Anyone needing health reads `_entity.HealthComponent.Value`. Or expose `int Health => HealthComponent.Value` on `Entity` directly.
- **Impact on current gameplay**: Identical. The interface was only used in two places (PropBase explicit impl, CombatActorBase explicit impl, plus test fakes). Tests that implement `IHasHealth` on fakes can implement a property instead.
- **Impact on future extensions**: Simpler mental model: "an entity has a Health helper" instead of "an entity implements an interface that wraps a helper". Easier to add derived stats (max health scaling, shields, regen) without growing the interface.

## B11. EntityManager typed-list consolidation

- **Constraint**: Entities are bucketed by their interfaces, but the per-interface lists duplicate the iteration cost. Use a single `Entity` list and a `Dictionary<Type, List<IX>>` projection updated on register/unregister, OR keep the lists but have a single `Register` switch expressed as a `[CapabilityFlags]` enum.
- **Code change**: Replace the 9 `if (entity is IX) _listX.Add(x)` branches in `AddToTypedLists`/`RemoveFromTypedLists` with a switch on `[Flags] EntityCapabilities` declared on each interface (e.g., `[EntityCapabilities(Updatable | Renderable)] IUpdatable`). Or use source generators. Pragmatic: extract a `CapabilitySet` record (Dictionary<Type, int>) keyed on the interface type, populated once at registration.
- **Impact on current gameplay**: Identical. Per-frame iteration over `_renderables`, `_updatables`, etc. still works.
- **Impact on future extensions**: Adding a new interface (e.g., `IInteractable`, `ISoundEmitter`, `IPickup`) is one line in the capability enum instead of 2 lines in 2 methods. The pattern is explicit instead of implicit.

## B12. OilDrum duplicate animation lambda

- **Constraint**: The animation-selection logic is one function, not two.
- **Code change**: Extract `private string SelectAnimation() => HealthComponent.Value switch { <= 2 => Critical, <= 4 => Damaged, _ => Idle };` in `OilDrumEntity`. Both entry lambdas call `Sprite.SetAnimation(SelectAnimation())`.
- **Impact on current gameplay**: Identical. The two lambdas are byte-identical today.
- **Impact on future extensions**: Adding a 4th health tier (e.g., "smoking" at ≤ 1 HP) is one case in `SelectAnimation` instead of editing two lambdas.

## B13. OilDrumStateController → simple boolean

- **Constraint**: Two states and one trigger doesn't need a state machine library.
- **Code change**: Delete `OilDrumStateController`, `OilDrumState`, `OilDrumTrigger`. Replace with `bool _isHitStunned; float _hitStunTimer;` fields on `OilDrumEntity`. `TakeDamage` sets `_isHitStunned = true; _hitStunTimer = HitStunDuration;`. `Update` decrements the timer and clears the flag when expired. `Stateless` dependency reduced for the prop path.
- **Impact on current gameplay**: Identical observable behavior. Hitstun duration, visual states, and hit-during-stun rejection all preserved.
- **Impact on future extensions**: A simple prop with 2 states stays simple. If a prop needs a complex FSM (e.g., barrel that ignites then explodes), keep `Stateless` for *that* prop. Removes the precedent of "use Stateless for everything."

## B14. PlayerEntity.Attack1/2/3 → Attack(MoveData)

- **Constraint**: An attack is parameterized by its move data, not by method identity.
- **Code change**: Replace the three `Attack1/Attack2/Attack3` methods with `private void Attack(MoveData move)`. Each attack button (`Action1/Action2/Action3`) maps to a `MoveData` field assigned at construction. The `OnActionTriggered` switch passes the right `MoveData` to `_player.Attack(...)`.
- **Impact on current gameplay**: Identical. Same animations, same hitbox frames, same damage values.
- **Impact on future extensions**: Adding a new attack is adding a field, not a method. Combo systems ("press Action1 then Action2 within 200ms for an uppercut") become feasible by indexing into a `MoveData[]` history. Designer-driven attack rosters become realistic.

## B15. Animation-key overrides → IAnimationSet

- **Constraint**: Each entity type has a single set of animation keys, looked up by role (Idle, Run, Attack1, etc.).
- **Code change**: Replace the 6-7 abstract `protected override string IdleAnimation => ...` properties in `CombatActorBase` with a single `IAnimationSet` interface or a `record struct Animations(string Idle, string Run, string Hurt, string Fall, string Die, string GetUp, ...)`. Constructors inject the set. `PlayerEntity` adds `Attack1/Attack2/Attack3` to its set.
- **Impact on current gameplay**: Identical.
- **Impact on future extensions**: Adding a new character type (e.g., a boss with a unique run animation) is one struct, not 7 overrides. The "what animations does an entity need" contract is in one place.

## B16. EnemyStateController declarative table

- **Constraint**: State machine config is a data table (state × trigger → next state), not 200 lines of fluent calls.
- **Code change**: Define `Dictionary<(EnemyState, EnemyTrigger), EnemyState>` (transitions) and `Dictionary<EnemyState, Action>` (entry callbacks). Initialize both from `EnemyStateControllerConfig`. The 11 `.Ignore(...)` calls become a single "ignored triggers per state" lookup. The `Dummy` initial state + `OnActivate` are replaced with a `Lazy<StateMachine<...>>` that defers the `Activate()` call to first use.
- **Impact on current gameplay**: Identical state machine semantics. All `Ignore` rules preserved.
- **Impact on future extensions**: Adding a new state (e.g., `Blocking`, `ChargingAttack`, `Retreating`) is two entries in the config — one transition, one entry callback. Visualizing the state graph in a doc becomes trivial (the table is the doc).

## B17. InputManager unified key→action table

- **Constraint**: All key bindings (movement, gameplay, menu, debug) live in one declarative table with mode predicates.
- **Code change**: Replace `_keyActions` + `_movementKeys` + `_menuKeys` with a `List<(HashSet<Keys> keys, InputAction action, InputMode? mode)>` initialized in ctor. The `Update` method iterates once: for each entry, if any key was pressed and (mode is null or mode matches), fire the action. Movement direction is computed by accumulating all matching `Vector2` actions and normalizing.
- **Impact on current gameplay**: Identical. WASD/arrows move; U/I/O attacks; Enter/Space confirms; etc.
- **Impact on future extensions**: Adding key bindings is one tuple in the ctor. Gamepad binding (MonoGame.Extended supports it) is one new tuple type. The mode switch in `Update` is replaced by the data.

## B18. GameLoop.OnActionTriggered switch → action table

- **Constraint**: Input actions route to handlers via a table, not a switch.
- **Code change**: Replace the 35-line `switch (action)` in `OnActionTriggered` with `Dictionary<InputAction, Action> _actionHandlers` populated at construction. Each handler closes over `_player`, `_gameState`, `_menuManager`, `IsDebug`, etc.
- **Impact on current gameplay**: Identical.
- **Impact on future extensions**: Adding a new input action is adding one entry to the table. Reassigning keys (settings menu) becomes possible by repopulating the table.

## B19. GameLoop debug overlay resolution-independence

- **Constraint**: All debug-overlay positioning uses virtual (logical) coordinates, not hardcoded `GAME_WIDTH` constants.
- **Code change**: In `GameLoop.Draw`, replace `new Vector2(GAME_WIDTH - textSize.X - 20, GAME_HEIGHT / 2f - textSize.Y / 2f)` (the "GO ->" position) with `new Vector2(ViewportAdapter.VirtualWidth - textSize.X - 20, ViewportAdapter.VirtualHeight / 2f - textSize.Y / 2f)`. Audit the entire Draw method for hardcoded `GAME_WIDTH`/`GAME_HEIGHT` and route through `ViewportAdapter`. Also: `_debugWindow2.X = -200` and `Width = 200` use pixel coordinates that may not survive resolution changes — document as known or convert to Gum's `WidthUnits` system.
- **Impact on current gameplay**: The "GO ->" prompt at a different window resolution (e.g., a future 1280x720 window) is currently anchored to GAME_WIDTH=800, which is wrong if the virtual resolution changes. After the fix, it follows the viewport.
- **Impact on future extensions**: If the user adds a `RESOLUTION_WIDTH/HEIGHT` option to settings, the debug overlay survives. The "GO ->" prompt scales correctly with virtual resolution.

---

## Consolidated Impact Summary

| ID | Area | Current gameplay delta | Future extension leverage |
| --- | --- | --- | --- |
| B1 | HitboxService | None | New attack types without dedup-set proliferation |
| B2 | Interface consolidation | None | New damageable types: zero new interfaces |
| B3 | Faction explicitness | None | Declarative faction routing; NPCs/summons |
| B4 | Animation event wrapper | None | Eliminates the documented pitfall; new states trivial |
| B5 | KnockdownPhase enum | None | New knockdown phases (stunned) are one-liners |
| B6 | MoveData decoupling | None | New attacks: no animation-name collisions |
| B7 | Drop Rotation | None | Faster renders; add back only where needed |
| B8 | Width/Height init | None | Catches accidental mutation; forces explicit API |
| B9 | Frame cache | Identical, faster | Free perf for spatial queries |
| B10 | Drop IHasHealth | None | Simpler health model; room for shields/regen |
| B11 | EntityManager caps | None | New interfaces: one line instead of two |
| B12 | OilDrum lambda dedup | None | New health tiers: one case |
| B13 | OilDrum no-FSM | None | Use FSM only when prop warrants it |
| B14 | Attack(MoveData) | None | Combo systems; designer rosters |
| B15 | IAnimationSet | None | New characters: one struct, not 7 overrides |
| B16 | Declarative FSM | None | New states: 2 lines instead of 12 |
| B17 | Input table | None | New bindings: one tuple; gamepad-ready |
| B18 | Action handlers table | None | New input actions: one entry |
| B19 | Resolution-indep. draw | None at default res | Future resolution settings work; "GO ->" follows viewport |

---

## Implementation Batch Order (for the implementation agent)

Items are largely independent and can be tackled in batches. Each batch produces a self-contained, buildable, test-passing commit.

**Batch A — Quick wins (< 30 LOC each):** B7, B8, B9, B12, B19
**Batch B — Animation/State (touches `CombatActorBase`):** B4, B5, B6, B15, B16
**Batch C — Combat (touches `HitboxService` + interfaces):** B1, B2, B3
**Batch D — Entities:** B10, B11, B13
**Batch E — Input/Action:** B14, B17, B18

---

## Validation (per batch)

- `dotnet build` clean
- `dotnet test` clean (existing tests must still pass; new tests added per batch)
- Manual play-test at the end of each batch to catch behavior regressions
- The B4 batch specifically must verify the `AGENTS.md` pitfall pattern no longer lurks: write a regression test that calls `SetAnimation` twice and confirms the second animation's completion still fires

---

## Open Questions

1. **B14 (Attack(MoveData)) ordering**: should `OnActionTriggered` route through the action-handler table (B18) first, so the attack dispatch becomes a field lookup? Recommended: do B14 and B18 together.
2. **B11 (EntityManager)**: source generator or hand-rolled `[Flags]` enum? Source generators are more powerful but add a build dependency. Recommend hand-rolled for this skeleton.
3. **B9 (Frame cache)**: introduce `SetPosition`/`SetSize` methods (forces call-site updates) or use property setters with a dirty flag? Recommend property setters for minimal diff.