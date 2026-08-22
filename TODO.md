**TODO**

A living backlog of approved refactor TODOs, each with enough context to implement correctly in a future session. When a TODO is completed, delete its entry.

---

### 1. Consolidate null-guard idioms in `CombatActorBase`

- **Location (inline):** `MonoGameLearning.Core/Entities/Actor/CombatActorBase.cs:56` (above the `Sprite` property).
- **Problem:** Multiple idioms for the *same* nullable state are in use throughout the class:
  - `if (Sprite is null) return;` (e.g. `AdvanceFrameAndRegisterHitboxes`)
  - `Sprite?.Update(...)` (e.g. `TryHandleIncapacitatedUpdate`)
  - `Debug.Assert(Sprite is not null, ...) + Sprite!` (e.g. `OnAnimationCompleted`)
  - `if (Sprite is not null) { ... }` (e.g. `ResetActor`)
  - A duplicated `EnsureSpriteAttached()` guard at the top of each `Update` (PlayerEntity.cs:150 / EnemyEntity.cs:151).
  - This dispersion means new logic will pick a 5th idiom; the class should admit exactly one pattern.
- **Why null at all:** `Sprite` is genuinely nullable because of the documented *headless-testing* contract — test doubles built via `FormatterServices`/overrides (see the `TestPlayerEntity` override of `EnsureSpriteAttached` returning `true`) have no `GraphicsDevice` and thus no sprite. Production construction always assigns a sprite, so the null is a *boundary* concern, not an interior one.
- **Preferred direction (choose one when implementing):**
  1. **Centralize at the boundary:** an internal `Sprite` accessor that `Debug.Assert`s non-null and returns it; interior `Update`/`Render` paths then never see `null`. Keep the null-when-headless only at the outer `Update` entry (`EnsureSpriteAttached()` or a single guard), collapsing the ~dozen scattered checks to ~2.
  2. **Alternative:** keep the nullable but standardize on exactly one idiom *everywhere* (inline `if (Sprite is { } sprite)` pattern or always `Sprite is null` early-return), delete `EnsureSpriteAttached` if it ends up redundant.
- **Not in scope:** removing the nullability itself (would require rework of the headless test contract) or changing `HitboxService`/`CurrentMove` nullability independently.
- **Checklist:** `dotnet build --warnaserror`; `dotnet test`; no behavior change outside headless tests (any test asserting current NPE-free headless paths must still pass).

---

### 2. Remove the type check in `WeaponPickupEntity.OnPickup`

- **Location:** `**GameLearning.Game/Entities/Pickups/WeaponPickupEntity.cs:16**`
- **Current code**:
  ```csharp
  public override void OnPickup(IDamageable target)
  {
      if (target is CombatActorBase actor)
          actor.EquipWeapon(_weapon);
  }
  ```
- **Goal**: eliminate the `target is CombatActorBase actor` guard, mirroring `FoodPickupEntity.OnPickup` which calls `target.Heal(...)` directly.
- **Blocker to resolve first**: `EquipWeapon` lives on `CombatActorBase` (a concrete class), not on any interface, while `OnPickup(IDamageable target)` receives an `IDamageable`. FoodPickup works because `Heal` is an `IDamageable` member. `EquipWeapon` is not.
- **Options:**
  1. Add a capability interface to Core (e.g. `IWeaponWielder` with `EquipWeapon(MeleeWeaponDef)`) implemented by `CombatActorBase`; then `OnPickup(IWeaponWielder)/target is IWeaponWielder` becomes `interface`-flagged with no `type`. Prefer this if `EquipWeapon`+`UnequipWeapon` are intended to outlive the bat weapon.
  2. Keep the type check but note it's deliberate. If **1** isn't desired, delete the TODO and the rationale in the file.
- **Checklist**: `dotnet build --warnaserror`; `dotnet test` (MeleeWeaponTests.cs pickups tests cover equip-on-pickup and equip-without-actor cases).

---

### 3. Extract repeated `DefineAnimation` logic in animated sprites

- **Location (Game):** `MonoGameLearning.Game/AnimatedSprites/EnemySprite.cs:46`
- **Context**: every static animated-sprite class (`PlayerSprite`, `EnemySprite`, `BatSprite`, `OilDrumSprite`) repeats this pattern:
  ```csharp
  Texture2DAtlas atlas = content.Load<Texture2DAtlas>(...);
  _spriteSheet = new SpriteSheet(name, atlas);
  _spriteSheet.DefineAnimation(key, builder => { builder.IsLooping(...); builder.AddFrame(...); });
  ```
  and a `Create()` that news up `AnimatedSprite`, sets `Origin = Center`, returns it.
- **Candidate simplification:** a small Core helper (static or generic) that takes a `ContentManager` + atlas asset path + a sequence of `(name, isLooping, frames)` and returns a `SpriteSheet`; the per-sprite classes then just do `define animations + Create`, or even collapse to a data-driven config.
- **Trade-offs / simplify first (per AGENTS.md):**
  - A helper only earns its keep if it meaningfully shrinks the spread; the current per-class spec at least reads well. Consider whether the *actual* duplicate surface is small enough that a helper adds indirection for no net reduction.
  - If implementing, keep `AnimationIdle`/`AnimationRun`-style constants on each sprite (they are used by entity state machines), do NOT introduce a runtime config format (JSON/etc.) — this is a code-level concern.
  - Validate with `dotnet build --warnaserror` and `dotnet test` (OilDrumSprite/PlayerSprite/EnemySprite consumers exercise this).

---

### 4. Add `SwingAnchors.Length` ↔ atlas frame-count wiring asserts

- **Category**: NEW deferred-from-discussion, not yet a TODO (candidate).
- **Context**: from the weapon-sync discussion — `BatWeapon.Bat.SwingAnchors` has 4 entries and the bat atlas has 4 frames; the *player's* `adventurer-attack1` has 5 frames. Frames are clamped (combatbase.cs:185) so bat ≤ player is safe, but `SetFrame` throws `ArgumentOutOfRangeException` if `SwingAnchors.Length` ever exceeds the atlas frame count.
- **Candidate action when the weapon system grows:** add `Debug.Assert(SwingAnchors.Length <= Sheet.FrameCount)` in `MeleeWeaponDef.CreateSprite` (inline TODO pointing here already added) so an oversized swing def exits loudly in Debug, not with an obscure draw-time throw.
- **Status:** candidate; do not implement unless a second weapon is added. Inline comment: `MonoGameLearning.Core/Combat/MeleeWeaponDef.cs` (top of `CreateSprite`).