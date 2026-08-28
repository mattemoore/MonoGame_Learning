# [NOT IMPLEMENTED] Hoist Food/Weapon Pickup Entities to Core

**Verdict: REJECT** — no valid benefit. Push back; no fix plan.

## Analysis

The generic pickup machinery is **already** in Core:

- `MonoGameLearning.Core/Entities/Pickup/PickupBase.cs` — abstract texture/size/
  collision/render + abstract `OnPickup(IDamageable)`.
- `IPickup.cs`, `IPickupDropper.cs`, `PickupSpawnDef.cs`.

The two concrete entities in Game are thin game-content specializations:

- `FoodPickupEntity.cs` — heals a **game-balance constant** (`HealAmount = 15`).
- `WeaponPickupEntity.cs` — equips a concrete `MeleeWeaponDef` (game weapon).

`LevelDirectorCore`/`LevelDirector` already treat pickups generically via
`Func<PickupSpawnDef, Entity>` (`GameLoop.CreatePickup`, `GameLoop.cs:433-438`),
and `Level1` supplies the placements (`Level1.cs:32-36`).

## Why hoisting is the wrong direction

- These two classes encode this game's balance (15 HP heal) and content (the bat).
  A different game would have different heal amounts, weapons, or "coin" pickups.
- Moving them to Core would leak game content into the reusable engine, *reducing*
  reusability — the opposite of the stated goal.
- There is no duplication to remove: `PickupBase` already absorbs the shared render/
  collision/OnPickup plumbing. The subclasses are 16 lines each and correct.

## Decision

Leave `FoodPickupEntity` and `WeaponPickupEntity` in
`MonoGameLearning.Game/Entities/Pickups/`. The Core/Game split is already correct.

## Assumptions

- None.

## Follow-up questions (for a dedicated planning session)

- Is the underlying motivation "I want a generic `HealthPickupBase` for future games"? If so, that is premature abstraction — defer until a second game actually exists.