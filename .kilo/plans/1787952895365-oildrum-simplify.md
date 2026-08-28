# [NOT IMPLEMENTED] Simplify OilDrum Behind a Single Entity

**Verdict: VALID (partial)** — collapse the auxiliary classes; treat the
"drop effective damage" half as a design decision with a documented tradeoff.

## Current state (3 files, ~105 lines, one prop)

- `OilDrumEntity.cs` — entity (health 6, hit-stun via `_behavior`, `TakeDamage`).
- `OilDrumBehavior.cs` — a 0.3s hit-stun timer (6 fields/methods).
- `OilDrumDamage.cs` — `AttackStrength` → damage (`Heavy 6 / Medium 3 / else 2`).

`OilDrumEntity.TakeDamage` (`OilDrumEntity.cs:44-62`) ignores `info.Amount`
entirely and substitutes `OilDrumDamage.GetEffectiveDamage(info.Strength)`. This
is the **only** `IDamageable` in the codebase that translates strength tiers
instead of using the amount; every actor uses `CombatService.ApplyDamage` →
`ReduceHealth(info.Amount)`.

## Note on "merge into one file"

AGENTS.md requires "Every type … declared in its own file at namespace level."
So we **collapse** the small helper types (delete them, move their logic inline),
rather than putting multiple types in one file.

## Tasks

1. **Collapse the hit-stun behavior into the entity.** Move the `_isHitStunned` /
   `_hitStunTimer` fields and the `CanTakeDamage`/`ApplyStun`/`Update` logic into
   `OilDrumEntity` (private members). Delete `OilDrumBehavior.cs`.
2. **Remove `OilDrumDamage.cs`.** In `OilDrumEntity.TakeDamage`, replace the
   strength translation with the direct amount:

   ```csharp
   if (IsHitStunned) return;          // former CanTakeDamage guard
   HealthComponent.Subtract(info.Amount);
   ```

   (Preserve the existing stun-on-surviving-hit and `PropExplosion`/`HitMetal` SFX.)
3. **Raise health to preserve current feel.** Current tiers (heavy 6 / med 3 /
   light 2 vs maxHP 6) = heavy 1-hit, medium 2-hit, light 3-hit. With direct
   amounts (player heavy 12 / medium 8 / light 5, enemy light 5), `maxHealth = 12`
   reproduces the same hit counts exactly. Change the `base(..., 6, ...)` health
   argument to `12` and update `SelectAnimation` thresholds `<= 2` / `<= 4` →
   `<= 4` / `<= 8` (≈ 1/3 and 2/3 of 12).
4. **Update tests** in `OilDrumStateTests.cs`:
   - Delete/replace `OilDrumBehaviorTests` and `OilDrumDamageTests` (they test the
     deleted types).
   - Add entity-level tests: heavy (Amount 12) destroys in 1 hit; medium (8) needs
     2; light (5) needs 3; a surviving hit stuns and blocks the next hit for 0.3s.
   - `OilDrumCollisionTests.cs` uses a standalone `TestPropCollision` (only
     `PropBase.ComputeCollisionBounds`) — unaffected.

## Tradeoff to confirm (push-back)

The strength-mapping is currently **robust to future damage-number tuning** (a
drum always dies in N hits regardless of rebalanced attack amounts). Switching to
`info.Amount` couples drum durability to today's exact player damage numbers and
slightly changes bat-swing behavior (bat = 6 damage → 2 hits instead of 3). If the
light/medium/heavy tiering is deliberate "armor" design, keep `OilDrumDamage` and
only do tasks 1 + stun-inline. Default assumption for this plan: the tiering is
incidental fluff and direct-amount is preferred.

## Validation

- `dotnet build --warnaserror`; `dotnet test`.

## Assumptions

- `DamageInfo.Amount` is always populated by `HitboxService` (`HitboxService.cs:67`),
  which it is (`Damage = move.Damage`).
- The light/medium/heavy tiering is not a deliberate gameplay requirement.

## Follow-up questions (for a dedicated planning session)

- Is the "heavy one-shots the drum, light takes three hits" feel a hard design rule, or just what the code happens to do?
- Should the drum stay in Game (it uses `OilDrumSprite` + `AnimatedSprites`), or is there appetite to generalize a destructible-prop base into Core later?
