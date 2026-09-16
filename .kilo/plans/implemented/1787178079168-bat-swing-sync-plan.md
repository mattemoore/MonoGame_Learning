# Bat Swing Sync — AnimatedSprite rotation + per-frame anchor

## Goal

Replace the static bat overlay (one `Texture2D` anchored carry/swing) with a synchronised bat sprite: rotation comes from bat animation **frames** on an `AnimatedSprite`, positioned on the holder via a **per-frame anchor** driven by the holder's attack progress (`AnimationFrameTracker.FrameIndex`). Hitbox fires only at swing apex (late frames), not mid-swing.

## Locked Decisions (from user)

| Decision | Answer |
| --- | --- |
| Rotation technique | Multi-frame `AnimatedSprite` (~4 frames) keyed to the holder's `attack1` frame index — no math rotations baked as more art; frames supply the visual arc |
| Anchor | `MeleeWeaponDef` exposes `CarryAnchor` (static) + `SwingAnchors[]` indexed per `attack1` frame (0–3); `CombatActorBase` picks the right vector |
| Facing-left handling | Negate anchor X **and** flip bat sprite with `SpriteEffects.FlipHorizontally` so the arc mirrors symmetric |
| Hitbox timing | Apex-only (late frames) → move `FrameHitboxes` from frames 1–2 to frames 2–3 in `BatWeapon.BatSwingMove` (avoids frame-0 pipeline pitfall) |
| Sprite instances per actor | Weapon defs hold the `SpriteSheet` (not a shared sprite); on `EquipWeapon`, actor creates its own `AnimatedSprite` from the def — required because player + an armed Enemy may both be armed concurrently |

## Key Codebase Facts (verified in v1)

- `CombatActorBase.RenderWeaponOverlay` draws `EquippedWeapon.Texture` with `Carry`/`Swing` `Vector2` offsets; replaced by sprite drawing.
- `AnimationFrameTracker.FrameIndex` already exposes the holder's attack-frame index — use it directly, no new tracking channel needed.
- New animation definitions belong at def load time: `BatWeapon.Load` must build a `SpriteSheet` for the bat ATLAS (same approach as `PlayerSprite.Load`/`EnemySprite.Load`), define "swing" with 4 frames non-looping, then `CreateSprite()` on equip.
- Per-attack completion already triggers `OnAttackingExit` cleanup paths in both classes; weapon sprite lifecycle hooks attach the same seams Equip/Unequip already have.
- `MeleeWeaponDef.Texture` becomes obsolete → removed; `WeaponPickupEntity` previously constructed its frame from `weapon.Texture` — replace with the weapon sprite's first frame `Frame` (fallback to default size when sprite unavailable in headless tests, same convention as `PickupBase`).

## Architecture

```text
Core/Combat/MeleeWeaponDef.cs        Name, SwingMove, CarryAnchor, SwingAnchors[], SpriteSheet Sheet, CreateSprite() factory
Core/Entities/Actor/CombatActorBase  adds WeaponSprite (own per-actor), anchor resolve per FrameIndex, flip + negate on facing left; WeaponSprite lifecycle on Equip/Unequip/Reset
Game/Weapons/BatWeapon.cs            loads atlas & defines "swing" (4 frames, non-looping), refreshes def anchors
Game/AnimatedSprites?                none needed — weapon anim defs live in BatWeapon.Load like (but self-contained)
Content/images/bat-swing.png         4-frame bat arc strip (agent placeholder: 12×40 frames side-by-side or 48×40 sheet)
Content.mgcb                         add bat-swing.png (bat.png removed)
```

**Equip flow**: `EquipWeapon(def)` stores `EquippedWeapon = def; WeaponSprite = def.CreateSprite(); WeaponSprite.SetAnimation(BatWeapon.SwingAnimation ... migration needed?)`. For render: anchor = `IsInAttackingState ? SwingAnchors[min(FrameIndex, len-1)] : CarryAnchor`.

**Anchor + flip on render**: compute base anchor = carry/swing[frame]; if facing left anchor.X *= -1 and sprite `Effect = FlipHorizontally`. Set `WeaponSprite.Origin = center(frame)`. Draw `WeaponSprite` at `Position + anchor`.

**Frame sync**: in render while armed choose frame? Alternatives: driver `WeaponSprite.Controller` safely stepping via `SetAnimation` re-entry on attack edge, or planned index mapping constant so index locked to holder. Decision implementation detail; plan uses direct manual frame step on `Update` if `SetFrame` exists, else per-render `WeaponSprite.SetAnimation("swing")` on attack-started edge and `WeaponSprite.Update(gameTime)` in `Update` as fallback. (Marked as an open implementation option — chose whichever compiles against the installed MonoGame.Extended 6.0 API.)

**Hitbox pivot**: `BatWeapon` `FrameHitboxes` keys move 1–2 → 2–3 (size/offset unchanged).

## Ordered Task List

1. **Content**: replace `images/bat.png` with a 4-frame bat arc strip `images/bat-swing.png` (placeholder; e.g., 48×40 made of four 12×40 frames: 90°, 45°, 45°-low, 0°); add/remove mgcb entry.
2. **Core**: `MeleeWeaponDef` — remove `Texture`; add `SpriteSheet? Sheet`, `Vector2 CarryAnchor`, `Vector2[] SwingAnchors`, and `AnimatedSprite CreateSprite()`. Guard `Sheet != null` at factory call (headless tests → sprite null).
3. **Core**: `CombatActorBase` — `protected AnimatedSprite? WeaponSprite` field; `EquipWeapon` creates sprite (guard def.Sheet), `UnequipWeapon`/`ResetActor` null it. Render path uses sprite sprite anchored per frame; facing-left → flip + negate anchor. Debug draw shows current anchor + frame.
4. **Game**: `BatWeapon` — in `Load`, build atlas from `images/bat-swing` asset (e.g., `BatAtlas`), define "swing" 4-frame non-looping, expose animation key; populate `CarryAnchor` (use same carry) + `SwingAnchors` (e.g., `(12,-15)`, `(25,-4)`, `(34,0)`, `(30,-2)` starting values that lock the barrel tip sweeping into the arc).
5. **Game**: `WeaponPickupEntity` — take `MeleeWeaponDef` without Texture; pick frame size from the sprite-sheet first region when available (else default `PickupBase` default size).
6. **Game**: `GameLoop.LoadContent` — `BatWeapon.Load(Content)` stays; switch asset to `images/bat-swing`.
7. **Core/Game**: hitbox keys shift to 2–3 in `BatWeapon.BatSwingMove`.
8. **Docs**: update AGENTS.md Weapons bullet (`bat-swing.png` asset + per-frame anchor + apex-only hitbox) if structure changed.

## Tests (mandatory per AGENTS.md)

1. `EquipWeapon_CreatesWeaponSpriteFromDef` + `UnequipWeapon/Reset_ClearsWeaponSprite`
2. `ArmedActorRender_AwaitsDrawing` — render path doesn't throw when weapon sprite is null (headless)
3. `AnchorSelection_AttackFrame` — expose anchor resolver; assert carry when not attacking, per-frame entry when attacking
4. `FacingLeft_AxisMirror` — anchor X negated and sprite flipped on left-facing render (assert via a code path split around `Direction`)
5. `Hitbox_ApexOnly` — register `BatWeapon.SwingMove` frames 2–3 via `HitboxService` → hits; frames 0–1 → no hit for the same target placement
6. existing v1 melee weapon tests stay green (update only expectations that hard-code frames 1–2 or assumptions about `Texture`)

## Risks / Pitfalls

- **AnimatedSprite frame setter availability**: if `Controller.CurrentFrame` is not settable / no `SetFrame` exists, fall back to edge-triggered `SetAnimation("swing")` on attack start + `Update(gameTime)` in `CombatActorBase.Update` while armed. Sync drift risk is bounded to one 0.1s frame.
- **Per-actor sprite ownership**: single shared sprite carried along def would corrupt concurrent player + enemy armings; per-equip `CreateSprite()` is mandatory and GC-paid only at equip time.
- **Reset pooling of the sprite**: `ResetActor` must null `WeaponSprite` or pooled enemies resurrect visuals from prior rental.
- **Upward tapering anchors can push the sprite off-level walkable bounds while armed** — fine visually, but watch for debug-mode draws outside the level edge in first playtest.
- **Null sheet at test time**: guarded factory vs Debug.WriteLine guards follow the same v1 pattern.

## GC / Performance

One extra `AnimatedSprite` allocation per equip (bounded, once at equip) + one draw call per armed actor per frame (unchanged). Anchor vectors are structs on stack. Frame-hitboxes remain a zero-alloc pipeline.

## Validation

1. `dotnet build --warnaserror` — zero warnings.
2. `dotnet test` — full suite passes.
3. Manual playtest: pick up bat → while swinging, bat visibly rotates 90°→45°→horizontal with a matching hit that only connects at the apex; facing left mirrors; pooled armed Grunt rentals don't revive stale bat sprites.

## Out of Scope (deferred)

Attack2/Attack3 weapon variants, bat articulation (grip anchor shifts along barrel), continuation of the swing through idle decay, weapon throw arcs, and v2 dps tuning.
