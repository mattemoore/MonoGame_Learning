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
  - [X] **Destroyable Prop Support**: Add `IDamageable` interface to allow non-combatant entities (garbage cans, barrels) to receive damage.
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

- [x] **Pickup Items**: Define a `PickupItem` entity type with subtypes (e.g., `HealthRestore`, `BonusPoints`, `ExtraLife`). *(Done: `PickupBase`/`IPickup`/`FoodPickupEntity` (heals 15 HP). `BonusPoints` and `ExtraLife` subtypes are **not planned** — score-based and extra-life pickups are out of scope for now.)*
- [x] **Prop Drops**: When an `IDamageable` prop is destroyed, spawn any pickups declared on its `PropSpawnDef.Drops`. *(Implemented: `IPickupDropper` interface on `PropBase`, `PropSpawnDef.Drops` field, `LevelDirector.OnPropDestroyed` pipes drops through `SpawnPickups`. `Level1` declares one `OilDrumEntity` at x=1000 drops a `Food` pickup on destroy.)* **Enemy drops `[x]`**: `EnemySpawnDef.Drops` wired through `LevelDirector.SpawnWave` → `OnEnemyDied` (spawns before pool return) through the shared aligned-drop path; `EnemyEntity` implements `IPickupDropper`; `Reset` clears `Drops` per rental. One Level1 wave-2 Grunt drops Food.
- [x] **Pickup Collision**: Detect player overlap with `PickupItem` and apply its effect (heal, add score, grant life), then despawn. *(Implemented: `"pickups"` collision layer, `EntityManager.PickupCollidables`, overlap check in `GameLoop.ResolvePickupOverlaps`.)*
- [x] **Throwable Weapons**: Add `ProjectileWeapon` items (e.g., knife, bottle) that the player throws forward in the facing direction with its own hitbox/hurtbox. *(Done for the knife: `WeaponDef` base + `ThrowableWeaponDef`, `ProjectileEntity`/`ProjectileService` pooled single-hit projectiles, Attack1 throws. [plan](<.kilo/plans/implemented/1789329918665-throwable-weapons.md>). Enemy throwing is tracked in Milestone 11; thrown-weapon flight/re-pickup and breakables are tracked as the items below.)* The knife's placeholder art was replaced with Aseprite-authored art (Hold pose + 7-frame Fly run) — [art plan](<.kilo/plans/implemented/1789600505620-replace-knife-placeholder-art.md>).
- [ ] **Thrown Weapon Flight & Re-Pickup**: Thrown weapons travel in a straight line in the facing direction until they hit a target or reach max range — **no gravity or arc**; a projectile never falls or bounces (author the illusion of a spin purely in its flight frames). A thrown weapon that reaches max range without hitting drops to the ground as a re-collectable pickup instead of vanishing, and a breakable throwable (e.g. a bottle) shatters on impact instead of being re-pickable. *(Today the knife flies straight and despawns on first hit/past `MaxRange`; ground re-pickup and shatter are not implemented.)*
- [x] **Melee Pickup Weapons**: Add `MeleeWeapon` items (e.g., bat, pipe, crate) the player can pick up to replace their standard attack for a limited time or until thrown/dropped. *(Done for the bat: `MeleeWeaponDef`/`BatWeapon` swap `Attack1Move`/`AttackMove` with a longer `SwingMove`; `WeaponPickupEntity` + armed-at-spawn enemies. Pipe/crate variants and re-drop deferred.)*
- [x] **Weapon Pickup/Drop Logic**: Detect overlap with dropped weapons, attach the weapon sprite to the player's attack animation, and animate attacks with the weapon's range and damage values. *(Done: pickup overlap → equip; weapon rendered as an overlay anchored to the holder, swing reuses `attack1` anim.)*
- [x] **Weapon Lifecycle**: Auto-drop the weapon on knockdown, on timer expiry, or on level transition. *(Drop on knockdown/death/reset implemented; timer-expiry and level-transition drops tracked as the items below.)*
- [ ] **Melee Weapon Lifetime**: A held melee weapon is used for a limited time (or a hit/durability count) and is dropped or despawned when that expires, so a powerful pickup does not last the whole level.
- [ ] **Level-Transition Weapon Drop**: Drop or despawn a held weapon when advancing between stages, so weapons are not carried across a level/stage transition.
- [ ] **Per-Frame Weapon Overlay Pose (angle + frame)**: Let a held weapon vary its **rotation** and/or **displayed frame** per actor animation frame, on top of the existing anchor position — e.g. tilt the bat while running, or use a distinct carry frame for each run frame. Today `CombatActorBase.RenderWeaponOverlay` always draws at a fixed rotation (`0f`) and selects the region only from swing state (`<SwingPrefix>-<NN>` while swinging, else the single `CarryRegion`), so a carried weapon is frozen to one pose across every animation. Candidate design: extend `WeaponDef` with pose-indexed carry data — a rotation-per-frame table (`CarryRotationOffsets`) and/or a carry-region run (`CarryRegionPrefix`/`CarryFrameCount`) indexed by `SpriteRenderer.AnimationFrame` (the animation-relative clock, never the atlas-index `AnimationController.CurrentFrame`), authored the same way as `SwingHandleOffsets`. Keep the split-ownership rule: the actor owns the hand point, the weapon owns its grip **and** pose. Open questions: whether per-frame carry regions alone make a rotation table unnecessary, and whether rotation should be additive to a base carry angle (mirroring `ApplyWeaponFacing`) so facing flips stay correct.
- [ ] **Deflect Throwables**: Let an actor *hit an incoming throwable* with its own attack and send it back the way it came. When the target's active hitbox overlaps a hostile `ProjectileEntity`, the projectile reverses direction, flips its `Faction` to the deflector's, re-arms (clears `MarkHit`/hit dedup) so it can hit its original owner, and continues straight. Directional melee/attack timing is what earns the deflect — no new projectile art needed if the flight run just faces the other way. Candidate design: give `ProjectileEntity` a hurtbox or a hitbox-vs-projectile check in `HitboxService.ResolveHits` (it currently resolves vs `IDamageable` only), then have `ProjectileService` own the reversal (flip `FacingDirection`/velocity, swap `Faction`, reset the hit flag). Open questions: whether deflection is melee-only or also projectile-vs-projectile, whether it costs the deflector their swing, and whether the reversed shot should damage the thrower or anyone in the return path.

---

## [ ] Milestone 8: Player Jumping & Aerial Combat

Add vertical movement to the player — jump arcs, aerial attacks, and landing recovery — expanding the combat and evasion toolkit.

- [ ] **Jump Input & Binding**: Add a `Jump` `InputAction` with a keyboard default (e.g., Space). Distinguish tap (short hop) vs hold (full jump) if height-modulated jumping is desired.
- [ ] **Ground Detection & Gravity**: Add `IsGrounded` flag, `VerticalVelocity`, and `Gravity` to the player. Ground check vs `Frame.Bottom` / level floor Y. No free-form platforming — fixed arc jumps typical of the genre.
- [ ] **Jump State**: Add `Jumping` / `Airborne` to the player's state machine — blocking normal ground movement and attack-move patterns while airborne. The state ends on landing with brief recovery frames.
- [ ] **Jump Animation**: Add jump-start (launch), airborne (loop), and landing (recovery) animation frames/tags to `PlayerSprite`.
- [ ] **Aerial Attack**: Allow `Attack1` / `Attack2` input during a jump. Plays a distinct aerial attack animation (different from ground attacks) with its own hitbox, damage, and knockback/knockdown values. The player continues the jump arc during the attack.
- [ ] **Air Hit Reactions**: Enemies hit by an aerial attack use the existing Knockdown state (standard for the genre — jumping attacks knock enemies down).
- [ ] **Landing Recovery**: On landing, play a brief recovery animation during which the player cannot move or attack (prevents landing-cancel exploits). Transitions back to Idle when recovery completes.
- [ ] **Jump-over Enemies (optional)**: Allow the jump arc to clear enemy hurtboxes so the player can evade by jumping over an approaching enemy. Requires Y-axis collision between player hurtbox and enemy hurtbox.

---

## [ ] Milestone 9: Lives, Combos & Boss Encounters

Round out core progression and introduce the set-piece boss fights that bookend each level.

- [x] **Lives System**: Track `LivesRemaining` on the player (start at 3). Decrement on death and respawn at the current position. *(Already implemented: `PlayerEntity.Lives`, `GameLoop.OnPlayerDied` decrements and respawns, `ResetGame` restores lives, `PlayerBar` displays count.)* **Missing**: safe respawn (see below) — checkpoint-based respawn is **not planned**.
- [ ] **Safe Respawn**: On respawn, place the player somewhere clear of overlapping enemies, hazards, and solid props (or displace them out of overlap) and grant a short invulnerability window so they cannot be instantly re-hit. Checkpoint-based respawn is intentionally out of scope. *(An invuln window already exists via `PlayerEntity.Respawn()`; safe-position selection is not implemented.)*
- [ ] **Extra Life Scoring**: Grant an extra life when the player's score crosses configurable thresholds. **Not planned** — extra lives from score are out of scope. *(Would have been blocked on the Score system regardless.)*
- [ ] **Continue Screen**: After `GameOver`, present a "Continue?" prompt with a countdown; on continue, restore health/lives and resume the current level. *(Currently `GameOver` only shows a static "Retry / Quit to Title" menu — no countdown or resume.)*
- [ ] **Combo Counter**: Track consecutive hits landed without a long gap and display a rising combo number on the HUD.
- [ ] **Chained / Multi-Hit Attack Moves**: Turn repeated attack input into a move chain so a sequence ends in a distinct finisher instead of repeating the same strike — e.g. two straight punches then an uppercut, or a kick leading into a roundhouse. Each link is a real `MoveData` with its own animation, hitbox frames, damage, and finisher/knockdown values (uppercut and roundhouse knock down). Candidate design: an active chain index owned by the actor (or a small `MoveChainDef`) advanced when a hit lands or a follow-up input arrives inside a combo window, and reset on whiff, window expiry, hurt, or knockdown; `Attack1Move` (which already swaps to the weapon's swing) would resolve to the current chain link. Open questions: advance on hit vs on next input, how weapon chains differ from bare-fist chains, whether `attack2`/`attack3` stay discrete branches or feed the same chain, and how the attack window relates to the HUD `Combo Counter`.
- [ ] **Score Multiplier**: Increase score-per-hit based on current combo tier, decaying back to baseline after the combo window expires. *(Blocked on Score system.)*
- [ ] **Boss Enemy Type**: Introduce a `BossEntity` with a large hurtbox, distinct multi-phase state machine, telegraphed attack patterns, and a dedicated boss health bar. *(Only `EnemyEntity` exists — single-phase, hardcoded 30 HP.)*
- [ ] **Boss Health Bar**: Reuse or extend `EnemyBar` to render a full-width bar at the bottom of the screen for boss encounters. *(Generic `EnemyBar` exists in top-left, but no dedicated boss bar layout.)*
- [ ] **Boss Wave Trigger**: Spawn the boss as the final wave of a level (e.g., inside the final scroll-lock area) and route `LevelComplete` to fire only on boss defeat.
- [ ] **Boss Defeat Reward**: On boss death, drop a high-value pickup (e.g. large health) and trigger the `LevelComplete` state. *(Blocked on drop table from Milestone 7.)*

---

## [ ] Milestone 10: Grabs, Throws & Advanced Combat

Round out the melee toolkit with the signature beat 'em up grab-and-throw loop and responsive input handling.

- [ ] **Player Grab**: Add a grab state — when the player initiates a grab on an adjacent enemy (in range and roughly facing them), lock both actors into a hold where the enemy cannot act and the player can knee/punch repeatedly before throwing or releasing.
- [ ] **Throw**: From a hold, throw the grabbed enemy forward or back — the thrown body arcs and knocks down any enemy it strikes on the way, then lands knocked down. The `PickupBase.BeginDropArc` code tween is a reusable starting point for the body arc.
- [ ] **Enemy Grab**: Enemies can grab and hold the player, dealing periodic damage until the player mashes free or a timer expires.
- [ ] **Break-Free / Escape**: Mash-to-escape window shared by player and enemies; escaping returns both actors to their neutral states.
- [ ] **Throw Damage & States**: Define damage/knockdown for the throw impact and for enemies hit by a thrown body, and integrate the grab/throw states with the existing Hurt/Knockdown/Dying transitions.
- [ ] **Input Buffering & Leniency**: Buffer attack/movement input during recovery frames so chained attacks and follow-ups land reliably, and define cancel windows between moves.
- [ ] **Special Moves (optional)**: Directional or meter-gated specials (e.g. spinning attack, aerial kick) with their own cooldown or health cost.

---

## [ ] Milestone 11: Enemy Variety & Archetypes

Replace the single grunt with a roster of distinct enemies, each with its own stats, reach, and AI behavior.

- [ ] **Enemy Archetype Data**: Parameterize `EnemyEntity` (health, speed, attack range/cooldown, damage, knockdown resistance, drop table) so a spawn def selects a type instead of one hardcoded profile.
- [ ] **Archetype Roster**: Add at least a fast lightweight brawler, a heavy bruiser (high HP, resists knockdown), a weapon-wielder (bat/pipe), and a ranged thrower (knife/bottle).
- [ ] **Per-Archetype Art**: Each archetype needs its own `EnemySprite` atlas + hand anchors, or a size/palette variant of the shared atlas.
- [ ] **Behavior Profiles**: Vary AI per archetype — chase distance, attack selection, back-off/guard behavior, and aggression timing.
- [ ] **Thrown Weapons (enemy)**: Let weapon-thrower archetypes throw projectiles at the player (the deferred enemy-throwing half of Milestone 7).

---

## [ ] Milestone 12: Environmental Hazards & Scene Transitions

Add interactive level geometry beyond the flat walkable band.

- [ ] **Death Pits / Hazards**: Hazard volumes (pits, spikes, water) that damage or instantly kill an actor that enters them; knocked-back or thrown enemies can be forced in.
- [ ] **Conveyor Belts / Moving Platforms**: Surfaces that add drift or carry actors, affecting movement and combat timing.
- [ ] **Elevators / Vertical Transitions**: Moving platform segments that change the active area.
- [ ] **Doors & Thresholds**: Enter/exit triggers between rooms/arenas within a stage, with scroll or fade transitions.
- [ ] **Hazard/Combat Interaction**: Route knockback and throw launches into hazard detection so bodies can be knocked into them.

---

## [ ] Milestone 13: Stage Progression & Difficulty

Turn the single level into a multi-stage campaign with selectable difficulty.

- [ ] **Stage Sequence**: A campaign flow that advances through multiple stages (data-driven `LevelData` per stage) with per-stage backgrounds, waves, drops, and bosses.
- [ ] **Stage Transitions**: Level-complete handling that advances to the next stage instead of ending the game, replacing the current single-level termination.
- [ ] **Stage Content**: Author at least 2-3 distinct stages reusing the existing prop/enemy/pickup factory pipeline.
- [ ] **Difficulty Scaling**: Difficulty options (e.g. Easy/Normal/Hard) that scale enemy count, health, aggression, and possibly continue count.
- [ ] **Difficulty Persistence**: Save the chosen difficulty in settings alongside audio and resolution.

---

## [ ] Milestone 14: Gamepad Support & Input Remapping

Add game controller support and allow players to rebind keyboard and gamepad controls at runtime.

- [ ] **Gamepad Detection**: Integrate `GamePad` state reads into `InputManager` alongside keyboard, normalising both into `InputAction` events.
- [ ] **Gamepad Bindings**: Define sensible gamepad defaults (d-pad/stick for movement, face buttons for attacks, start/select for confirm/back).
- [ ] **Gamepad UI Hints**: Show gamepad-specific control text (e.g., button icons or names) on the title screen and settings menu when a controller is detected.
- [ ] **Rebinding Screen**: Add a "Controls" section to the settings menu where each action can be reassigned a keyboard key or gamepad button.
- [ ] **Persistence**: Save rebound controls to a config file and load them on startup, so rebinds survive restart.
- [ ] **Conflict Detection**: Prevent or flag duplicate bindings when remapping (e.g., warn if two actions map to the same key).

---

## [ ] Milestone 15: Local Co-op Multiplayer

Add drop-in couch co-op where a second player joins on a second input device and plays alongside the first on a shared screen. *(Builds on Milestone 14 gamepad support — default binding: P1 keyboard, P2 gamepad.)*

- [ ] **Second Player Spawn**: Spawn a second `PlayerEntity` at level start (or drop-in mid-level) from the title/menu or via a join button.
- [ ] **Input Device Assignment**: Map `InputManager` actions to per-player input sources (e.g., P1 keyboard, P2 gamepad) so both players can act simultaneously.
- [ ] **Player Differentiation**: Apply distinct visual indicators (tint, name plate, or character select) so each player is identifiable on the shared screen.
- [ ] **Shared Camera**: Adjust the camera to frame both players, clamping to level bounds and re-centering as they move apart.
- [ ] **Friendly Fire**: Define whether players can damage each other (default off) and route hit detection accordingly.
- [ ] **Shared Progression**: Decide how lives, score, and continues aggregate across players (shared pool vs per-player).
- [ ] **Revive Mechanic**: Allow a live player to revive a downed teammate within a short window instead of burning a life.
- [ ] **HUD Scaling**: Adapt the HUD to show health/lives for both players without overlap.

---

## [ ] Milestone 16: Online Multiplayer

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

## Profile-First Investigation

- [ ] **`CollisionWorld2D.QueryCollisionPairs` per-frame allocation**: Called every frame in `GameLoop.ResolveCollisions`, this method is a compiler iterator — allocates a state-machine object + `HashSet<ActorPairKey>` per call. Profile to confirm it is a real GC pause contributor before vendoring/patching MonoGame.Extended.
