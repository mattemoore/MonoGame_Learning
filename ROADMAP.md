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

## [x] Milestone 5: HUD & Score

Provide visual feedback of game parameters using basic text/shapes.

- [x] **Player HUD**:
  - [x] Draw a health bar and remaining lives counter for the active player.
- [x] **Enemy HUD**:
  - [x] Display the active enemy's health bar (or a boss health bar at the bottom) when engaged in combat.
- [ ] **Score and Timer**:
  - [ ] Add a running level timer and score counter to the top-center HUD.

---

## [x] Milestone 6: Sound Engine

Implement a sound and music system for audio feedback.

- [x] **Audio Manager**: Create a central `AudioManager` for loading and playing sound effects and music tracks via MonoGame's `SoundEffect` API.
- [x] **SFX Cues**: Define sound effect triggers for core gameplay events (punch, hit, knock down, enemy death, player hurt, menu select, level complete, go prompt).
- [x] **Music Playback**: Add background music playback with transition support between gameplay and menu states.
- [x] **Volume Controls**: Expose master SFX and music volume settings, persisted via `SettingsService`.

---

## [ ] Milestone 7: Items, Pickups & Weapons

Add the iconic beat 'em up loot loop: defeated enemies and breakable props drop items, and the player can wield pickup weapons.

- [x] **Pickup Items**: Define a `PickupItem` entity type with subtypes (e.g., `HealthRestore`, `BonusPoints`, `ExtraLife`). *(Done: `PickupBase`/`IPickup`/`FoodPickupEntity` (heals 15 HP). `BonusPoints` and `ExtraLife` subtypes not yet implemented.)*
- [x] **Prop Drops**: When an `IDamageable` prop is destroyed, spawn any pickups declared on its `PropSpawnDef.Drops`. *(Implemented: `IPickupDropper` interface on `PropBase`, `PropSpawnDef.Drops` field, `LevelDirector.OnPropDestroyed` pipes drops through `SpawnPickups`. `Level1` declares one `OilDrumEntity` at x=1000 drops a `Food` pickup on destroy.)* **Enemy drops `[x]`**: `EnemySpawnDef.Drops` wired through `LevelDirector.SpawnWave` → `OnEnemyDied` (spawns before pool return) through the shared aligned-drop path; `EnemyEntity` implements `IPickupDropper`; `Reset` clears `Drops` per rental. One Level1 wave-2 Grunt drops Food.
- [x] **Pickup Collision**: Detect player overlap with `PickupItem` and apply its effect (heal, add score, grant life), then despawn. *(Implemented: `"pickups"` collision layer, `EntityManager.PickupCollidables`, overlap check in `GameLoop.ResolvePickupOverlaps`.)*
- [ ] **Pickup Animation**: Spawn with a brief idle/spin animation and fade out if untouched after a timeout. *(Static sprite rendering done via `PickupBase.Render` + `Texture2D`. Bob/spin animation and fade-out timer not yet implemented.)*
- [x] **Throwable Weapons**: Add `ProjectileWeapon` items (e.g., knife, bottle) that the player throws forward in the facing direction with its own hitbox/hurtbox.
- [x] **Melee Pickup Weapons**: Add `MeleeWeapon` items (e.g., bat, pipe, crate) the player can pick up to replace their standard attack for a limited time or until thrown/dropped. *(Done for the bat: `MeleeWeaponDef`/`BatWeapon` swap `Attack1Move`/`AttackMove` with a longer `SwingMove`; `WeaponPickupEntity` + armed-at-spawn enemies. Pipe/crate variants and re-drop deferred.)*
- [x] **Weapon Pickup/Drop Logic**: Detect overlap with dropped weapons, attach the weapon sprite to the player's attack animation, and animate attacks with the weapon's range and damage values. *(Done: pickup overlap → equip; weapon rendered as an overlay anchored to the holder, swing reuses `attack1` anim.)*
- [x] **Weapon Lifecycle**: Auto-drop the weapon on knockdown, on timer expiry, or on level transition. *(Drop on knockdown/death/reset implemented; timer-expiry and level-transition drops deferred.)*

---

## [ ] Milestone 8: Lives, Combos & Boss Encounters

Round out core progression and introduce the set-piece boss fights that bookend each level.

- [x] **Lives System**: Track `LivesRemaining` on the player (start at 3). Decrement on death and respawn at the current position. *(Already implemented: `PlayerEntity.Lives`, `GameLoop.OnPlayerDied` decrements and respawns, `ResetGame` restores lives, `PlayerBar` displays count.)* **Missing**: checkpoint-based respawn (currently respawns at camera position).
- [ ] **Extra Life Scoring**: Grant an extra life when the player's score crosses configurable thresholds. *(Blocked on Score system — Milestone 5 still has `[ ] Score and Timer`.)*
- [ ] **Continue Screen**: After `GameOver`, present a "Continue?" prompt with a countdown; on continue, restore health/lives and resume the current level. *(Currently `GameOver` only shows a static "Retry / Quit to Title" menu — no countdown or resume.)*
- [ ] **Combo Counter**: Track consecutive hits landed without a long gap and display a rising combo number on the HUD.
- [ ] **Score Multiplier**: Increase score-per-hit based on current combo tier, decaying back to baseline after the combo window expires. *(Blocked on Score system.)*
- [ ] **Boss Enemy Type**: Introduce a `BossEntity` with a large hurtbox, distinct multi-phase state machine, telegraphed attack patterns, and a dedicated boss health bar. *(Only `EnemyEntity` exists — single-phase, hardcoded 30 HP.)*
- [ ] **Boss Health Bar**: Reuse or extend `EnemyBar` to render a full-width bar at the bottom of the screen for boss encounters. *(Generic `EnemyBar` exists in top-left, but no dedicated boss bar layout.)*
- [ ] **Boss Wave Trigger**: Spawn the boss as the final wave of a level (e.g., inside the final scroll-lock area) and route `LevelComplete` to fire only on boss defeat.
- [ ] **Boss Defeat Reward**: On boss death, drop a high-value pickup (e.g., large health or bonus points) and trigger the `LevelComplete` state. *(Blocked on drop table from Milestone 7.)*

---

## [ ] Milestone 9: Gamepad Support & Input Remapping

Add game controller support and allow players to rebind keyboard and gamepad controls at runtime.

- [ ] **Gamepad Detection**: Integrate `GamePad` state reads into `InputManager` alongside keyboard, normalising both into `InputAction` events.
- [ ] **Gamepad Bindings**: Define sensible gamepad defaults (d-pad/stick for movement, face buttons for attacks, start/select for confirm/back).
- [ ] **Gamepad UI Hints**: Show gamepad-specific control text (e.g., button icons or names) on the title screen and settings menu when a controller is detected.
- [ ] **Rebinding Screen**: Add a "Controls" section to the settings menu where each action can be reassigned a keyboard key or gamepad button.
- [ ] **Persistence**: Save rebound controls to a config file and load them on startup, so rebinds survive restart.
- [ ] **Conflict Detection**: Prevent or flag duplicate bindings when remapping (e.g., warn if two actions map to the same key).

---

## [ ] Milestone 10: Local Co-op Multiplayer

Add drop-in couch co-op where a second player joins on a second input device and plays alongside the first on a shared screen. *(Builds on Milestone 9 gamepad support — default binding: P1 keyboard, P2 gamepad.)*

- [ ] **Second Player Spawn**: Spawn a second `PlayerEntity` at level start (or drop-in mid-level) from the title/menu or via a join button.
- [ ] **Input Device Assignment**: Map `InputManager` actions to per-player input sources (e.g., P1 keyboard, P2 gamepad) so both players can act simultaneously.
- [ ] **Player Differentiation**: Apply distinct visual indicators (tint, name plate, or character select) so each player is identifiable on the shared screen.
- [ ] **Shared Camera**: Adjust the camera to frame both players, clamping to level bounds and re-centering as they move apart.
- [ ] **Friendly Fire**: Define whether players can damage each other (default off) and route hit detection accordingly.
- [ ] **Shared Progression**: Decide how lives, score, and continues aggregate across players (shared pool vs per-player).
- [ ] **Revive Mechanic**: Allow a live player to revive a downed teammate within a short window instead of burning a life.
- [ ] **HUD Scaling**: Adapt the HUD to show health/lives for both players without overlap.

---

## [ ] Milestone 11: Online Multiplayer

Extend the skeleton to support networked play between two remote players. *(Largest scope milestone — design choices for transport, netcode, and matchmaking should be locked in a plan doc before implementation.)*

- [ ] **Netcode Model Selection**: Decide and document the netcode approach (lockstep vs rollback vs client-server) and its tradeoffs for a beat 'em up.
- [ ] **Transport Layer**: Integrate a networking library (e.g., Lidgren, ENet) for reliable/unreliable channels, connection lifecycle, and NAT traversal.
- [ ] **Lobby & Matchmaking**: Add a lobby flow (host/join by code or IP) and basic session discovery.
- [ ] **Input Synchronization**: Send each player's `InputAction` stream to the remote peer as the authoritative game state driver.
- [ ] **Entity Replication**: Replicate entity positions, states, and hitbox events across the wire; reconcile on receive.
- [ ] **Latency Compensation**: Hide lag via prediction (rollback) or interpolation, with configurable buffer to smooth jitter.
- [ ] **Desync Detection & Recovery**: Detect state divergence and resync from a checkpoint or full snapshot when divergence exceeds a threshold.
- [ ] **Online-Adapted UI**: Show connection state, ping, player ready indicators, and disconnect/reconnect prompts.
- [ ] **Security Considerations**: Validate inbound inputs/actions server-side or via deterministic lockstep to prevent cheating.

---

## [ ] Milestone 12: Double Dragon Graphics & Basic Combat Moves

Replace placeholder art with assets styled after *Double Dragon* and implement the core punch/kick move set.

- [ ] **Double Dragon Sprite Source**: Acquire and catalogue Double Dragon character sprites (player, enemy, boss) from a public-art/ripping source that is legally safe to use for this learning project, and document the source and any licensing notes.
- [ ] **Sprite Atlas Build**: Assemble the ripped frames into MonoGame texture atlases per character/state (idle, walk, punch, kick, hurt, knockdown, walk/death) with an `.atlas` definition matching `Content` pipeline conventions.
- [ ] **Character Re-skin**: Replace the current placeholder `PlayerSprite` and `EnemySprite` frames with the Double Dragon sprite set, preserving existing state machine timing and animation event wiring.
- [ ] **Environment/Backgrounds**: Replace placeholder background/level art with Double Dragon stage art (building facade, street, interior) cut to the game's scrollable bounds so seams and scroll-locks stay intact.
- [ ] **Prop & Pickup Art**: Swap placeholder prop (oil drum, crate, garbage can) and pickup (food, weapon) textures for Double Dragon equivalents where reasonable.
- [ ] **HUD & UI Pass**: Tone in the Double Dragon art style for health bars, lives counter, title screen, and "GO" prompt so the presentation is cohesive.
- [ ] **Basic Punch Move**: Implement a fast, short-range `Attack1Move` punch with a small hitbox, light damage, and brief hitstun, using the punch animation frames. *(Extends existing combat engine from Milestone 2.)*
- [ ] **Basic Kick Move**: Implement a second attack input mapped to a kick with longer reach and slightly longer recovery than the punch, using the kick frames; give the two moves distinct hitboxes, damage, and knockback values.
- [ ] **Basic Punch/Kick Combos**: Chain a light punch-punch-kick combo sequence; allow canceling into it and out of knockdown/vs it interrupts, keeping the existing animation-event cleanup patterns (`SubscribeToAnimationEvent`/`UnsubscribeFromAnimationEvent`).
- [ ] **Move Tuning Data**: Move per-move properties (recovery, damage, knockback, hitstun, hitbox offsets) into a tunable `MeleeWeaponDef`-style definition so punch/kick values can be balanced without code edits.

---

## Profile-First Investigation

- [ ] **`CollisionWorld2D.QueryCollisionPairs` per-frame allocation**: Called every frame in `GameLoop.ResolveCollisions`, this method is a compiler iterator — allocates a state-machine object + `HashSet<ActorPairKey>` per call. Profile to confirm it is a real GC pause contributor before vendoring/patching MonoGame.Extended.
