# TODO

- [ ] **Hit resolution: per-frame hitbox damage accumulates.** Attack1 does 5 damage per hitbox frame, but has hitboxes on frames 1 and 2. Both frames hit the target, so the enemy receives 10 damage per attack (5 × 2). Decide if this should be per-attack (5 total) or per-frame (10 total), and whether `_resolvedThisFrame` should persist across frames.

1. Move props and player1 loading into level1.cs.  Make reset() in GameLoop call Level.load() or something similar.  Also make sure on level unload all things are released like sprites etc.
1. Add in debug mode the health above entities
1. Per frame hurtboxes?
1. Update to monogame.extended 6.0
