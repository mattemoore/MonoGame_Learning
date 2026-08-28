# [NOT IMPLEMENTED] Clarify LevelDirector Composition-Root (typically no change)

**Verdict: REJECT** — no correctness/maintenance defect. The code is the intended
composition-root pattern; the "weirdness" is a misreading.

## What the code actually does

`GameLoop.InitLevelSystems` (`GameLoop.cs:411-422`) constructs `LevelDirector` and
passes six method groups:

```csharp
CreateProp,            // Func<PropSpawnDef, PropBase>
CreatePickup,          // Func<PickupSpawnDef, Entity>
BatWeapon.Get,         // Func<string, MeleeWeaponDef>
CreateEnemy,           // Func<string, int, Func<WorldSnapshot>, EnemyEntity>
ConfigureSpawnedEnemy, // Action<EnemyEntity, EnemySpawnDef, FacingDirection, MeleeWeaponDef?>
GetCameraView          // Func<RectangleF>
```

These are **not** "parameterless method calls" — they are delegates (factory
functions) passed as arguments. `BatWeapon.Get` is a static method group bound to
`Func<string, MeleeWeaponDef>`. This is exactly the dependency-injection strategy
documented in AGENTS.md:

> "Game content (drums, pickups, weapons, enemy visuals, spawn-walk) is injected
> via createProp/createPickup/getWeapon/createEnemy/onEnemySpawned delegates."

## Why this is correct

- `LevelDirectorCore<TEnemy>` lives in Core and must not reference Game types.
  Injecting factories keeps Core decoupled from game content.
- The closure over `_audio`, `_player`, `Content`, etc. is idiomatic C# and
  allocation-free after construction (the delegates are built once per level init,
  not per frame).
- This is the game-side composition root: it is *supposed* to be the one place that
  knows all concrete types.

## Minor observations (not blockers)

- The 9-argument constructor is a mild *future* smell (positional same-shaped
  `Func`s are easy to mis-order and compile silently). If `LevelDirectorCore`
  grows, introduce a `LevelDirectorConfiguration` record. Not warranted today.
- Wrapping the method groups in named `Func<...>` locals would add noise without
  adding meaning; not recommended.

## Decision

No fix plan. Optionally, if readability is genuinely a concern, add a brief comment
above the constructor call mapping each argument to its `LevelDirectorCore`
parameter — cosmetic only.

## Assumptions

- The user read the method groups as "calls" and found them unusual; the underlying
  design (delegate injection) is accepted per AGENTS.md.

## Follow-up questions (for a dedicated planning session)

- Is the real pain point "too many constructor parameters"? If so, a config-record
  refactor of `LevelDirectorCore` could be planned separately.