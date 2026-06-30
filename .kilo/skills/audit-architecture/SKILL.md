---
name: audit-architecture
description: Analyze the full codebase for architectural complexity — leaky abstractions, excessive coupling, circular dependencies, inheritance smells, and classes that know too much about other classes' internals. Suggest concrete simplifications.
---

## Workflow

1. Map the project structure by reading the top-level directory trees of each project:
   - `MonoGameLearning.Core/`
   - `MonoGameLearning.Game/`
   - `MonoGameLearning.Game.Tests/`
   
   Use `ls -R` or multiple `read` calls on each directory. Identify namespaces and layers (Entities, Combat, Levels, Sprites, Rendering, Input, GameLoop, etc.).

2. For each project, read every `.cs` file to build a mental model of types, their dependencies, base classes, and interfaces. Use `task` sub-agents in parallel for large projects.

3. Analyze for these specific smells:

   ### Leaky Abstractions
   - Does a base class expose internal details of its subclasses? (e.g., protected fields that are only needed by one subclass, virtual methods with non-obvious preconditions)
   - Does a class return internal mutable state that callers can modify? (e.g., exposing a `List<T>` directly instead of `IReadOnlyList<T>`)
   - Does a facade or wrapper class force callers to understand the wrapped component's internals?

   ### Excessive Coupling / Law of Demeter violations
   - Does class A reach through class B to access class C's internals? (e.g., `foo.Bar.Baz.DoSomething()` chains)
   - Does a class accept dependencies it doesn't use, or only passes through to another class?
   - Does a class in `MonoGameLearning.Game` directly reference internals of `MonoGameLearning.Core` that should be abstracted?

   ### Circular Dependencies
   - Does project A reference project B while B also references A? (check `.csproj` files)
   - Within a single project, does namespace X import namespace Y while Y imports X?
   - Does class A depend on class B, and B depends on A (directly or through a chain)?

   ### Inheritance Smells
   - Deep inheritance trees (3+ levels) where subclasses only override one method
   - Base classes with many protected members that exist only for one subclass
   - Abstract methods that most subclasses leave empty or throw `NotImplementedException`
   - Classes that inherit solely to access a single protected method
   - `new` modifier used to hide base members

   ### God Classes / Feature Envy
   - Classes with many responsibilities (e.g., both rendering and game logic, or both input handling and entity management)
   - Classes that reference many unrelated types across different namespaces
   - Methods that use more members of another class than of their own class

4. For each confirmed smell, propose a concrete simplification:
   - "Extract X into a separate class"
   - "Remove the base class and inline into the single subclass"
   - "Replace protected field with a private + protected accessor method"
   - "Break the cycle by introducing an interface in a shared layer"
- "Collapse the hierarchy — move the shared logic up and delete the subclass"
    - "Move the class from `MonoGameLearning.Game` to `MonoGameLearning.Core`"
    - Always prefer **removing code** over **adding new abstractions**.

### Misplaced Core Class
- Does a class in `MonoGameLearning.Game` contain generic engine-level logic (level infrastructure, enemy AI, combat systems, object pooling, sprite pipeline, camera behavior, input mapping, collision) that any 2D sidescrolling beat 'em up would need?
- Is the class's only dependency on Game project types something that could be pushed into a subclass or strategy, allowing the base logic to live in Core?
- Does a class in `MonoGameLearning.Game` inherit from a Core base class but override everything, suggesting the base class should absorb the logic (or the class should be promoted)?
- Does a class in `MonoGameLearning.Game` duplicate logic already present (or logically belonging) in `MonoGameLearning.Core`?

  **Test:** For every `.cs` file in `MonoGameLearning.Game`, ask: *"If I started a new 2D sidescroller today, would I copy this file verbatim?"* If yes, it belongs in Core.

  **Test:** For every `.cs` file in `MonoGameLearning.Core`, ask: *"Does this file reference anything from `MonoGameLearning.Game`?"* If yes, there is an inverted dependency — extract an interface or move the Game-specific subclass into Game. Core must never depend on Game.

5. Do NOT suggest:
   - Style or naming changes
   - Performance micro-optimizations
   - Adding new types, layers, or indirection unless removing an existing smell
   - Changes outside the scope of the smells listed above

6. Report findings as follows. If no smells found: `NO_ARCHITECTURAL_ISSUES`

   ```
   ## Smell: <category>
   **Location:** <path>:<line>
   **Problem:** (1-2 sentences describing the smell)
   **Impact:** (what maintenance or correctness risk this creates)
   **Suggestion:** (concrete simplification — what to remove, merge, or restructure)
   ```