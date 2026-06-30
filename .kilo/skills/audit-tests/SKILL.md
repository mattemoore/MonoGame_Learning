---
name: audit-tests
description: Audit every test in the solution for real-world fidelity — verify that test-time modifications (mocks, stubs, no-ops, constructor bypasses, overrides) don't reduce the tests' ability to catch real bugs and regressions.
---

## Workflow

1. Find all test files. Use `glob` to find `*Tests.cs` files inside `MonoGameLearning.Game.Tests/`.

2. Classify each test file into one of three fidelity tiers:

   - **Pure logic tests** — Test stateless functions, static methods, or value-type computations with zero game runtime dependencies. No mocks, no constructor bypasses, no partial interface implementations.
   - **State machine tests** — Test Stateless state machine configuration (transition tables, guard conditions, entry/exit callbacks). Use inline lambdas for callbacks, no game runtime dependencies.
   - **Gameplay integration tests** — Test entities, managers, controllers, or systems that require modifying production classes to function without a running game.

3. For each **gameplay integration test**, inventory every fidelity gap:

   ### Constructor bypasses
   - Does the test use `FormatterServices.GetUninitializedObject` to create an object without calling its constructor?
   - What does the constructor normally do that is being skipped? (state machine initialization, AI setup, event subscriptions, sprite warmup, field defaults)
   - Is there a test elsewhere that validates the constructor's behavior? If not, the constructor logic is untested.
   - **Alternative:** Can the test be rewritten to call the real constructor with a test double for the sprite dependency?

   ### No-op interface implementations
   - Does a test helper implement `IDamageable` with empty `OnDeath()`, `OnKnockdown()`, or `OnHit()` methods?
   - Does a test helper suppress `Died` with `#pragma warning disable CS0067`?
   - What real behavior is being lost? (state machine triggers, event firing, score tracking, debug warnings)
   - **Risk:** A regression that breaks damage → death → state transition logic would pass these tests because the no-op swallows the entire chain.
   - **Alternative:** Can the test helper delegate to a real implementation path (e.g., use `CombatActorBase` or a minimal subclass that calls the real state machine)?

   ### Override patterns that replace real behavior
   - Does a test subclass override a `protected virtual` method to skip real logic? (e.g., `OnRentEnemy` that skips `Reset()`, `InitializePool` that is a no-op, `CreateBackgroundRenderer` that returns null)
   - What real behavior is being skipped? (entity reset → state machine reset, health restore, sprite color change, AI re-initialization, content loading)
   - **Risk:** A regression in the skipped method would not be caught by this test suite.
   - **Alternative:** Can the override be removed by providing the real dependency via a test double instead?

   ### Null / dummy constructor arguments
   - Does the test pass `null!` for a constructor parameter (e.g., `new CameraController(null!, ...)`)?
   - Would new code added to that constructor or method cause a `NullReferenceException` that the test would miss?
   - **Risk:** If a future developer adds a `graphicsDevice.DoSomething()` call in the constructor or update method, it would NPE in production but pass the test.
   - **Alternative:** Can a lightweight test double be created instead of null?

   ### Partial interface contracts with no-op methods
   - Does a test helper implement an interface but leave most methods empty?
   - Which specific method bodies are empty that would be non-empty in production?
   - **Risk:** A regression that affects only the no-oped methods is invisible to the test.
   - **Alternative:** Can the test use a production class or a more complete test double that delegates to the real implementation?

4. For each gap, assess the concrete regression risk:

   | Risk Level | Criteria |
   |---|---|
   | **HIGH** | The skipped/overridden/no-oped logic is critical to gameplay (state transitions, damage application, death handling, AI behavior, collision response). A regression in that logic would produce a game-breaking bug (entity stuck, no damage, crash on death) and the test would still pass. |
   | **MEDIUM** | The skipped/overridden/no-oped logic is important but has some coverage from other tests, or the gap is narrow (e.g., a single method is no-oped but the overall flow is still validated). |
   | **LOW** | The skipped/overridden/no-oped logic is non-critical (debug drawing, logging, optional callbacks), or is independently tested by pure-logic tests elsewhere. |

5. Report findings in the following format. If no fidelity gaps found: `ALL_TESTS_REAL_WORLD_FIDELITY`

   ```
   ## <test_file_path>:<line>
   **Tier:** (Pure Logic / State Machine / Gameplay Integration)
   **Fidelity gap:** (what is being bypassed, skipped, or no-oped)
   **Risk level:** (HIGH / MEDIUM / LOW)
   **Risk description:** (what real-world bug or regression would pass undetected)
   **Suggested fix:** (how to close the gap — specific code change or alternative approach)
   ```

6. Do NOT suggest:
   - Removing tests or reducing coverage
   - Adding integration tests that require a running game (unless practical)
   - Changes that make tests significantly slower or harder to run
   - Style or naming changes