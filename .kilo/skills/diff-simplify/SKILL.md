---
name: diff-simplify
description: Examine staged and unstaged changes for simplification opportunities. Look for code that could be made simpler by adding constraints, accepting tradeoffs, or removing unneeded flexibility. Do not propose feature additions — only reductions and simplifications.
---

## Workflow

1. Run `git status --short` and `git diff --stat HEAD` to inventory what changed. If the diff is empty, report NO_CHANGES and stop.

2. Run `git diff HEAD` (staged + unstaged) to get the full diff. Also check for any new untracked files that are part of the change set.

3. For each changed function, class, or block in the diff, ask:  
   **"Could this be simpler by shrinking its responsibility, removing a parameter, removing a branch, or collapsing a hierarchy?"**  
   Focus on:
   - **Unnecessary flexibility** — Generics, configurable behaviors, extensibility hooks, or reflection that exist but aren't used by any consumer in the diff or the surrounding codebase. Can it be hardcoded or removed?
   - **Superfluous branches** — `if/else` or `switch` arms that currently lead to the same behavior. Can they be collapsed?
   - **Removable parameters** — Parameters that always receive the same argument, or could be derived from existing state.
   - **Collapsible layers** — Indirection (wrappers, facades, abstract base classes) that only has one implementation in practice.
   - **Tradeoff-amenable constraints** — Places where accepting a specific constraint (e.g., "only one enemy type right now", "only one collision layer", "always sentinel at -99999") lets you delete branching, parameters, or indirection.
   - **Removable state** — Fields, dictionaries, or lists that are written but never read, or only read in tests.

4. For each candidate, verify by reading the surrounding file context (not just the diff line). A simplification that looks good in isolation may be wrong given how the code is actually used.

5. Do NOT suggest:
   - Style changes, formatting, or naming
   - Refactors that add new abstractions or layers
   - Feature additions or new capabilities
   - Performance micro-optimizations that add complexity
   - Changes outside the diff scope

6. Report findings as follows:

   If no simplifications found: `NO_SIMPLIFICATIONS`

   Otherwise, for each finding:
   ```
   ## <path>:<line>
   **Code:** (brief description of what the code does)
   **Why it can be simpler:** (why the flexibility/branch/parameter is unnecessary)
   **Suggested simplification:** (what to change, constrained direction)
   **Files to change:** (which files need editing)
   ```