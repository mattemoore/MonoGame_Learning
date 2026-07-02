---
name: audit-simplicity
description: Audit the entire codebase for unnecessary complexity — redundant abstractions, unused flexibility, superfluous branching, removable parameters, collapsible layers, and dead code. Actively seek out constraints or tradeoffs that could be imposed to simplify the code. Do not propose feature additions — only reductions and simplifications.
---

## Workflow

1. Map the project structure by reading the top-level directory trees:
   - `MonoGameLearning.Core/`
   - `MonoGameLearning.Core.Tests/`
   - `MonoGameLearning.Game/`
   - `MonoGameLearning.Game.Tests/`

2. Read every `.cs` file across all projects. Use `task` sub-agents in parallel for large projects.

3. For each file and each function/class/block, ask:
   **"Could this be simpler by shrinking its responsibility, removing a parameter, removing a branch, collapsing a hierarchy, or imposing a constraint/tradeoff that eliminates the need for flexibility?"**
   Concretely: *"What would I have to accept as never-changing (or as a hard limit) to delete code here?"*

   Focus on the following categories. The **tradeoff-amenable constraint** lens should be applied FIRST — before looking for any other type of simplification, ask whether accepting a limitation would make the whole problem disappear.

   ### Tradeoff-amenable constraints (APPLY FIRST)
   - **What can you accept as fixed?** — Can you hardcode a value, limit, or assumption that is currently parameterized but never actually varies? Every parameter that can be made constant is branching, state, and test surface you can delete.
   - **Examples of productive constraints in this codebase:**
     - "Only one player character in this game" → removes entity management branching, pooling code, multiplayer sync, player selection UI.
     - "Only one active enemy type per wave" → collapses enemy factory, AI dispatch, spawn logic.
     - "All entities occupy a single collision layer" → removes layer filtering, mask parameters, layer traversal loops.
     - "All attacks do a fixed damage value per type" → removes damage calculation pipeline, buff/debuff stacking, armor formulas.
     - "Screen width and height never change at runtime" → removes resize handling, dynamic viewport recalculation.
     - "Background is always a single static image per level" → removes parallax logic, layered scrolling, tilemap compositing.
     - "Maximum N enemies on screen at once" → removes dynamic pool sizing, overflow strategies, adaptive spawning.
     - "Animation always plays to completion (cannot be interrupted)" → removes interrupt logic, blend states, priority queues.
   - **Constraint patterns to look for:**
     - Any `enum` or `const` that has only one value used in production.
     - Any `interface` with a single implementation across the entire codebase.
     - Any `List<T>` or array that is always the same small size.
     - Any configuration/option that is set once at startup and never changed.
     - Any loop that iterates over a range but the range is always the same.
     - Any switch/if chain where all but one branch is dead in practice.
     - Any factory/strategy pattern with only one concrete product/strategy.
     - Any event that has only one subscriber, or any callback that is always the same lambda.
   - **The tradeoff test:** For each candidate constraint, ask: "If I accept this limitation, what code disappears? Is that a tradeoff the project can live with given its scope (a 90s-style beat 'em up)?" If yes, propose it.

   ### Unnecessary flexibility
   - Generics that are always instantiated with the same type argument.
   - Configurable behaviors, strategies, or callbacks that have only one consumer or one implementation.
   - Extensibility hooks (virtual methods, interfaces, events) that are never overridden, implemented by more than one class, or subscribed to.
   - Reflection-based dispatch that could be replaced with a direct call.
   - Parameters that accept a range of values but are only ever called with one value across the entire codebase.

   ### Superfluous branches
   - `if/else` or `switch` arms where all branches produce identical behavior or lead to the same next state.
   - Null checks on values that are never null at call sites (invariant established by construction).
   - Guard clauses at the top of methods that return early, when the guard condition is always false in practice.

   ### Removable parameters
   - Parameters that always receive the same argument from every caller.
   - Parameters that can be derived from existing state (e.g., `entity.Position` passed alongside `entity`).
   - Parameters that are only used in one branch and that branch is never taken.
   - `bool` or enum parameters that control behavior that could be split into two methods.

   ### Collapsible layers
   - Indirection (wrappers, facades, abstract base classes) that has only one implementation in practice.
   - Delegating methods that do nothing but forward to another method with the same signature.
   - Partial classes where all parts are in the same file and could be merged.
   - Extension methods that are only called from one place and could be inlined.

   ### Dead code
   - Fields, properties, methods, events, or inner classes that are never referenced (except by tests).
   - `TODO`, `HACK`, `FIXME`, or `WORKAROUND` comments pointing to code that is no longer needed.
   - Code behind `#if DEBUG` or `DEBUG` conditionals that is never exercised.
   - Unused `using` directives (beyond what the compiler warns about).
   - Exception types that are never caught.
   - `default` clauses in switches that are never reached.

   ### Debug/development-only code left in production paths
   - `Debug.WriteLine` / `Console.WriteLine` calls in non-debug code paths.
   - Diagnostic counters, timers, or tracing that accumulates state but is never inspected.
   - Test hooks (`InternalsVisibleTo`, conditional compilation for test seams) that are no longer needed.

4. For each candidate, verify by reading surrounding context and cross-referencing callers. A simplification that looks good in isolation may be wrong given how the code is actually used.

5. Do NOT suggest:
   - Style changes, formatting, or naming
   - Refactors that add new abstractions or layers
   - Feature additions or new capabilities
   - Performance micro-optimizations that add complexity
   - Architecture-level restructuring (that belongs in audit-architecture)

6. Report findings as follows. If no simplifications found: `NO_SIMPLIFICATIONS`

   ```
   ## <path>:<line>
   **Category:** (Unnecessary flexibility / Superfluous branching / Removable parameter / Collapsible layer / Tradeoff-amenable constraint / Dead code / Debug leftover)
   **Code:** (brief description of what the code does)
   **Why it can be simpler:** (why the flexibility/branch/parameter/indirection is unnecessary)
   **Suggested simplification:** (what to change, constrained direction)
   **Callers/usage:** (how many call sites or implementations exist, to support the claim)
   **Files to change:** (which files need editing)
   ```