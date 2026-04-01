# Loot And Economy

## Legacy Baseline (from GAME_DESIGN.md)

### Loot System

Items drop from defeated enemies during combat:

- **Base drop chance:** 30% + 2.5% per battle index (capped at 65%)
- **Bonus per tier:** +10% per tier
- **Modifier rolls:** random values between 1-10
- Items roll random modifiers based on rarity tier

### Rewards

| Reward Type | Formula                      | Frequency       |
|-------------|------------------------------|-----------------|
| Experience  | 10 + battleIndex x 5         | Every battle    |
| Gold        | 5 + battleIndex x 2          | Every 3rd battle |
| Item Drop   | 30-65% chance per enemy kill | Random per enemy |

## Current Implementation (Code-Backed)

### Formula Sources

- `Assets/_Game/Application/Combat/GrantBattleRewardUseCase.cs`
- `Assets/_Game/Infrastructure/Configs/Combat/ScriptableObjectCombatConfigProvider.cs`

### Runtime Loot Logic

- Primary drop chance:
  `dropChance = min(baseDropChance + battleIndex * dropChancePerBattle, maxDropChance)`
- If drop roll succeeds, one item is rolled.
- Bonus roll:
  `bonusChance = tierIndex * bonusDropChancePerTier`
- If bonus roll succeeds, one additional item is rolled.

### Implementation Notes

- Runtime orchestration is currently item-drop centric through `GrantBattleRewardUseCase`.
- Legacy reward table includes XP and Gold formulas as baseline design context; those loops can remain target/planned unless explicitly wired in reward use cases.

## Target Design

- Expand currency loops and sinks while keeping drop formulas transparent.
- Keep reward formulas documented separately from presentation-layer text.
