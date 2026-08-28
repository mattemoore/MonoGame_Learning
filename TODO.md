
1. Manually make a bat and animation and have it swing realistically to make sure the weapon overlay logic works
1. Replace all placholder sprites with double dragon sprites if that is legal
1. AudioService refers to content in Content
1. Food and Weapon entities in core?
1. Oildrum is in 3 files in game. Can we merge and also can we drop the effective damage thing by just giving it a health number larger like everything else?
1. _levelDirector creation in GameLoop.cs looks weird.  Passing in Weapon.Get and passing in parameterless method calls?
1. GameStateService hoisted to Core?
1. ActorStateMachine callbacks still needed after adding phases?  What about StateMachineController...should that be in Core?
1. MenuService hoisted to Core?

TODO

A living backlog of approved refactor TODOs, each with enough context to implement correctly in a future session. When a TODO is completed, delete its entry.

---

### 4. Add `SwingAnchors.Length` ↔ atlas frame-count wiring asserts

- **Category**: NEW deferred-from-discussion, not yet a TODO (candidate).
- **Context**: from the weapon-sync discussion — `BatWeapon.Bat.SwingAnchors` has 4 entries and the bat atlas has 4 frames; the *player's* `adventurer-attack1` has 5 frames. Frames are clamped (combatbase.cs:185) so bat ≤ player is safe, but `SetFrame` throws `ArgumentOutOfRangeException` if `SwingAnchors.Length` ever exceeds the atlas frame count.
- **Candidate action when the weapon system grows:** add `Debug.Assert(SwingAnchors.Length <= Sheet.FrameCount)` in `MeleeWeaponDef.CreateSprite` (inline TODO pointing here already added) so an oversized swing def exits loudly in Debug, not with an obscure draw-time throw.
- **Status:** candidate; do not implement unless a second weapon is added. Inline comment: `MonoGameLearning.Core/Combat/MeleeWeaponDef.cs` (top of `CreateSprite`).
