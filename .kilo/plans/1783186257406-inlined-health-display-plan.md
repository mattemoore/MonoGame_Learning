# Inline HealthDisplay; move format to Health component

## Goal

Remove the `MonoGameLearning.Core.Rendering.HealthDisplay` static helper. Co-locate the display string format with the `Health` data component; inline the 3-line debug draw call into the two base classes that use it.

## Decisions (already resolved)

- `HealthDisplay.Format` becomes `Health.ToDisplayString()` (instance method, parameterless, uses `Value` / `MaxHealth`).
- `HealthDisplay.Draw` is inlined (3 lines) into `CombatActorBase.DrawDebug` and `PropBase.DrawDebug`.
- `HealthDisplay.cs` is deleted.
- Tests are retargeted to `Health.ToDisplayString()`.
- No new shared base class. No new interface method. No change to debug-only gating (caller remains `GameLoop` `IsDebug` branch).

## Affected files

| File | Change |
| --- | --- |
| `MonoGameLearning.Core/Entities/Components/Health.cs` | Add `public string ToDisplayString() => $"{Value}/{MaxHealth}";` |
| `MonoGameLearning.Core/Entities/CombatActorBase.cs` | In `DrawDebug` (around line 112-123), replace the `HealthDisplay.Draw(...)` call with the 3 inlined lines using `HealthComponent.ToDisplayString()` and `context.SpriteBatch` / `context.Font`. |
| `MonoGameLearning.Core/Entities/PropBase.cs` | In `DrawDebug` (line 41-46), same inlined replacement using its `HealthComponent`. |
| `MonoGameLearning.Core/Rendering/HealthDisplay.cs` | Delete file. |
| `MonoGameLearning.Game.Tests/HealthDisplayTests.cs` | Delete file. |
| `MonoGameLearning.Game.Tests/HealthTests.cs` | New file. 4 test cases ported: full, partial, zero, equal health. Use a small helper to seed non-`maxHealth` starting values (e.g., `var h = new Health(100); h.Subtract(70);` or expose a test seam if needed — prefer `Subtract` to keep the component sealed). |

## Inlined draw code (for both base classes)

```csharp
var text = HealthComponent.ToDisplayString();
var size = context.Font.MeasureString(text);
context.SpriteBatch.DrawString(
    context.Font,
    text,
    new Vector2(Frame.Center.X - size.X / 2, Frame.Top - size.Y - 2),
    Color.White);
```

`PropBase.cs` keeps `using MonoGameLearning.Core.Rendering;` for `DebugDrawContext` — do not remove the using.

## Test porting notes

- `HealthDisplay.Format(30, 30)` → `new Health(30).ToDisplayString() == "30/30"`.
- `HealthDisplay.Format(0, 100)` → `new Health(100) { ... Value = 0 ... }` — current `Health` has no public setter for `Value`. Use `Subtract(100)` from a `new Health(100)` to get `0/100`.
- `HealthDisplay.Format(6, 18)` → `var h = new Health(18); h.Subtract(12);` → `"6/18"`.

Confirm `Subtract` clamps to 0 (it does: `Value = Math.Max(0, Value - amount)`).

## Validation (mandatory per AGENTS.md pre-completion checklist)

1. `dotnet build` — must succeed.
2. `dotnet test` — all tests pass; no regressions.
3. Spot-check: no remaining references to `HealthDisplay` in the solution.

## Out of scope

- Any non-debug health rendering (HUD bars, damage numbers, etc.). Debug-only remains the only call site.
- Refactoring `IDamageable` or adding a `DamageableActorBase` shared base.
- Renaming `IDebugDrawable` / `DebugDrawContext`.
