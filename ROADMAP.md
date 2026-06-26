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

## [X] Milestone 2: Combat Engine (Hitboxes, Hurtboxes, and Health)

Implement the core collision-based combat mechanics.

- [X] **Hitbox/Hurtbox Component**:
  - [X] Add support for defining attack `Hitboxes`
  - [X] Add support for `Hurtboxes` (collision areas on actors that can receive damage).
  - [X] Implement overlap check: when a `Hitbox` overlaps an active enemy's `Hurtbox`, trigger a hit.
- [X] **Health System**:
  - [X] Add `Health` (Current / Max) to `ActorEntity`.
  - [X] Add basic damage processing and damage invincibility frames (i-frames) after being hit.
- [X] **Hit States**:
  - [X] Implement a "HitStun" state (actor is temporarily unable to move/attack).
  - [X] **Destroyable Prop Support**: Add `IDamageable` interface to allow non-combatant entities (garbage cans, barrels) to receive damage. See [plan](.kilo/plans/add-prop-support.md).
  - [X] Implement a "Knockdown" state (actor is knocked onto the floor, becomes invulnerable, then stands back up).

---

## [x] Milestone 3: Enemy AI & Spawning

Introduce automated opponents with basic tracking behavior.

- [x] **Enemy Entity Class**:
  - [x] Create an `EnemyEntity` inheriting from `CombatActorBase`.
  - [x] Equip it with a state machine (Idle, Chasing, Attacking, Hurt, KnockedDown, Dying, Dead).
- [x] **Chase AI**:
  - [x] Implement basic movement where the enemy moves toward the player's coordinates on the screen.
  - [x] Stop movement at a minimum chase distance to prevent overlapping with the player.
- [x] **Combat AI**:
  - [x] Implement proximity detection: when the enemy is in range, trigger an attack after a brief delay.
  - [x] Enforce an attack cooldown between attacks.
- [x] **Enemy Wave/Spawner Trigger**:
  - [x] Create a wave manager or level trigger that spawns a set number of enemies when the player reaches specific points in the level.

---

## [x] Milestone 4: Scroll Locking & Level Progression

Control player movement and camera tracking during fights.

- [x] **Fight Areas (Scroll Locks)**:
  - [x] Add invisible boundaries that trigger when a wave spawns, preventing the player and camera from scrolling further right.
- [x] **Wave Clearance & "GO" Prompt**:
  - [x] Detect when all enemies in the current scroll-lock wave are defeated.
  - [x] Lift the scroll lock.
  - [x] Draw a flashing "GO ->" placeholder prompt on the HUD to signal the player to advance.
- [x] **Level End Trigger**:
  - [x] Add a final trigger volume at the end of the scrollable bounds that transitions the game to `LevelComplete` when reached.

---

## [ ] Milestone 5: HUD & UI Integration (Placeholder Layouts)

Provide visual feedback of game parameters using basic text/shapes.

- [ ] **Player HUD**:
  - [ ] Draw a health bar and remaining lives counter for the active player.
- [ ] **Enemy HUD**:
  - [ ] Display the active enemy's health bar (or a boss health bar at the bottom) when engaged in combat.
- [ ] **Score and Timer**:
  - [ ] Add a running level timer and score counter to the top-center HUD.
