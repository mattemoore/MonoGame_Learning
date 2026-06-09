# Beat 'Em Up Game Skeleton Roadmap

This roadmap outlines the milestones and individual TODO items required to build a fully functioning side-scrolling beat 'em up game skeleton. All items focus on logical, gameplay, and architectural implementation, using placeholder graphics for sprites, levels, and user interface elements.

---

## [x] Milestone 1: Game State Lifecycle Management
Establish the high-level states of the application to support game transitions.

- [x] **State Machine for Game State**: Create a main game flow state machine (e.g., in `GameLoop` or a new manager) with the following states:
  - `TitleScreen`
  - `Playing`
  - `Paused`
  - `GameOver`
  - `LevelComplete`
- [x] **Screens and Inputs**:
  - [x] Implement a basic Title Screen with "Start Game" and "Exit" actions.
  - [x] Implement a pause toggle (e.g., ESC key) that halts gameplay update logic.
  - [x] Create Game Over and Level Complete screens that display placeholder text/menus.

---

## [ ] Milestone 2: Combat Engine (Hitboxes, Hurtboxes, and Health)
Implement the core collision-based combat mechanics.

- [ ] **Hitbox/Hurtbox Component**:
  - [ ] Add support for defining attack `Hitboxes`
  - [ ] Add support for `Hurtboxes` (collision areas on actors that can receive damage).
  - [ ] Implement overlap check: when a `Hitbox` overlaps an active enemy's `Hurtbox`, trigger a hit.
- [ ] **Health System**:
  - [ ] Add `Health` (Current / Max) to `ActorEntity`.
  - [ ] Add basic damage processing and damage invincibility frames (i-frames) after being hit.
- [ ] **Knockback and Hit States**:
  - [ ] Implement a "HitStun" state (actor is temporarily unable to move/attack).
  - [ ] Implement a "Knockdown" state (actor is knocked onto the floor, becomes invulnerable, then stands back up).

---

## [ ] Milestone 3: Enemy AI & Spawning
Introduce automated opponents with basic tracking behavior.

- [ ] **Enemy Entity Class**:
  - [ ] Create a generic `EnemyEntity` inheriting from `ActorEntity`.
  - [ ] Equip it with a simplified state machine (Idle, Walk/Chase, Attack, Hit, KnockedDown, Dead).
- [ ] **Chase AI**:
  - [ ] Implement basic pathfinding/steering where the enemy moves toward the player's coordinates on the screen.
  - [ ] Constrain movement speed and distance to prevent overlapping exactly with the player.
- [ ] **Combat AI**:
  - [ ] Implement proximity detection: when the enemy is in range, trigger an attack state.
- [ ] **Enemy Wave/Spawner Trigger**:
  - [ ] Create a wave manager or level trigger that spawns a set number of enemies when the player reaches specific points in the level.

---

## [ ] Milestone 4: Scroll Locking & Level Progression
Control player movement and camera tracking during fights.

- [ ] **Fight Areas (Scroll Locks)**:
  - [ ] Add invisible boundaries that trigger when a wave spawns, preventing the player and camera from scrolling further right.
- [ ] **Wave Clearance & "GO" Prompt**:
  - [ ] Detect when all enemies in the current scroll-lock wave are defeated.
  - [ ] Lift the scroll lock.
  - [ ] Draw a flashing "GO ->" placeholder prompt on the HUD to signal the player to advance.
- [ ] **Level End Trigger**:
  - [ ] Add a final trigger volume at the end of the scrollable bounds that transitions the game to `LevelComplete` when reached.

---

## [ ] Milestone 5: HUD & UI Integration (Placeholder Layouts)
Provide visual feedback of game parameters using basic text/shapes.

- [ ] **Player HUD**:
  - [ ] Draw a health bar and remaining lives counter for the active player.
- [ ] **Enemy HUD**:
  - [ ] Display the active enemy's health bar (or a boss health bar at the bottom) when engaged in combat.
- [ ] **Score and Timer**:
  - [ ] Add a running level timer and score counter to the top-center HUD.
