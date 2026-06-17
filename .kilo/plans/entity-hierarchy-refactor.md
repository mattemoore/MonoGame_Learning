# Plan: Address All TODO.md Items

## TODO Items

| # | Item | Status |
|---|------|--------|
| 1 | `SpatialEntity` as base for non-rendering entities — lighter-weight interface or flatter hierarchy? | Addressed by `PropEntity` + `TriggerEntity` below |
| 2 | `OilDrumEntity` extends `ActorEntity` with no movement/combat — lighter base needed? | Addressed by `PropEntity` below |
| 3 | `ActorEntity.TakeDamage` is virtual no-op — make abstract? | Addressed by making it `abstract` (user decision) |

---

## Target Hierarchy

```
Entity
  └── SpatialEntity (position, Frame, no-op Update, debug draw)
       ├── ActorEntity (IAnimated, IHitboxProvider, IMoveableEntity, ICollisionActor, IDamageable)
       │    ├── PlayerEntity : ActorEntity, ICombatant
       │    └── TestActorEntity (test)
       ├── PropEntity (ICollisionActor, IDamageable — Bounds→Frame, OnCollision, virtual TakeDamage)
       │    ├── OilDrumEntity (owns Sprite, state controller, damage logic)
       │    └── (future props: breakable barrels, crates, etc.)
       └── TriggerEntity (ICollisionActor — no push, no damage, just overlap detection)
```

`ActorEntity` is **unchanged** structurally — it stays a direct child of `SpatialEntity`. `PropEntity` is a **new sibling** under `SpatialEntity`. `TriggerEntity` is a skeleton to demonstrate the pattern.

---

## Changes

### 1. Make `ActorEntity.TakeDamage` abstract (TODO #3)

**File:** `MonoGameLearning.Core/Entities/ActorEntity.cs:95`

```diff
-    public virtual void TakeDamage(int amount, bool knockdown = false)
-    {
-    }
+    public abstract void TakeDamage(int amount, bool knockdown = false);
```

Forces every `ActorEntity` subclass to implement `TakeDamage`. No behavioral change for existing entities:
- `PlayerEntity.TakeDamage` already overrides it ✓
- `OilDrumEntity.TakeDamage` already overrides it ✓
- `TestActorEntity` (empty subclass in `ActorCollisionTests.cs:8`) — does NOT override `TakeDamage`. Must add an override or mark the class as `abstract`. Fix below.

### 2. Create `PropEntity.cs` (TODO #1 & #2)

**File:** `MonoGameLearning.Core/Entities/PropEntity.cs`

```csharp
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGameLearning.Core.Entities.Interfaces;

namespace MonoGameLearning.Core.Entities;

public abstract class PropEntity : SpatialEntity, ICollisionActor, IDamageable
{
    public IShapeF Bounds => Frame;

    protected PropEntity(string name, Vector2 position, int width, int height, float rotation = 0f)
        : base(name, position, width, height, rotation) { }

    public virtual void OnCollision(CollisionEventArgs collisionInfo)
    {
        Position -= collisionInfo.PenetrationVector;
    }

    public virtual void TakeDamage(int amount, bool knockdown = false) { }
}
```

Pulls `ICollisionActor` (Bounds, OnCollision) and `IDamageable` (TakeDamage) from `ActorEntity` into a shared sibling base. No sprite, no movement, no hitbox provider.

### 3. Modify `OilDrumEntity.cs` — change base to `PropEntity` (TODO #2)

**Before:** `OilDrumEntity : ActorEntity`
**After:** `OilDrumEntity : PropEntity`

Additions needed (was inherited from `ActorEntity`, now owned directly):
- `AnimatedSprite Sprite` property
- `Draw(SpriteBatch)` override — render sprite at Position with Scale
- `DrawDebug(SpriteBatch)` override — render hitbox overlay + frame (optional, from ActorEntity.DrawDebug)
- `ICollisionActor` implementation already provided by `PropEntity` ✓

Update constructor: instead of passing `sprite` to `ActorEntity`'s base (which computed width/height from sprite size * scale), compute width/height from sprite manually and pass to `PropEntity`'s `(name, position, width, height)`.

### 4. Skeleton: `TriggerEntity` (demonstrates the pattern)

**File:** `MonoGameLearning.Core/Entities/TriggerEntity.cs`

```csharp
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace MonoGameLearning.Core.Entities;

public class TriggerEntity(string name, Vector2 position, int width, int height)
    : SpatialEntity(name, position, width, height)
{
    public IShapeF Bounds => Frame;

    public void OnCollision(CollisionEventArgs collisionInfo)
    {
        // Empty — detect but don't push
    }
}
```

Key points:
- Inherits `SpatialEntity` directly (sibling of `ActorEntity` and `PropEntity`)
- Implements `ICollisionActor` implicitly via `Bounds` and `OnCollision` — can be inserted into `CollisionComponent`
- `OnCollision` is empty: no push-apart, no damage — just overlap detection
- No `Sprite`, no `TakeDamage`, no movement
- Can be extended with events (`OnEntered` / `OnExited`) when game loop detects overlap state changes
- Renders nothing — debug draw inherited from `SpatialEntity` (frame outline)

### 5. Fix `TestActorEntity` — add `TakeDamage` override (TODO #3 fallout)

**File:** `MonoGameLearning.Game.Tests/ActorCollisionTests.cs:8`

`TestActorEntity` is an empty subclass of `ActorEntity`. With `TakeDamage` becoming `abstract`, it must provide an override:

```csharp
public class TestActorEntity(string name, Vector2 position, int width, int height)
    : ActorEntity(name, position, width, height)
{
    public override void TakeDamage(int amount, bool knockdown = false) { }
}
```

### 6. GameLoop list changes (TODO #2 fallout)

`GameLoop._actorEntities` is `List<ActorEntity>`. Oil drums are no longer `ActorEntity`, so they can't live in this list.

Options:
- **Split into two lists**: `List<ActorEntity> _actors` and `List<PropEntity> _props`. Iterate both in update/draw/collision loops.
- **Widen to `List<SpatialEntity>`**: single list for all spatial entities, then filter by type where needed.
- **Widen to `List<Entity>`**: simplest, already have `_entities` list.

Likely best: keep `_actorEntities` for actors only (players), add `List<PropEntity> _props` for props. Iterate both in update/draw/collision loops, and merge them into `_entities`.

### 7. Optional: Migrate test entities (TODO #1)

- `TestSpatialEntity` (HitboxTests) — change from `SpatialEntity, ICombatant` to `PropEntity, ICombatant`. Gets `TakeDamage` + `OnCollision` for free.
- `TestDamageableEntity` (OilDrumStateTests) — change from `SpatialEntity, IDamageable` to `PropEntity`. Gets `virtual TakeDamage` to override, `OnCollision` for free.

---

## Implementation Order

1. Make `ActorEntity.TakeDamage` abstract
2. Fix `TestActorEntity` — add empty `TakeDamage` override
3. Create `PropEntity.cs`
4. Modify `OilDrumEntity.cs` — change base to `PropEntity`
5. Create `TriggerEntity.cs` skeleton
6. Modify `GameLoop.cs` — add prop list
7. Migrate test entities (optional)
8. Build + test

---

## Risks

- **OilDrumEntity sprite advancement**: `ActorEntity.Update()` uses `AnimationFrameTracker.AdvanceOnFrameChange()` to step the sprite. Oil drums use looping animations at 0.1f per frame. Need to call `Sprite.Update(gameTime)` directly in `OilDrumEntity.Update()` — verify this is sufficient for frame advancement.  Consider extending `IAnimated` to make this work.
- **Bounds consistency**: `ActorEntity` sizes the entity to `sprite.Size * scale`. `PropEntity` takes explicit width/height. `OilDrumEntity` must compute these in its constructor.  Make `PropEntity` use same params in constructor i.e. `sprint.Size * scale` in constructor.
- **GameLoop.draw() iterates `_actorEntities`**: oil drums rendered here. Must add prop rendering loop.  Create a new array for `_propEntities` and loop through it when rendering.
