# Plan: Floating health numbers above entities in debug mode

## Goal

Render each actor's and prop's remaining health as `health/maxHealth` text floating above its model in debug mode, drawn in world space inside the existing camera-transformed `SpriteBatch`. This is a simple 2D side-scroller iteration — no clipping, occlusion, billboarding, or 3D concerns.

## Verified context (from codebase)

- `GameLoop.Draw` opens `SpriteBatch.Begin(transformMatrix: Camera.GetViewMatrix())` and, inside `if (IsDebug)`, calls `entity.DrawDebug(SpriteBatch)` per entity. Debug rendering currently uses only `spriteBatch.DrawRectangle`.
- `GameCore` is already a static hub: `SpriteBatch`, `Content`, `Camera`, `GraphicsDevice`, `ViewportAdapter` are all `public static` set during init. Entities live in the same assembly (`MonoGameLearning.Core`) and can reference `GameCore` statics.
- `DrawDebug(SpriteBatch spriteBatch)` is the abstract debug entry on `Entity`; changing its signature would ripple to `SpatialEntity`, `ActorEntity`, `PropEntity`, `BackgroundEntity`, `Level`. Avoided.
- `ActorEntity` has **no** `Health`/`MaxHealth`. `PlayerEntity` and `EnemyEntity` implement `ICombatant` (Health/MaxHealth/IsAlive/Died). `PropEntity` carries `Health`/`MaxHealth`/`IsAlive` directly (only `IDamageable`).
- No `SpriteFont`/`DrawString` usage exists today. The only text rendering is Gum (`TextRuntime`), which is screen-space and unsuitable for in-world floating labels.
- A `DebugFont.spritefont` build was already proven to succeed once the XML declares `xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics"`. The earlier revert was by user choice, not a build failure.

## Decisions

1. **Rendering:** `SpriteBatch.DrawString` in world space (inside the camera-transformed begin/end). Text scrolls/tracks entities automatically; no world→screen projection needed.
2. **Font:** one `DebugFont.spritefont` (Arial, size 10) via the MonoGame content pipeline.
3. **Code placement:** per-entity `DrawDebug`, faithful to "like current draw debug works". Font reaches entities via a new `public static SpriteFont DebugFont` on `GameCore` (mirrors existing static-hub idiom; avoids touching the `DrawDebug` signature).
4. **Health access:** `ActorEntity.DrawDebug` draws text only when `this is ICombatant` (covers player + enemies in one place). `PropEntity.DrawDebug` uses its own `Health`/`MaxHealth`.
5. **Format/position:** `$"{health}/{maxHealth}"`, horizontally centered above `Frame` using `MeasureString`:
   `pos = new Vector2(Frame.Center.X - size.X/2, Frame.Top - size.Y - 2)`. Single color `Color.White`. No culling (matches existing `DrawDebug` which draws all entities). Always shown.
6. **Tests (AGENTS.md mandate):** pure `DrawString` rendering is not unit-testable without a `GraphicsDevice`. Extract a trivial pure `HealthDisplay.Format(health, max)` helper used by both draw sites and unit-test it. Font-dependent position math stays untested.

## Files to change

### NEW `MonoGameLearning.Game/Content/fonts/DebugFont.spritefont`

```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">
  <Asset Type="Graphics:FontDescription">
    <FontName>Arial</FontName>
    <Size>10</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#126;</End>
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
```

The `xmlns:Graphics` declaration is mandatory — without it MGCB fails with `Could not resolve type 'Graphics:FontDescription'`.

### EDIT `MonoGameLearning.Game/Content/Content.mgcb`

Append (after the `images/logo.png` block):

```monogame
#begin fonts/DebugFont.spritefont
/importer:FontDescriptionImporter
/processor:FontDescriptionProcessor
/processorParam:PremultiplyAlpha=True
/processorParam:TextureFormat=Compressed
/build:fonts/DebugFont.spritefont
```

### EDIT `MonoGameLearning.Core/GameCore/GameCore.cs`

Add a static font property alongside the existing statics (e.g. after `ViewportAdapter`):

```csharp
/// <summary>Globally accessible debug SpriteFont for in-world debug text.</summary>
public static SpriteFont DebugFont { get; set; }
```

### EDIT `MonoGameLearning.Game/GameLoop/GameLoop.cs`

In `LoadContent()`, immediately after `base.LoadContent();`:

```csharp
GameCore.DebugFont = Content.Load<SpriteFont>("fonts/DebugFont");
```

(`Content` here resolves to the static `GameCore.Content` via the existing `new` shadow.) Add `using MonoGameLearning.Core.GameCore;` is not needed in GameLoop (it already inherits `GameCore`).

### NEW `MonoGameLearning.Core/Entities/HealthDisplay.cs`

Pure, testable helper centralizing the format (used by both draw sites):

```csharp
namespace MonoGameLearning.Core.Entities;

public static class HealthDisplay
{
    public static string Format(int health, int maxHealth) => $"{health}/{maxHealth}";
}
```

### EDIT `MonoGameLearning.Core/Entities/ActorEntity.cs`

Add `using MonoGameLearning.Core.GameCore;` to imports. In `DrawDebug`, after the existing frame + hitbox drawing, append:

```csharp
if (GameCore.DebugFont is not null && this is ICombatant c)
{
    var text = HealthDisplay.Format(c.Health, c.MaxHealth);
    var size = GameCore.DebugFont.MeasureString(text);
    spriteBatch.DrawString(GameCore.DebugFont, text,
        new Vector2(Frame.Center.X - size.X / 2, Frame.Top - size.Y - 2), Color.White);
}
```

(`ICombatant` is already imported via `using MonoGameLearning.Core.Combat;`.)

### EDIT `MonoGameLearning.Core/Entities/PropEntity.cs`

Add `using MonoGameLearning.Core.GameCore;` to imports. In `DrawDebug`, after the existing frame drawing, append:

```csharp
if (GameCore.DebugFont is not null)
{
    var text = HealthDisplay.Format(Health, MaxHealth);
    var size = GameCore.DebugFont.MeasureString(text);
    spriteBatch.DrawString(GameCore.DebugFont, text,
        new Vector2(Frame.Center.X - size.X / 2, Frame.Top - size.Y - 2), Color.White);
}
```

### NEW `MonoGameLearning.Game.Tests/HealthDisplayTests.cs`

```csharp
using MonoGameLearning.Core.Entities;

namespace MonoGameLearning.Game.Tests;

[TestFixture]
public class HealthDisplayTests
{
    [Test]
    public void Format_ReturnsHealthSlashMax() =>
        Assert.That(HealthDisplay.Format(30, 30), Is.EqualTo("30/30"));

    [Test]
    public void Format_FullHealth() =>
        Assert.That(HealthDisplay.Format(100, 100), Is.EqualTo("100/100"));

    [Test]
    public void Format_ZeroHealth() =>
        Assert.That(HealthDisplay.Format(0, 100), Is.EqualTo("0/100"));

    [Test]
    public void Format_PartialHealth() =>
        Assert.That(HealthDisplay.Format(6, 18), Is.EqualTo("6/18"));
}
```

## Edge cases / notes

- `TestActorEntity` (test helper) inherits `ActorEntity` but does **not** implement `ICombatant`, so `this is ICombatant` is false — no health text drawn in tests. No impact.
- Dead enemies are removed from `_enemies` on death, so they stop drawing. A dead player (`0/100`) remains and shows `0/100` — informative for debug.
- Existing `DrawDebug` draws all entities with no camera culling; health text follows the same behavior. Cheap for the entity counts here.
- `GameCore.DebugFont` null-guard covers any path where `DrawDebug` could run before `LoadContent` (defensive; in practice debug draw only happens post-load).
- Single-pass white text is the baseline; a two-pass black-outline (offset black then white) is an optional future readability enhancement, intentionally omitted to keep this iteration simple.

## Validation checklist (mandatory, in order)

1. `dotnet build` — content pipeline compiles `DebugFont.spritefont`; solution builds clean.
2. `dotnet test` — all existing tests (194) plus the new `HealthDisplayTests` pass; no regressions.
3. Manual sanity: run game, toggle debug (`InputAction.Debug`), confirm `health/maxHealth` numbers float above the player, each enemy, and each oil drum, and track them as they move/collide.

## Risks

- Forgetting `xmlns:Graphics` in the `.spritefont` → MGCB `Could not resolve type` build failure. The XML above includes it.
- Static mutable `DebugFont` is a mild coupling smell, but it matches the existing `GameCore` static-hub pattern (`SpriteBatch`, `Content`, `Camera`), so it is idiomatic for this codebase.
- If `Arial` is unavailable on a build host, MGCB font import fails. Arial is present on the dev Windows host; if portability becomes a concern later, swap to an embedded bitmap font.
