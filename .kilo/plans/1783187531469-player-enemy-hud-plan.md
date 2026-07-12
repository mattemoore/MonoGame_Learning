# Plan: Dynamic Focus HUD (Final Fight Style)

## Context

`ROADMAP.md` Milestone 5 has three unchecked items. This plan covers two: Player HUD and Enemy/Prop HUD. **Score and timer are out of scope** — explicitly excluded, no score counter anywhere.

The HUD follows the **Capcom Dynamic Focus HUD** pattern from *Final Fight* — a priority-based pointer system that shows only the relevant target's health, rather than every entity at once.

## Layout

```
+--+-----------------+
|  |  CODY  =3       |
|  |[================== HEALTH BAR ====================]  |
+--+-----------------+
| +----+                                                 |
| | 🙂 | BRED                                            |
| +----+ [======= TARGET HEALTH BAR =====]               |
+---------------------------------------------------------+
```

**Top section (player metrics):**

- **Mugshot**: Static portrait placeholder (drawn primitive or texture).
- **Name + lives**: `"{PLAYER_NAME} ={LIVES}"` — text string, not just circles.
- **No score**: Score counter is intentionally excluded.
- **Health bar**: Long yellow bar over dark red background, right-to-left depletion.

**Bottom section (contextual target):**

- **Enemy portrait**: Small mugshot placeholder (same style as player mugshot, smaller).
- **Dynamic name ID**: Entity's `Name` string.
- **Target health bar**: Shorter red bar (60% width of player bar). Visible only during interaction.

**Red X death indicator (on portrait, in HUD):**

- When the tracked target enemy dies, a **red "X"** is drawn over the enemy's portrait in the HUD target bar. This is the Capcom convention — the X marks the enemy as eliminated. The X stays visible for the linger duration (1.5s) while the death animation plays, then the HUD hides the target bar entirely.

**Red X world-space indicator:**

- Additionally (or alternatively), a red X can be drawn over the enemy's sprite in world space during the Dying/Dead state. Removed when the entity is cleaned up.

## Priority-Based Pointer System

A strict hierarchy determines what the target HUD shows:

1. **Active attack/grab** (highest): The entity currently being hit by the player. Piped via `OnEnemyHit`/`OnPropHit`. HUD locks until target dies or player stops hitting for 1.5s.
2. **Proximity aggro** (secondary): If no active target, the HUD shows the closest enemy who last dealt damage to the player, or the nearest enemy by X/Y.
3. **Prop interaction** (lowest): Striking a breakable object shows its name and durability bar.

## Decisions

- **HudService is the single HUD entry point** at `MonoGameLearning.Core/UI/HudService.cs`. Owns two widget subsystems: `_playerMetrics` and `_targetBar`.
- **`_playerMetrics` renders**: mugshot placeholder (rectangle + letter), name string (`"CODY =3"`), player health bar (yellow/dark red).
- **`_targetBar` renders**: enemy portrait (small mugshot), entity `Name` label, shorter red health bar. When target dies, draws red X overlay over the portrait. Hidden when no target.
- **`_hudRoot : UiBase`** wraps both, registered once with `_entityManager`.
- **Lives:** `PlayerEntity.Lives` (int, starts at 3). Displayed as `=3`, `=2`, etc. Zero lives → `GameOver`.
- **Respawn:** Same formula (`clamp(Camera.X + 60, levelLeft + 10, levelRight - 10)`) with 2.5s invincibility.
- **No score:** `PlayerEntity.Score` is not added. No score display, no extra-life-from-score mechanic.
- **Health bar colors:** Yellow (player) / Red (target) over dark red background. No green/yellow/red gradient.
- **Entity name display:** Uses `Entity.Name` — already present on all entities.
- **Red X death indicator:** Drawn in the **HUD target bar** over the enemy portrait when `target.IsAlive == false` during the linger period. Optionally also drawn in world space over the enemy sprite during Dying/Dead state — removed when entity is cleaned up.

## Data Flow

```
Player hits enemy → HitboxService.ResolveHits()
  → damageable.TakeDamage()
  → _hudService.OnEnemyHit(enemy)
  → _targetBar locks on, 1.5s linger timer resets

No active hits → _targetBar checks proximity aggro each frame
  → scans _entityManager.All for nearest IDamageable
  → shows nearest enemy or prop that has been hit recently

Target dies:
  → _targetBar draws red X over enemy portrait (1.5s linger)
  → timer expires → _targetBar hides

Enemy hits player → _playerMetrics reads HealthComponent, updates bar
```

## In Scope

- `HudService.cs` rewrite — player mugshot, name+lives, yellow health bar; target enemy portrait, name label, red health bar, red X overlay on death
- Use `Entity.Name` for label display (already available)
- Proximity-based target fallback in `GameLoop.Update`
- Red X drawn over enemy portrait in HUD when target dies
- Test updates for new visual format

## Out of Scope

- Pixel-art mugshot texture (use colored rectangle + letter placeholder)
- Boss health bar at bottom of screen
- Gum UI panels

## Risks & Edge Cases

- **Multiple targets same frame:** Last hit wins. 1.5s timer resets each hit.
- **Proximity vs hit priority:** If player is hitting enemy A but enemy B is closer and was hit 0.5s ago, enemy A wins (attack priority > proximity). Attack priority lockout is 1.5s from last hit.
- **Red X on prop death:** Props don't have portraits — the red X overlay only applies to enemies with mugshots. Props just hide immediately on destruction.
- **Linger after death:** The target bar with red X stays visible for 1.5s after death so the player sees the X before it disappears.

## Validation

- `dotnet build` — 0 warnings
- `dotnet test` — all tests pass
- Manual: hit an enemy → see enemy portrait + name + red health bar. Kill enemy → see red X overlay on portrait for 1.5s then HUD hides. Hit a prop → see prop name + red durability bar. Lose a life → `=3` → `=2`.
