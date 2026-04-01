# Combat

## Legacy Baseline (from GAME_DESIGN.md)

### Progression Hierarchy

```text
Tier (Act I, Act II, ...)
 └── Map (Twilight Shore, ...)
      └── Battle (1-10 per map)
           └── Wave (2-4 per battle, invisible to player)
                └── Enemy Spawns
```

The player sees: **Tier Name** + **Battle N / Total**. Waves are internal pacing and auto-advance once all enemies in a wave are defeated.

### Battle Flow

1. Battle starts -> first wave spawns after short delay.
2. Enemies march downward; hero auto-fires projectiles at nearest enemy.
3. Projectiles are homing and deal damage on hit.
4. When all enemies in a wave die -> next wave spawns (after delay).
5. When all waves are cleared -> battle completes.
6. Rewards granted -> next battle auto-starts.
7. After final battle in map -> advance to next map/tier.

### Enemy Archetypes

Enemies are categorized into three behavior archetypes:

| Archetype | Behavior | Attack Style |
|-----------|----------|-------------|
| Melee | Moves to target, stops at AttackRange, wind-up AoE hit | Semi-transparent red circle around attacker during wind-up, damages all targetable entities in radius |
| Ranged | Moves to AttackRange (5-8 units), fires homing projectiles | Orange enemy projectiles, slower than hero projectiles (speed = 8) |
| Caster | Stays at max range, casts spells with cast bar | Cast bar above enemy, interruptible by Silence/Stun/Knockback/Kill. Creates AoE damage zone on target position |

### Enemies

| Enemy    | HP  | Damage | Armor | Speed | Archetype | AttackRange | Appears In         |
|----------|-----|--------|-------|-------|-----------|-------------|-------------------|
| Skeleton | 30  | 5      | 2     | 2.0   | Melee     | 1.0         | Battles 0-9       |
| Zombie   | 50  | 8      | 4     | 1.2   | Melee     | 1.2         | Battles 5+ (boss) |
| Ghost    | 20  | 10     | 0     | 3.0   | Ranged    | 6.0         | Battles 7+ (adds) |

### Targeting System

Enemies select targets using aggro-weighted formula: `score = aggroWeight / distance`.

| Targetable Object | Aggro Weight | Notes |
|-------------------|-------------|-------|
| Hero | 10 | Default target |
| Clone (utility skill) | 15 | Draws enemy attention away from hero |

### Scaling

- **Tier scaling:** `1.0 + tierIndex * 0.5` (multiplies enemy HP, damage, armor)
- **Wave count:** `min(2 + battleIndex/4, 4)` waves per battle
- **Enemy count:** `min(2 + battleIndex/3 + waveIndex, 8)` enemies per wave

### Damage Model

- Hero fires projectiles at nearest enemy.
- Damage = hero physical damage (modified by main skill multiplier).
- Attack rate = hero attack speed (modified by main skill multiplier).
- Projectiles are homing, speed = 12 units/second.
- Damage numbers appear at hit position (larger + yellow for crits).
- Enemies attack hero (melee AoE, ranged projectiles, or caster spells).
- Armor reduction formula: `reduction = armor / (armor + 10 * rawDamage)`.

### Visual Representation

All combat entities are rendered as instanced quads (no GameObjects):

- **Hero:** Blue quad
- **Enemy:** Red quad
- **Hero Projectile:** Yellow quad
- **Enemy Projectile:** Orange quad
- **Clone:** Green quad (slightly smaller than hero)
- **Melee AoE Zone:** Semi-transparent red circle (during wind-up)
- **Spell AoE Zone:** Semi-transparent purple circle (during cast delay)
- **HP Bars:** Green fill on dark red background (above enemy, hidden at full HP)
- **Cast Bars:** Purple fill on dark background (above casting enemy)
- **Effect Icons:** Colored squares below HP bar (orange=Ignite, blue=Chill, yellow=Shock, red=Bleed, gray=Stun, purple=Silence)

Damage numbers use pooled world-space TextMeshPro elements. All visual indicators can be toggled via Settings.

## Current Implementation (Code-Backed)

### Formula Sources

- `Assets/_Game/Domain/Stats/StatCollection.cs`
- `Assets/_Game/Domain/Combat/DamageCalculator.cs`
- `Assets/_Game/Presentation/Combat/Systems/HeroAttackSystem.cs`
- `Assets/_Game/Presentation/Combat/CombatBridge.cs`

### Damage Types and Stat Aggregation

- Types in active design model: `Lightning`, `Fire`, `Cold`, `Physical`, `Corrosion`.
- Stateless aggregation rule by type: `base + flat -> increased -> more`.

`typedValue[t] = (base[t] + flat[t]) * (1 + sumIncreased[t]) * product(1 + more[t])`

### Gain As Model (Finalized)

Gain works from a snapshot of base pools and does not recurse.

- `base[t]`: flat pool of type `t` before any gain transfer.
- `gain[s->t]`: gain percentage from source type `s` to target type `t`.
- Same-type gain is allowed (`Physical -> Physical`).

Order:

1. Build `base[t]` for all types.
2. `gainAdd[t] = sum_over_sources(base[s] * gain[s->t])`
3. `preScale[t] = base[t] + gainAdd[t]`
4. `scaled[t] = preScale[t] * (1 + inc[t]) * moreMul[t]`
5. Apply mitigation.
6. Sum all `dealt[t]`.

Constraint:

- `gainAdd` must read only `base` values, never `preScale`.

### Penetration Model (Finalized)

Penetration acts on target defenses during mitigation:

- `effectiveResist[t] = targetResistOrArmor[t] - penetration[t]`
- `dealt[t] = scaled[t] * (1 - effectiveResist[t])`

Rules:

- No lower clamp on effective resist.
- Negative effective resist increases damage above baseline.
- Penetration is mitigation-stage only.

### Runtime Pipeline Summary

1. Build attacker flat pools by type.
2. Apply gain transfer from base snapshot.
3. Apply increased/more by type.
4. Resolve critical hit at attack resolution.
5. Apply mitigation against target defenses.
6. Produce per-type dealt values and total hit.

### Implementation Notes

- Current runtime code still has a legacy armor-focused mitigation path in several systems.
- `DamageCalculator.CalculateMultiType` currently models gain-as from physical into elemental channels; full 5x5 typed transfer matrix is target behavior.
- Keep this file as contract: `Legacy baseline` documents prior spec, `Current implementation` documents code-backed behavior, and this section tracks migration gaps.

## Target Design

- Full typed runtime support for all five damage types in hit, DoT, UI, and logs.
- Unified non-recursive gain contract across all combat systems.
