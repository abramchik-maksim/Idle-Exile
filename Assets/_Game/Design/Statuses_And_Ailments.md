# Statuses And Ailments

## Legacy Baseline (from GAME_DESIGN.md)

All ailments have a proc chance defined on skills and gear bonuses.

### Elemental Ailments

| Ailment | Stacking | Effect | Duration |
|---------|----------|--------|----------|
| Ignite | 1 stack (re-applied overwrites) | DoT = 300% of triggering hit damage | 3 seconds (6 ticks x 0.5s) |
| Chill | Up to 10 stacks | -5% MoveSpeed/AttackSpeed/CastSpeed per stack. At 10 stacks: reset + Freeze (3s Stun) | Persistent while stacked |
| Shock | Up to 10 stacks | +5% damage taken per stack | Persistent while stacked |

### Physical Ailments

| Ailment | Stacking | Effect | Duration |
|---------|----------|--------|----------|
| Bleed | Infinite (independent timers) | DoT = 60% of triggering hit damage | 5 seconds (10 ticks x 0.5s) |

### Corrosion Ailments

| Ailment | Stacking | Effect | Duration |
|---------|----------|--------|----------|
| Poison | Infinite (independent timers) | 120% of triggering hit damage after duration | 3s (Delayed burst) |

### Control Effects

| Effect | Action | Interrupts Cast? |
|--------|--------|-----------------|
| Silence | Cannot start/continue casting | Yes |
| Stun | Cannot move, attack, or cast | Yes |
| Slow | Reduces MoveSpeed by % | No |
| Knockback | Pushback + brief Stun (0.5s) | Yes |
| Freeze | = Stun for 3s (from 10 Chill stacks) | Yes |

## Current Implementation (Code-Backed)

### Formula Sources

- `Assets/_Game/Domain/Combat/AilmentCalculator.cs`
- `Assets/_Game/Presentation/Combat/Systems/AilmentTickSystem.cs`

### Active Runtime Behavior

- Ignite uses `IgniteTotalDamagePercent = 3.0`, `IgniteDuration = 3s`, `IgniteTickCount = 6`.
- Bleed uses `BleedDamagePercent = 0.6`, `BleedDuration = 5s`, `BleedTickCount = 10`.
- Global ailment tick cadence is `AilmentTickInterval = 0.5s`.
- Chill stack cap is 10; at 10 stacks freeze is applied for `FreezeDuration = 3s`.
- Bleed display and Ignite display are accumulated and flushed on tick cadence.

### Implementation Notes

- Shock stack tracking exists in constants and data paths. Shock damage multiplier wiring is not consistently applied across all hit paths yet.
- DoT damage is applied by tick systems directly to health and rendered as dedicated damage categories.

## Target Design

- Expand typed ailment support to include complete corrosion branch behavior.
- Keep ailment formulas centralized and code-referenced in one place for balancing.
