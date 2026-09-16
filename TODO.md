1. Fix bug where attack2 and attack3 swing the weapon when only attack1 should

---

### 4. Add `SwingAnchors.Length` ↔ atlas frame-count wiring asserts

- **Category**: NEW deferred-from-discussion, not yet a TODO (candidate).
- **Context**: from the weapon-sync discussion — `BatWeapon.Bat.SwingAnchors` has 4 entries and the bat atlas has 4 swing regions; the *player's* `adventurer-attack1` has 5 frames. `MeleeWeaponDef.ResolveWeaponRegionName` clamps to `SwingAnchors.Length - 1`, so bat ≤ player is safe — but if `SwingAnchors.Length` ever exceeds the atlas's swing-region count, the draw-time `TextureAtlas[regionName]` lookup in `CombatActorBase.RenderWeaponOverlay` throws `KeyNotFoundException`.
- **Candidate action when the weapon system grows:** add `Debug.Assert` wiring `SwingAnchors.Length` to the sheet's swing-region count (e.g. in `RenderWeaponOverlay` when a weapon is equipped, or in a `MeleeWeaponDef` validation step at `Load`) so an oversized swing def exits loudly in Debug, not with an obscure draw-time throw.
- **Status:** candidate; do not implement unless a second weapon is added.
