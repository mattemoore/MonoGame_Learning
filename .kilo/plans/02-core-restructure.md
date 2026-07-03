# Core Directory Restructure

## Objective

Rename and reorganize directories within `MonoGameLearning.Core/` to better reflect file purpose and make the structure more intuitive. All directory renames include matching C# namespace changes.

**Executing order:** This plan assumes Finding 4 of `architecture-simplification.md` (move `OilDrumBehavior`/`OilDrumDamage` to Game project) has been completed first, so `Core/Combat/` no longer contains game-specific files.

---

## Change 1: Create `Core/Rendering/` — extract drawing-related files from `Entities/`

**Why:** `DebugDrawContext`, `RenderContext`, `HealthDisplay` (drawing helper), and `SpriteRenderer` are rendering utilities, not entities. They're consumed by both `Entities/` and `Game/` but don't belong in a subfolder of Entities.

### Move files

| Current path | New path |
|---|---|
| `Core/Entities/DebugDrawContext.cs` | `Core/Rendering/DebugDrawContext.cs` |
| `Core/Entities/RenderContext.cs` | `Core/Rendering/RenderContext.cs` |
| `Core/Entities/HealthDisplay.cs` | `Core/Rendering/HealthDisplay.cs` |
| `Core/Entities/Helpers/SpriteRenderer.cs` | `Core/Rendering/SpriteRenderer.cs` |

### Namespace changes

- `namespace MonoGameLearning.Core.Entities` → `namespace MonoGameLearning.Core.Rendering` (for DebugDrawContext, RenderContext, HealthDisplay)
- `namespace MonoGameLearning.Core.Entities.Helpers` → `namespace MonoGameLearning.Core.Rendering` (for SpriteRenderer)

### File-level updates needed

**Core/Entities/CombatActorBase.cs:**
- Remove `using MonoGameLearning.Core.Entities.Helpers;` (no longer needed — SpriteRenderer is imported via entity itself, but if it references `SpriteRenderer` directly, add `using MonoGameLearning.Core.Rendering;`)
- Replace `HealthDisplay.Draw(...)` → needs `using MonoGameLearning.Core.Rendering;`

**Core/Entities/PropBase.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for HealthDisplay references
- Add `using MonoGameLearning.Core.Rendering;` for SpriteRenderer (used as field type)

**Game/GameLoop/GameLoop.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for DebugDrawContext, RenderContext

**Game/Entities/Enemy/EnemyEntity.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for DebugDrawContext

**Game/Entities/GoIndicator/GoIndicatorEntity.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for DebugDrawContext, RenderContext

**Game/Levels/LevelDirector.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for DebugDrawContext

**Game/Levels/Level.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for DebugDrawContext

**Game/Rendering/BackgroundRenderer.cs:**
- Add `using MonoGameLearning.Core.Rendering;` for RenderContext

**Game.Tests/HealthDisplayTests.cs:**
- Change `using MonoGameLearning.Core.Entities;` → `using MonoGameLearning.Core.Rendering;`

**Game.Tests/AnimationFrameTrackerTests.cs:**
- No change — `AnimationFrameTracker` stays in Entities (see Change 5)

---

## Change 2: Rename `Entities/Helpers/` → `Entities/Components/`

**Why:** "Helpers" is a grab-bag name. These files are actual gameplay components: `Health`, `Mover`, `SpriteRenderer` (moved in Change 1), `EnemyAI`, `ActorSnapshot`, `WorldSnapshot`, etc. After Change 1 moves `SpriteRenderer` out, the remaining files are structural/data types that support entity behavior.

### Move directory

`Core/Entities/Helpers/` → `Core/Entities/Components/`

### Files in Components/ after the move

- `ActorSnapshot.cs`, `WorldSnapshot.cs` — data snapshots for AI queries
- `AIAction.cs` — enum for AI decisions
- `DominantForce.cs` — enum for AI steering forces
- `EnemyAI.cs` — AI logic
- `Health.cs` — health component
- `Mover.cs` — movement utilities

### Namespace change

`namespace MonoGameLearning.Core.Entities.Helpers` → `namespace MonoGameLearning.Core.Entities.Components`

### File-level updates needed

**Core/Entities/CombatActorBase.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Core/Entities/PropBase.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Game/Entities/Enemy/EnemyEntity.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Game/Entities/Player/PlayerEntity.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Game/Levels/LevelDirector.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Game.Tests/HitboxTests.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Game.Tests/EnemyEntityTests.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

**Game.Tests/ActorCollisionTests.cs:**
- Change `using MonoGameLearning.Core.Entities.Helpers;` → `using MonoGameLearning.Core.Entities.Components;`

---

## Change 3: Flatten `GameCore/` directory into root

**Why:** `GameCore/` contains a single file (`GameCore.cs`). A one-file directory is unnecessary nesting.

### Move file

`Core/GameCore/GameCore.cs` → `Core/GameCore.cs`

### Namespace change

`namespace MonoGameLearning.Core.GameCore` → `namespace MonoGameLearning.Core`

### File-level updates needed

**Game/GameLoop/GameLoop.cs:**
- Change `using MonoGameLearning.Core.GameCore;` → `using MonoGameLearning.Core;`

**Game/GameLoop/MenuManager.cs:**
- Change `MonoGameLearning.Core.GameCore.GameCore.Graphics` → `GameCore.Graphics` (if `using MonoGameLearning.Core;` is added, the fully-qualified ref is no longer needed)

**Game/Entities/GoIndicator/GoIndicatorEntity.cs:**
- Change `using MonoGameLearning.Core.GameCore;` → `using MonoGameLearning.Core;`

**Game/Levels/LevelDirector.cs:**
- Change `using MonoGameLearning.Core.GameCore;` → `using MonoGameLearning.Core;`

---

## Change 4: Move `UiBase` from `Entities/` to `UI/`

**Why:** `UiBase` is a base class for UI elements. It belongs alongside `DebugOverlay` and `GumManager` in the `UI/` directory.

### Move file

`Core/Entities/UiBase.cs` → `Core/UI/UiBase.cs`

### Namespace change

`namespace MonoGameLearning.Core.Entities` → `namespace MonoGameLearning.Core.UI`

### File-level updates needed

Any file that references `UiBase` by full namespace path (rare — it's typically imported via `using MonoGameLearning.Core.Entities`). No immediate `using` changes needed since `Core.Entities` still exists and `UiBase` was just one class in it. However, if any file does `using UiBase = ...`, that needs updating.

Search: `grep -rn "UiBase" --include="*.cs"` to verify no broken references.

---

## Change 5: Move `AnimationFrameTracker` from `Entities/` to `Entities/Components/`

**Why:** `AnimationFrameTracker` is a reusable animation component used by `CombatActorBase`. It belongs with other entity components.

### Move file

`Core/Entities/AnimationFrameTracker.cs` → `Core/Entities/Components/AnimationFrameTracker.cs`

### Namespace change

`namespace MonoGameLearning.Core.Entities` → `namespace MonoGameLearning.Core.Entities.Components`

### File-level updates needed

**Core/Entities/CombatActorBase.cs:**
- Add `using MonoGameLearning.Core.Entities.Components;` (or it's already added by Change 2)

**Game.Tests/AnimationFrameTrackerTests.cs:**
- Change `using MonoGameLearning.Core.Entities;` → `using MonoGameLearning.Core.Entities.Components;`

---

## Execution Order (within this plan)

1. Change 3 (GameCore flatten) — most independent, no dependency on other changes
2. Change 1 (Rendering/) — moves files out of Entities, reducing Entities/ clutter
3. Change 2 (Helpers→Components) — depends on SpriteRenderer gone from Helpers (done in Change 1)
4. Change 5 (AnimationFrameTracker→Components) — depends on Components/ existing (done in Change 2)
5. Change 4 (UiBase→UI) — independent, can be done anytime

## Validation

1. Run `dotnet build` — must compile with zero errors
2. Run `dotnet test` — must pass all tests

If the build fails, the most likely issues are:
- Missed `using` directive updates in test files
- `SpriteRenderer` references in `CombatActorBase` or `PropBase` that need the new `Core.Rendering` import
