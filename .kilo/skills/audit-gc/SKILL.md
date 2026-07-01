---
name: audit-gc
description: Audit the entire codebase for garbage collection and allocation performance issues in gameplay-critical paths. Identify per-frame heap allocations, boxing, LINQ in hot paths, delegate allocations, params arrays, and other GC pressure sources that cause frame stutters.
---

## Workflow

1. Map the project structure by reading the top-level directory trees:
   - `MonoGameLearning.Core/`
   - `MonoGameLearning.Core.Tests/`
   - `MonoGameLearning.Game/`
   - `MonoGameLearning.Game.Tests/`

2. Read every `.cs` file across all projects. Use `task` sub-agents in parallel for large projects.

3. Focus audit on **gameplay-critical paths** — any code reached during `Update()`, `Draw()`, collision detection, input handling, entity spawning/despawning, state machine transitions, and AI updates. Library/test-only code that is not on these paths may have findings but with lower severity.

4. Check for these specific allocation patterns:

   ### LINQ in hot paths
   - `.Where()`, `.Select()`, `.Any()`, `.All()`, `.First()`, `.FirstOrDefault()`, `.Last()`, `.Count()` (on IEnumerable), `.OrderBy()`, `.ToArray()`, `.ToList()` in `Update()`, `Draw()`, or any per-frame method.
   - **Why it allocates:** LINQ allocates enumerators, and many methods allocate closures or intermediate collections.
   - **Fix:** Replace with `for`/`foreach` loops, pre-allocated buffers, or hand-rolled predicates.

   ### `foreach` over non-`IList<T>` causing enumerator allocation
   - `foreach` over raw `IEnumerable<T>`, `IEnumerable`, `IQueryable<T>`, `IOrderedEnumerable<T>`, `YieldInstruction` (in coroutines), or LINQ chains.
   - `foreach` over a `struct` that implements `IEnumerable<T>` via a generic interface (causes boxing of the struct enumerator).
   - **Fix:** Ensure the type is `List<T>`, `T[]`, `HashSet<T>`, `Dictionary<K,V>`, or another concrete collection where the C# compiler generates a `struct` enumerator. Or use a `for` loop with an index.

   ### Boxing
   - Value types (`int`, `float`, `Vector2`, `Rectangle`, enums, `struct`) passed to `object`, `interface`, or `Enum` parameters.
   - `string.Format()` or string interpolation with value-type arguments (boxes the values).
   - `ArrayList` or `Hashtable` usage (legacy non-generic collections).
   - `Enum.HasFlag()` (boxes the enum).
   - `Delegate.Combine` with struct delegates.
   - `Debug.WriteLine(object)` or `Console.WriteLine(object)` with value types.
   - **Fix:** Use generics, overloads accepting specific types, `nameof()`, or avoid boxing via conditional string building.

   ### Delegate/lambda allocations
   - Lambdas or anonymous methods in per-frame code (each lambda capture creates a new closure class instance).
   - Event subscriptions/unsubscriptions in `Update()` or per-frame methods.
   - Passing `new Action(...)` or `new Func<...>(...)` in hot paths.
   - `list.ForEach(x => ...)` — allocates a delegate and often a closure.
   - **Fix:** Cache delegates in static readonly fields, use method groups (`SomeMethod` instead of `x => SomeMethod(x)`), avoid captures.

   ### `params` array allocations
   - Methods with `params T[]` called in per-frame code (allocates a new array on every call).
   - **Fix:** Provide explicit overloads without `params`, or use `ReadOnlySpan<T>` overloads.

   ### String allocations
   - String concatenation in `Update()` / `Draw()` — each `+` creates a new string.
   - `string.Format()` / `StringBuilder` in per-frame code.
   - `ToString()` calls on value types in hot paths.
   - **Fix:** Use pre-computed strings, conditional lookup tables, or skip string building per-frame entirely.

   ### Temporary collection allocations
   - `new List<T>()`, `new Dictionary<K,V>()`, `new HashSet<T>()` created per frame.
   - `list.Add()` triggering internal resize (allocates new backing array).
   - `ToArray()` or `ToList()` on existing collections in per-frame code.
   - **Fix:** Pre-allocate collections with `Capacity`, reuse via `Clear()`, use `ArrayPool<T>`.

   ### Closure captures via anonymous methods / local functions
   - Local functions that capture local variables — each invocation allocates a closure object.
   - Lambda expressions in `Update()` / `Draw()` that reference `this` or local variables.
   - Task/async continuations in per-frame code.
   - **Fix:** Move the logic into a method that accepts the captured values as parameters, or cache the delegate.

   ### Implicit allocations in common MonoGame/MonoGame.Extended APIs
   - Frequent calls to `Vector2.Lerp`, `Matrix.Create*`, `BoundingBox` operations — many return structs but some internal paths may allocate.
   - `SpriteBatch.DrawString()` — allocates per call for glyph lookups unless using `SpriteFont` efficiently.
   - `ContentManager.Load()` — called per-frame (should only load once).
   - `AnimatedSprite.Update()` — may allocate animation frame arrays.
   - `OrthographicCamera` viewport recalculations.
   - **Fix:** Verify each call site, cache results, or use struct-based alternatives.

   ### Async / Task allocations in gameplay paths
   - `async Task` methods used in per-frame code (each `await` allocates state machine boxes if the type is `Task` rather than `ValueTask`).
   - `Task.Run()`, `Task.Delay()`, `Task.WhenAll()` in gameplay-critical paths.
   - **Fix:** Use `ValueTask` where possible, or avoid async entirely in `Update()`.

5. For each finding, assess severity:

   | Severity | Criteria |
   |---|---|
   | **CRITICAL** | Allocation occurs every frame in `Update()` or `Draw()` (or inner loop) for a non-trivial object (array, closure, LINQ enumerator). Will cause visible GC spikes. |
   | **HIGH** | Allocation occurs periodically but predictably (e.g., every time an enemy spawns, every hit, every state transition). Accumulates over time. |
   | **MEDIUM** | Allocation occurs rarely or on user-triggered events, but could be trivially fixed. |
   | **LOW** | Allocation occurs in non-critical paths (debug drawing, loading screens, editor-only code) or is a single small allocation that is hard to avoid. |

6. Do NOT suggest:
   - Premature optimizations in cold paths (loading, configuration, one-time setup)
   - Changes that significantly reduce code readability for marginal allocation savings
   - Adding object pooling for objects that are created fewer than ~100 times per second
   - Style or naming changes

7. Report findings as follows. If no allocation issues found: `NO_GC_ISSUES`

   ```
   ## <path>:<line>
   **Pattern:** (LINQ / Boxing / Delegate / params / String / Temp collection / Closure / MonoGame API / Async)
   **Code:** (what the code does — the hot path context)
   **Allocation:** (what is allocated and how often — per-frame, per-hit, per-spawn)
   **Severity:** (CRITICAL / HIGH / MEDIUM / LOW)
   **Fix:** (specific code change — use `for` loop, cache delegate, pre-allocate, use struct, etc.)
   ```