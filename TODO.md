1. Fix bug where attack2 and attack3 swing the weapon when only attack1 should

---

### 4. Add `SwingHandleOffsets.Length` ↔ atlas frame-count wiring asserts

- **Category**: NEW deferred-from-discussion, not yet a TODO (candidate).
- **Context**: from the weapon-sync discussion — `BatWeapon.Bat.SwingHandleOffsets` has 4 entries and the bat atlas has 4 swing regions; the *player's* `adventurer-attack1` had 5 source frames before the placeholder sheet was rebuilt to 4. `MeleeWeaponDef.ResolveSwingFrame` clamps to `SwingHandleOffsets.Length - 1`, so bat ≤ actor animation is safe — but if `SwingHandleOffsets.Length` ever exceeds the atlas's swing-region count, the draw-time `TextureAtlas[regionName]` lookup in `CombatActorBase.RenderWeaponOverlay` throws `KeyNotFoundException`. The actor-side counterpart (a `HandAnchorTable` entry per animation frame) is already covered by `ActorHandAnchorTests`.
- **Candidate action when the weapon system grows:** add `Debug.Assert` wiring `SwingHandleOffsets.Length` to the sheet's swing-region count (e.g. in `RenderWeaponOverlay` when a weapon is equipped, or in a `MeleeWeaponDef` validation step at `Load`) so an oversized swing def exits loudly in Debug, not with an obscure draw-time throw.
- **Status:** candidate; do not implement unless a second weapon is added.
