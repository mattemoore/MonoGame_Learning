# [IMPLEMENTED] Simplify OilDrum Behind a Single Entity

**Verdict: VALID (partial)** — collapse the auxiliary classes; keep the
"drop effective damage" design with a commented rationale.

## Current state (3 files, ~105 lines, one prop)

- `OilDrumEntity.cs` — entity (health 6, hit-stun via `_behavior`, `TakeDamage`).
- `OilDrumBehavior.cs` — a 0.3s hit-stun timer (6 fields/methods).
- `OilDrumDamage.cs` — `AttackStrength` → damage (`Heavy 6 / Medium 3 / else 2`).

`OilDrumEntity.TakeDamage` ignores `info.Amount` and substitutes
`OilDrumDamage.GetEffectiveDamage(info.Strength)`. This is the **only**
`IDamageable` in the codebase that translates strength tiers instead of using
the amount; every actor uses `CombatService.ApplyDamage` →
`ReduceHealth(info.Amount)`.

## Note on "merge into one file"

AGENTS.md requires "Every type … declared in its own file at namespace level."
So we **collapse** the small helper types (delete them, move their logic inline),
rather than putting multiple types in one file.

## Implemented changes

1. **Collapsed the hit-stun behavior into the entity.** Moved `_isHitStunned` /
   `_hitStunTimer` fields and the `CanTakeDamage`/`ApplyStun`/`Update` logic into
   `OilDrumEntity` (private members + `HitStunDuration` const). Deleted
   `OilDrumBehavior.cs`.
2. **Kept `OilDrumDamage.cs` and the strength mapping** (deliberate deviation
   from other entities — see comment below). Added the sprite-less internal test
   constructor (mirrors `PropBase` test overload).
3. **Added explanatory comments** in `OilDrumEntity.TakeDamage` and
   `OilDrumDamage.GetEffectiveDamage`: the drum's tiny max HP (6) is tiered by
   `AttackStrength` rather than budgeted to raw damage amounts, so rebalanced
   attack numbers elsewhere cannot silently change its designed hit count
   (heavy 1-hit / medium 2-hit / light 3-hit).
4. **Updated tests**:
   - Deleted `OilDrumBehaviorTests`; renamed `OilDrumStateTests.cs` →
     `OilDrumDamageTests.cs` (damage mapping tests unchanged).
   - Added entity-level `OilDrumEntityTests` (via `TestableOilDrumEntity` test
     double): heavy 1-hit, medium 2-hit, light 3-hit, amount is ignored, a
     surviving hit stuns and blocks the next, stun expires after 0.3s, dead drum
     ignores hits.
   - `OilDrumCollisionTests.cs` uses a standalone `TestPropCollision` — unaffected.

## Tradeoff (resolved)

The strength-mapping is **robust to future damage-number tuning** (a drum always
dies in N hits regardless of rebalanced attack amounts) at the cost of coupling
durability tiering to strength buckets instead of the shared damage amount. Kept
by decision, documented in code so future readers know it's intentional.

## Validation

- `dotnet build --warnaserror` — 0 warnings.
- `dotnet test` — 455 passed, 0 failed.

## Assumptions

- `DamageInfo.Strength` is always populated alongside `DamageInfo.Amount` by
  `HitboxService` (`HitboxService.cs:67`), which it is (`Damage = move.Damage`,
  `Strength = move.Strength`).
- The light/medium/heavy tiering is a deliberate gameplay requirement.

## Follow-up questions (for a dedicated planning session)

- Should the drum stay in Game (it uses `OilDrumSprite` + `AnimatedSprites`), or is
  there appetite to generalize a destructible-prop base into Core later?