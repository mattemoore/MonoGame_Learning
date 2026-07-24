# Pickup Item System — Phase 1

## Goal

Add the plumbing for collectible pickup items, one concrete food-heal pickup that restores 15 HP, and a static spawn in Level1 between wave 0 and wave 1. Pickups scroll into view as part of the level (no pop-in), do not take damage, render a static sprite, and are removed from the game on player overlap with a sound effect.

**Out of scope:** drop tables on death, weapon/score/life pickups, pickup animation/bob/fade/timer, weapon systems, ROADMAP.md updates.

---

## Decisions (resolved)

1. **PickupBase is a new abstract class** extending `Entity` — not `PropBase`. Props force `IDamageable` + pushback-layer collision; pickups need overlap, not push.
2. **New "pickups" collision layer** in `CollisionWorld2D`. Enable with `"actors"` for overlap queries. Manual overlap in `GameLoop` (not `MTV`-based) so the player isn't pushed back.
3. **`IDamageable.Heal(int amount)`** added to the interface. `Health.Add(int amount)` clamps to `MaxHealth`. `CombatActorBase` delegates to `HealthComponent.Add`. `PropBase` gets an explicit no-op `Heal`.
4. **Sprite**: `Content/images/food_apple.png` already exists and is wired into `Content.mgcb` (lines 77+). Loaded as `Texture2D` via `Content.Load<Texture2D>("images/food_apple")`.
5. **Separate `Level.Pickups` list** + `LevelDirector.SpawnPickups()`, mirroring `Props` / `SpawnProps`.
6. **PickupBase implements** `IRenderable`, `IDebugDrawable`, `ICollisionActor`, `IPickup`.
7. **`IPickup.OnPickup(IDamageable target)`** is the effect hook — `FoodPickupEntity` overrides it to heal.
8. **Audio**: `SfxId.PickupHeal` already exists. Add a placeholder `audio/pickup_heal.wav` and wire it into `AudioManager.LoadContent` and `Content.mgcb`. `AudioManager.PlaySfx` for an unloaded id is a safe no-op (existing pattern).

---

## Implementation steps

### Core

1. **`MonoGameLearning.Core/Entities/Interfaces/IPickup.cs`** (new)

   ```csharp
   namespace MonoGameLearning.Core.Entities.Interfaces;

   public interface IPickup
   {
       void OnPickup(IDamageable target);
   }
   ```

2. **`MonoGameLearning.Core/Entities/PickupBase.cs`** (new)

   ```csharp
   using Microsoft.Xna.Framework;
   using Microsoft.Xna.Framework.Graphics;
   using MonoGame.Extended.Collisions;
   using MonoGameLearning.Core.Entities.Interfaces;
   using MonoGameLearning.Core.Rendering;

   namespace MonoGameLearning.Core.Entities;

   public abstract class PickupBase(string name, Vector2 position, Texture2D texture)
       : Entity(name, position, texture.Width, texture.Height),
         IRenderable, IDebugDrawable, ICollisionActor, IPickup
   {
       public int Id => GetHashCode();
       protected Texture2D Texture { get; } = texture;

       public CollisionShape2D Shape => new(new BoundingBox2D(
           new Vector2(Frame.X, Frame.Y),
           new Vector2(Frame.Right, Frame.Bottom)));

       public void Render(RenderContext context)
       {
           context.SpriteBatch.Draw(Texture, Position, null, Color.White,
               0f, new Vector2(Texture.Width / 2f, Texture.Height / 2f),
               1f, SpriteEffects.None, 0f);
       }

       public void DrawDebug(DebugDrawContext context) =>
           context.SpriteBatch.DrawRectangle(Frame, Color.Yellow);

       public abstract void OnPickup(IDamageable target);
   }
   ```

3. **`MonoGameLearning.Core/Entities/Components/Health.cs`** — add `Add`:

   ```csharp
   public void Add(int amount) => Value = Math.Min(MaxHealth, Value + amount);
   ```

4. **`MonoGameLearning.Core/Entities/Interfaces/IDamageable.cs`** — add `void Heal(int amount);`

5. **`MonoGameLearning.Core/Entities/CombatActorBase.cs`** — add:

   ```csharp
   void IDamageable.Heal(int amount) => HealthComponent.Add(amount);
   ```

6. **`MonoGameLearning.Core/Entities/PropBase.cs`** — add (props can't be healed):

   ```csharp
   void IDamageable.Heal(int amount) { }
   ```

7. **`MonoGameLearning.Core/Entities/EntityManager.cs`** — wiring:
   - Add `private readonly List<ICollisionActor> _pickupCollidables = [];` and `public IReadOnlyList<ICollisionActor> PickupCollidables => _pickupCollidables;`
   - In `AddToTypedLists`: BEFORE the existing `if (entity is CombatActorBase actor)` branch, add:

     ```csharp
     else if (entity is PickupBase pickup)
     {
         _pickupCollidables.Add(pickup);
         world.Insert(pickup, "pickups");
     }
     ```

   - In `RemoveFromTypedLists`: mirror the above with `_pickupCollidables.Remove(...)` + `world.Remove(...)`.
   - In `Clear()`: also `_pickupCollidables.Clear()`.

### Game

1. **`MonoGameLearning.Game/Entities/Pickups/FoodPickupEntity.cs`** (new)

   ```csharp
   using Microsoft.Xna.Framework;
   using Microsoft.Xna.Framework.Graphics;
   using MonoGameLearning.Core.Entities;
   using MonoGameLearning.Core.Entities.Interfaces;

   namespace MonoGameLearning.Game.Entities.Pickups;

   public class FoodPickupEntity : PickupBase
   {
       public const int HealAmount = 15;

       public FoodPickupEntity(string name, Vector2 position, Texture2D texture)
           : base(name, position, texture) { }

       public override void OnPickup(IDamageable target) => target.Heal(HealAmount);
   }
   ```

2. **`MonoGameLearning.Game/AnimatedSprites/FoodPickupSprite.cs`** (new) — follows `GoIndicatorSprite` pattern:

   ```csharp
   using Microsoft.Xna.Framework.Content;
   using Microsoft.Xna.Framework.Graphics;

   namespace MonoGameLearning.Game.AnimatedSprites;

   public static class FoodPickupSprite
   {
       private const string AssetPath = "images/food_apple";
       private static Texture2D _texture;

       public static void Load(ContentManager content) => _texture = content.Load<Texture2D>(AssetPath);
       public static Texture2D Texture => _texture;
   }
   ```

3. **`MonoGameLearning.Game/Levels/PickupSpawnDef.cs`** (new)

    ```csharp
    using Microsoft.Xna.Framework;

    namespace MonoGameLearning.Game.Levels;

    public record PickupSpawnDef(string Type, Vector2 Position);
    ```

4. **`MonoGameLearning.Game/Levels/Level.cs`** — add abstract property:

    ```csharp
    public abstract List<PickupSpawnDef> Pickups { get; }
    ```

5. **`MonoGameLearning.Game/Levels/Level1.cs`** — add override:

    ```csharp
    public override List<PickupSpawnDef> Pickups =>
    [
        new PickupSpawnDef("Food", new Vector2(1400f, 556f)),
    ];
    ```

    (Y=556 is best-effort — verify visually, adjust ±10px.)

6. **`MonoGameLearning.Game/Levels/LevelDirector.cs`** — add `SpawnPickups`:

    ```csharp
    public void SpawnPickups(List<PickupSpawnDef> pickupDefs)
    {
        foreach (var def in pickupDefs)
        {
            Entity pickup = def.Type switch
            {
                "Food" => new FoodPickupEntity(def.Type, def.Position, FoodPickupSprite.Texture),
                _ => throw new ArgumentException($"Unknown pickup type: {def.Type}", nameof(pickupDefs)),
            };
            _entityManager.Register(pickup);
        }
    }
    ```

7. **`MonoGameLearning.Game/GameLoop/GameLoop.cs`** — three additions:
    - **`LoadContent`**: add `FoodPickupSprite.Load(Content);` next to `PlayerSprite.Load(Content);`
    - **`CreateCollisionWorld`**: add `pickups` layer + enable with `actors`:

      ```csharp
      var pickupSpace = new QuadTreeSpace(bb);
      world.AddLayer("pickups", new Layer(pickupSpace));
      world.EnableCollisionBetweenLayers("actors", "pickups");
      ```

    - **`Update`**: after `ResolveCollisions()`, call a new `ResolvePickupOverlaps()`:

      ```csharp
      private void ResolvePickupOverlaps()
      {
          var pickups = _entityManager.PickupCollidables;
          for (int i = 0; i < pickups.Count; i++)
          {
              var p = pickups[i];
              if (p is not IPickup pickup) continue;
              if (!p.Frame.Intersects(_player.Frame)) continue;
              if (_player.HealthComponent.IsAlive)
                  pickup.OnPickup(_player);
              _audio.PlaySfx(SfxId.PickupHeal);
              _entityManager.Destroy((Entity)p);
          }
      }
      ```

    - **`ReinitLevel`**: add `_levelDirector.SpawnPickups(_currentLevel.Pickups);` next to existing `_levelDirector.SpawnProps(...)` call.

8. **Audio wiring**:
    - **`MonoGameLearning.Core/Audio/AudioManager.cs`** — add to `LoadSfxGroup(...)` call:

      ```csharp
      (SfxId.PickupHeal, "audio/pickup_heal"),
      ```

    - **`Content/audio/pickup_heal.wav`** (new) — small placeholder WAV file.
    - **`Content/Content.mgcb`** — add an entry mirroring `audio/attack_swing1.wav`:

      ```csharp
      #begin audio/pickup_heal.wav
      /importer:WavImporter
      /processor:SoundEffectProcessor
      /processorParam:Quality=Best
      /build:audio/pickup_heal.wav
      ```

### Tests

1. **`HealthTests.cs`** — add:
    - `Add_BelowMax_IncreasesValue`
    - `Add_AboveMax_ClampsToMax`
    - `Add_Zero_NoChange`

2. **`FoodPickupEntityTests.cs`** (new) — use a hand-rolled `IDamageable` stub (no `Entity` needed for unit-testing `OnPickup`). Tests:
    - `OnPickup_HealsByHealAmount`
    - `OnPickup_AtMax_NoChange`
    - `OnPickup_DoesNotExceedMaxHealth`
    - `OnPickup_DeadTarget_NoHeal` (stub sets `IsAlive=false`; `Health.Add` clamps to 0; expect 0 — note: this test asserts Health behavior; the GameLoop `IsAlive` check is separate)

3. **`PickupCollisionTests.cs`** (new) — `CollisionWorld2D` integration with `"actors"` + `"pickups"` layers:
    - `Overlap_AdjacentFrames_ProducesCollisionPair`
    - `Overlap_NonAdjacent_NoCollisionPair`
    - `OverlapPair_ResolvePickupOverlaps_CallsOnPickupAndQueuesDestroy` — wire `EntityManager`, register a `FoodPickupEntity` and a stub player `ICollisionActor`, run the overlap logic, assert `Heal` was applied and `Destroy` was queued.

4. **`LevelDirectorPickupSpawnTests.cs`** (new):
    - `SpawnPickups_Food_RegistersOnePickupInEntityManager`
    - `SpawnPickups_UnknownType_Throws`

5. **`AudioManagerTests.cs`** — add:
    - `PlaySfx_PickupHeal_DoesNotThrow_WhenAssetMissing` — confirms graceful no-op (asset may be missing if WAV file not yet committed).

---

## Files touched

**New (5):** `IPickup.cs`, `PickupBase.cs`, `FoodPickupEntity.cs`, `FoodPickupSprite.cs`, `PickupSpawnDef.cs`, `FoodPickupEntityTests.cs`, `PickupCollisionTests.cs`, `LevelDirectorPickupSpawnTests.cs`, `Content/audio/pickup_heal.wav`.

**Modified (10):** `IDamageable.cs`, `Health.cs`, `CombatActorBase.cs`, `PropBase.cs`, `EntityManager.cs`, `Level.cs`, `Level1.cs`, `LevelDirector.cs`, `GameLoop.cs`, `AudioManager.cs`, `Content.mgcb`, `HealthTests.cs`, `AudioManagerTests.cs`.

(Some files already exist in part — see Decision section. Implementer verifies presence before adding.)

---

## Validation

1. `dotnet build` — 0 warnings, 0 errors.
2. `dotnet test` — all existing + new tests pass.
3. Run game: walk right past X=1400 in Level1; food appears off-screen-left, scrolls into view (no pop-in), and disappears with heal + sound effect when player overlaps.
4. Regression: actor-vs-prop pushback unchanged.
5. Regression: `PickupHeal` with missing .wav asset is silent (no exception).
6. Level reset re-spawns the pickup (covered by `_entityManager.Clear()` + `ReinitLevel` flow).

---

## Risks

- **Y placement**: 556 may float/clip; verify visually.
- **Audio asset failure**: graceful no-op via existing `try/catch` in `AudioManager.LoadSfxGroup`.
- **Pickup inside a prop's collision box**: both actor→prop MTV and actor→pickup overlap fire the same frame. Allowed (recommended behavior).
- **Pickup not in `_hitboxService.ResolveHits` target filter**: handled by `is not IDamageable tgt continue` — free, already in code.

---

## Implementer decisions during execution

1. Y-offset (556 ±10px) — verify visually.
2. Dead-player pickup behavior: GameLoop checks `IsAlive` before calling `OnPickup`, but still destroys the pickup + plays the sound.
3. WAV asset: any short, free placeholder works (a 100–500ms blip is sufficient). If no audio asset can be committed, leave the .mgcb entry out and the code path is silent no-op via the existing try/catch.
