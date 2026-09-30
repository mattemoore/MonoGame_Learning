# Plan: Drop held weapon on knockdown/death + debug knockdown hotkey

## Goal

1. When an actor (player or enemy) holding a weapon enters **Knockdown** or **Dying**,
   spawn a real ground weapon pickup that can be picked back up. Today
   `CombatActorBase.KnockdownEntryImpl`/`DyingEntryImpl` only call `UnequipWeapon()` and the
   weapon vanishes.
2. Give the dropped weapon a short **drop arc** (toss to the ground) instead of popping into
   place.
3. Add a debug hotkey that forces the **player** into knockdown, mirroring the existing
   `K` (kill) and `C` (complete level) debug macros.

## Key decisions (confirmed)

- Applies to **both player and enemies**.
- Drop is a **lightweight code-driven arc tween** (position + spin), reusing the existing
  pickup texture; no new art and no physics engine.
- Reuses the existing pickup/factory path: `weapon.Name` is already the content key
  (`"Bat"`/`"Knife"` == `LevelContent.Bat`/`LevelContent.Knife`).
- Debug key: **`L`** (gameplay mode only, requires debug overlay on).
- Weapon is unequipped when knockdown/dying *starts*, so the drop is raised there, not when
  the actor later dies (enemy `Died` fires after `DyingEntryImpl` already unequipped).

## Critical constraint: the drop must not be instantly re-collected

A knocked-down player is still `IDamageable.IsAlive == true`, so today's `PickupService`
would collect any pickup overlapping their frame. A weapon dropped at their feet would be
re-grabbed the same frame while they lie on the ground, defeating the feature. Required:

1. **Collection gate** — the pickup cannot be collected while its arc is in flight
   (`CanBeCollected == false`), and `PickupService` must skip uncollectible pickups.
2. **Forward landing point** — the drop lands in the actor's facing direction, offset by
   `actorHalfWidth + clearance`, clear of the dropper's frame, so once it lands it is not
   overlapping the actor who dropped it.

This gate is needed regardless of the animation; the arc merely makes the flight visible.

## Implementation tasks

### 1. `CombatActorBase` — raise a weapon-drop event (Core)

File: `MonoGameLearning.Core/Entities/Actor/CombatActorBase.cs`

- Add event (payload: the weapon, the launch point, and the intended ground-landing point):
  `public event Action<WeaponDef, Vector2 origin, Vector2 landing>? WeaponDropped;`
- Add private helper:

  ```csharp
  private const float DropClearance = 32f;

  private void DropWeapon()
  {
      if (EquippedWeapon is not { } weapon) return;
      float forward = Direction == FacingDirection.Left ? -1f : 1f;
      float x = Position.X + forward * (Width * 0.5f + DropClearance);
      // Keep the landing reachable: never drop outside the actor's walkable bounds.
      if (MovementBounds.Width > 0f)
          x = MathHelper.Clamp(x, MovementBounds.X, MovementBounds.Right);
      var landing = new Vector2(x, Frame.Bottom);
      UnequipWeapon();
      WeaponDropped?.Invoke(weapon, Position, landing);
  }
  ```

- In `KnockdownEntryImpl()` and `DyingEntryImpl()`, replace the `UnequipWeapon()` call with
  `DropWeapon()`.
- Do **not** change `ResetActor()` — it must keep calling `UnequipWeapon()` directly so pool
  return / player reset never spawns a pickup.
- Throwing is unaffected: `PlayerEntity.OnFrameAdvanced` consumes the throwable via
  `UnequipWeapon()`, and the throw move does not enter knockdown/dying.

### 2. `PickupBase` + `IPickup` — opt-in drop arc and collectibility gate (Core)

File: `MonoGameLearning.Core/Entities/Pickup/IPickup.cs`

- Add a gate with a safe default so existing implementers (including the test double) are
  unchanged:
  `bool CanBeCollected => true;`

File: `MonoGameLearning.Core/Entities/Pickup/PickupBase.cs`

- Add `IUpdatable` to the implemented interfaces.
- Add:

  ```csharp
  private bool _isArcing;
  private float _arcT, _arcDuration, _arcPeak, _arcSpinTurns;
  private Vector2 _arcStart, _arcEnd;

  public virtual bool CanBeCollected => !_isArcing;
  public float Rotation { get; private set; }

  public void BeginDropArc(Vector2 start, Vector2 end, float duration, float peakHeight, float spinTurns)
  {
      _arcStart = start; _arcEnd = end;
      _arcDuration = duration; _arcPeak = peakHeight; _arcSpinTurns = spinTurns;
      _arcT = 0f; _isArcing = true; Rotation = 0f;
      Position = start;
  }

  public void Update(GameTime gameTime)
  {
      if (!_isArcing) return;
      _arcT += (float)gameTime.ElapsedGameTime.TotalSeconds;
      float t = MathHelper.Clamp(_arcT / _arcDuration, 0f, 1f);
      var linear = Vector2.Lerp(_arcStart, _arcEnd, t);
      float lift = _arcPeak * (1f - (2f * t - 1f) * (2f * t - 1f)); // parabolic hop
      Position = new Vector2(linear.X, linear.Y - lift);
      Rotation = MathHelper.TwoPi * _arcSpinTurns * t;
      if (t >= 1f) { _isArcing = false; Position = _arcEnd; Rotation = 0f; }
  }
  ```

- `Render`: pass `Rotation` to `SpriteBatch.Draw` (currently hard-coded `0f`). Default `0`
  keeps every existing pickup visually identical.

File: `MonoGameLearning.Core/Entities/Pickup/PickupService.cs`

- After `if (pickup is not IPickup pickupInterface) continue;` add:
  `if (!pickupInterface.CanBeCollected) continue;`

### 3. `LevelDirectorCore<TEnemy>` — spawn the arced pickup for enemies + expose for player

File: `MonoGameLearning.Core/Levels/LevelDirectorCore.cs`

- Add tuning constants and:

  ```csharp
  private const float DropArcDuration = 0.35f;
  private const float DropArcPeak = 40f;
  private const float DropSpinTurns = 1f;

  public void SpawnWeaponPickup(WeaponDef weapon, Vector2 origin, Vector2 landing)
  {
      var pickup = _createPickup(new PickupSpawnDef(weapon.Name, landing));
      var ground = new Vector2(landing.X, landing.Y - pickup.Height / 2f);
      if (pickup is PickupBase arcPickup)
          arcPickup.BeginDropArc(origin, ground, DropArcDuration, DropArcPeak, DropSpinTurns);
      else
          pickup.Position = ground;
      EntityManager.Register(pickup);
  }

  private void OnEnemyWeaponDropped(WeaponDef weapon, Vector2 origin, Vector2 landing) =>
      SpawnWeaponPickup(weapon, origin, landing);
  ```

- In `SpawnWave()`, next to `enemy.Died += OnDiedHandler;`, add
  `enemy.WeaponDropped += OnEnemyWeaponDropped;`.
- In `OnEnemyDied(TEnemy enemy)`, next to `enemy.Died -= OnDiedHandler;`, add
  `enemy.WeaponDropped -= OnEnemyWeaponDropped;`.
- Do **not** subscribe to the player here (would leak across `ReinitLevel`). GameLoop owns
  the player subscription (task 5).

### 4. `InputService` — bind the debug knockdown key (Core)

File: `MonoGameLearning.Core/Input/InputService.cs`

- Add `DebugKnockdown` to `InputAction`.
- Add binding in `_bindings`:
  `(new() { Keys.L }, InputAction.DebugKnockdown, InputMode.Gameplay),`

### 5. `GameLoop` — wire player drop + debug handler (Game)

File: `MonoGameLearning.Game/GameLoop/GameLoop.cs`

- In `LoadContent`, after `_player.Thrown += OnPlayerThrown;` add
  `_player.WeaponDropped += OnPlayerWeaponDropped;`.
- Add handler:

  ```csharp
  private void OnPlayerWeaponDropped(WeaponDef weapon, Vector2 origin, Vector2 landing) =>
      _levelDirector.SpawnWeaponPickup(weapon, origin, landing);
  ```

- In `_actionHandlers`, add:

  ```csharp
  [InputAction.DebugKnockdown] = () => { if (IsDebug && _gameState.State == GameState.Playing) _player?.TakeDamage(new DamageInfo { Amount = 0, Knockdown = true }); },
  ```

  (`Amount = 0` avoids damaging the player; routes through `CombatService` like real
  knockdowns so the guard/invincibility behavior matches gameplay.)

## Tests

Add `MonoGameLearning.Game.Tests/WeaponDropTests.cs` (reuse `TestEnemyEntity`, the existing
player test double, and `TestLevelDirector`; add a Knife mapping to
`TestLevelContent.CreatePickup` only if a knife case is added).

- **Actor-level** (enemy and player doubles):
  - Knockdown while holding `BatWeapon.Bat` raises `WeaponDropped` exactly once, clears
    `EquippedWeapon`, and the landing point is offset from the actor in its facing direction
    (clear of `Frame`); with the actor at a `MovementBounds` edge the landing `X` is clamped
    inside the bounds.
  - A second knockdown (no weapon) does not raise.
  - Death while holding a weapon raises `WeaponDropped` once (verifies `DyingEntryImpl`).
  - Unarmed knockdown/death raises nothing.
  - `Reset`/pool-return with a weapon equipped raises nothing (no drop on reset).
- **Arc / gate** (using `FoodPickupEntity("Food", pos, null)` as a concrete `PickupBase`):
  - During the arc `CanBeCollected` is false and `Position` moves along the arc; after the
    duration `CanBeCollected` is true, `Position == ground end`, `Rotation == 0`.
  - `PickupService.ResolveOverlaps` does **not** collect an overlapping pickup whose
    `CanBeCollected` is false (arc in flight), and does collect it once the arc completes at
    an overlapping position.
- **Director-level** (`LevelDirectorTests` style):
  - Spawn a bat-armed enemy, drive it to knockdown, assert a `WeaponPickupEntity` is
    registered and `ActiveEnemyCount` is unchanged (enemy got up, did not die).
  - Call `SpawnWeaponPickup` directly and assert the registered pickup is a
    `WeaponPickupEntity`; a non-`PickupBase` factory result still lands at
    `landing.Y - pickup.Height / 2`.
  - Assert the landed pickup is re-collectible (`CanBeCollected` true after its arc) and
    equips the weapon via `OnPickup`.
  - Assert `weapon.Name` resolves through the factory (`PickupSpawnDef(weapon.Name, ...)`),
    guarding the `WeaponDef.Name` ↔ `LevelContent` coupling.

## Docs / skill check

- `MANUAL_TESTING.md` (authoritative manual catalog):
  - Controls table: add `| L (debug) | Knock the player down |`.
  - Update row **7.19** — currently "Knife drops (not re-pickable)…". Change to: the held
    weapon (knife/bat) is tossed and lands in front of the actor as a re-pickable ground
    pickup on knockdown/death; Attack1 is a normal punch after getting up until re-picked.
  - Add a row covering the visual/feel check: dropped weapon arcs and spins to the ground,
    is not collectible mid-flight, lands clear in front of the dropper, can be re-picked,
    and persists when walking away and back.
- `LEARNING.md`: add a short entry for the pattern (opt-in `PickupBase` drop arc + an
  `IPickup.CanBeCollected` gate that defaults true) if it reads as a new reusable pattern.
- Skill check (mandatory): audit `.kilo/skills/create-weapon/SKILL.md` (and built-ins) for
  stale guidance. Its "drops as a pickup" step concerns level spawn defs and should remain
  accurate; update only if the new auto-drop behavior contradicts a documented step.

## Validation

1. `dotnet build --warnaserror` (zero warnings).
2. `dotnet test` (all green, including new tests).
3. Run `.markdownlint-cli2.jsonc` markdown lint on any `.md` created/modified
   (`MANUAL_TESTING.md`, `LEARNING.md`, this plan) and fix warnings.
4. Manual (human, live window): toggle `~`, press `L` — player is knocked down, the held
   weapon arcs/spins to the ground behind them, cannot be grabbed mid-flight, and can be
   re-collected; `K` and `C` still work.

## Risks / notes

- Coupling: `SpawnWeaponPickup` keys the pickup off `WeaponDef.Name`, which must equal a
  `LevelContent` key and be handled by `CreatePickup`. Already true for Bat/Knife; the
  factory throws loudly on an unknown name. Covered by a test.
- `PickupBase` gaining `IUpdatable` means every pickup is in `EntityService.Updatables`;
  `Update` is a cheap early-out when not arcing. Acceptable, and it keeps the arc in one
  place instead of a parallel dropping-pickup type.
- The landing point is behind the actor at `Width/2 + 32px`, level with the actor sprite's
  frame bottom; the mid-flight `CanBeCollected` gate keeps the weapon from being grabbed
  before it settles.
- Player invincibility (`_invincibilityTimer`, 1.5s) after a knockdown means repeated `L`
  presses within that window are ignored — expected and matches gameplay.
- Enemy `WeaponDropped` is unsubscribed on death; a knocked-down enemy that later dies has
  already dropped once (weapon is cleared). No pool leak because pooled enemies only return
  via death.

## Post-implementation revision (feel pass)

The original plan dropped the weapon forward of the actor. Revised after playtest:

- **Direction**: the weapon is blown out of the hands — launched from a point ahead of the
  actor (`Position ± Width/2` in facing) and landing **behind** the actor (`Position ∓
  (Width/2 + 32px)`), level with the actor sprite's frame bottom (its drawn feet, not the
  collision frame bottom). (Two intermediate passes tried landing it at the feet center and
  then at the collision-frame bottom; playtest settled on behind + sprite-frame bottom.)
- **Slower, more deliberate drop**: `DropArcDuration` went `0.35s` → `0.7s` (one full turn
  over the longer arc ≈ half the angular velocity). `DropArcPeak`/`DropSpinTurns` unchanged.

Tests updated: origin (ahead of the hands, both facings), landing (behind, both facings; the
sprite-frame-bottom Y falls back to the collision frame headlessly), and the arc-completion
elapsed time.
