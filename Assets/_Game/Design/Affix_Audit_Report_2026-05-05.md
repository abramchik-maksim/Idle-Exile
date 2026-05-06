# Affix Audit Report (2026-05-05)

## Summary
- Total unique modId in pool: **94**
- IMPLEMENTED_RUNTIME: **62**
- UI_ONLY (mapped/displayed but not consumed in combat runtime): **23**
- RESOLVER_ONLY (mapped but no runtime/UI usage): **9**

## High-impact fixes applied in this pass
- Added corrosion damage into runtime combat path (combat stats, damage breakdown/calculator, hero attack total).
- Added gear `GainAsPhysicalPercent` and `GainAsCorrosionPercent` into runtime damage conversion path.
- Extended Character tab visibility/formatting for projectile proc chances, extra gain-as lines, and penetration lines.

## Backlog (next implementation batch)
- Poison branch: `PoisonChance` mods are currently UI-only because combat ailment runtime has no poison type/system.
- Ailment effect/duration/faster/extra-stacks/spread-area stats are mostly mapped but not consumed by ailment tick/stack systems.
- `ArmorAppliedToNonPhysical`, `MeleeAreaOfEffect`, `SpellAreaOfEffect` are mapped but not consumed in runtime mechanics.
- `LifeLeechRate`, `Barrier`, and class-specific increased-damage stats need explicit runtime hooks or explicit de-scope.

## Per-mod status

| modId | Stat | RuntimeStatus | FinalStatus | gameplayHits | uiHits |
|---|---|---|---:|---:|---:|
| Ailment_Chance_All | AilmentChanceAll | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_Chance_Bleed | BleedChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_Chance_Chill | ChillChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_Chance_Ignite | IgniteChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_Chance_Poison | PoisonChance | UI_ONLY | Partial | 0 | 1 |
| Ailment_Chance_Shock | ShockChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_Duration_Generic | AilmentDuration | UI_ONLY | Partial | 0 | 1 |
| Ailment_Effect_All | AilmentEffectAll | UI_ONLY | Partial | 0 | 1 |
| Ailment_Effect_Bleed | BleedEffect | UI_ONLY | Partial | 0 | 1 |
| Ailment_Effect_Chill | ChillEffect | UI_ONLY | Partial | 0 | 1 |
| Ailment_Effect_Ignite | IgniteEffect | UI_ONLY | Partial | 0 | 1 |
| Ailment_Effect_Poison | PoisonEffect | UI_ONLY | Partial | 0 | 1 |
| Ailment_Effect_Shock | ShockEffect | UI_ONLY | Partial | 0 | 1 |
| Ailment_ExtraStack_Chill | ExtraChillStacks | RESOLVER_ONLY | Deferred | 0 | 0 |
| Ailment_ExtraStack_Shock | ExtraShockStacks | RESOLVER_ONLY | Deferred | 0 | 0 |
| Ailment_Faster_Bleed | FasterBleed | RESOLVER_ONLY | Deferred | 0 | 0 |
| Ailment_Faster_Corrosion | FasterCorrosion | RESOLVER_ONLY | Deferred | 0 | 0 |
| Ailment_Faster_Ignite | FasterIgnite | RESOLVER_ONLY | Deferred | 0 | 0 |
| Ailment_SpreadArea | AilmentSpreadArea | RESOLVER_ONLY | Deferred | 0 | 0 |
| Ailment_SpreadOnHit_Bleed | BleedChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnHit_Chill | ChillChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnHit_Ignite | IgniteChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnHit_Poison | PoisonChance | UI_ONLY | Partial | 0 | 1 |
| Ailment_SpreadOnHit_Shock | ShockChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnKill_Bleed | BleedChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnKill_Chill | ChillChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnKill_Ignite | IgniteChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ailment_SpreadOnKill_Poison | PoisonChance | UI_ONLY | Partial | 0 | 1 |
| Ailment_SpreadOnKill_Shock | ShockChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Block_Chance | BlockChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Block_Chance_Increased | BlockChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Crit_Chance | CriticalChance | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Crit_Chance_Increased | CriticalChance | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Crit_Multiplier | CriticalMultiplier | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_DoubleHit_All | DoubleHitChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_DoubleHit_Cold | DoubleHitChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_DoubleHit_Corrosion | DoubleHitChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_DoubleHit_Fire | DoubleHitChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_DoubleHit_Lightning | DoubleHitChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_DoubleHit_Physical | DoubleHitChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_Flat_Cold | ColdDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Flat_Corrosion | CorrosionDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Flat_Fire | FireDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Flat_Lightning | LightningDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Flat_Physical | PhysicalDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_GainAs_Cold | GainAsColdPercent | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_GainAs_Corrosion | GainAsCorrosionPercent | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_GainAs_Fire | GainAsFirePercent | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_GainAs_Lightning | GainAsLightningPercent | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_GainAs_Physical | GainAsPhysicalPercent | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_IgnoreArmor | IgnoreArmorChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Damage_Increased_All | GlobalDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Increased_Cold | ColdDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Increased_Corrosion | CorrosionDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Increased_Fire | FireDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Increased_Lightning | LightningDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Increased_Physical | PhysicalDamage | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Damage_Penetration_Cold | ColdPenetration | UI_ONLY | Partial | 0 | 1 |
| Damage_Penetration_Corrosion | CorrosionPenetration | UI_ONLY | Partial | 0 | 1 |
| Damage_Penetration_Fire | FirePenetration | UI_ONLY | Partial | 0 | 1 |
| Damage_Penetration_Lightning | LightningPenetration | UI_ONLY | Partial | 0 | 1 |
| Defense_ArmorToNonPhysical | ArmorAppliedToNonPhysical | RESOLVER_ONLY | Deferred | 0 | 0 |
| Defense_Flat_Armor | Armor | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Defense_Flat_Barrier | Barrier | UI_ONLY | Partial | 0 | 1 |
| Defense_Flat_Evasion | Evasion | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Flat_Health | MaxHealth | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Increased_Armor | Armor | IMPLEMENTED_RUNTIME | Done | 2 | 1 |
| Defense_Increased_Barrier | Barrier | UI_ONLY | Partial | 0 | 1 |
| Defense_Increased_Evasion | Evasion | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Increased_Health | MaxHealth | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Increased_LeechRate | LifeLeechRate | UI_ONLY | Partial | 0 | 1 |
| Defense_LifeLeech | LifeLeech | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Resist_Cold | ColdResistance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Resist_Corrosion | CorrosionResistance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Resist_Fire | FireResistance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Defense_Resist_Lightning | LightningResistance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Melee_AreaOfEffect | MeleeAreaOfEffect | RESOLVER_ONLY | Deferred | 0 | 0 |
| Melee_AttackSpeed | AttackSpeed | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Melee_IncreasedDamage | MeleeIncreasedDamage | UI_ONLY | Partial | 0 | 1 |
| Ranged_AttackSpeed | AttackSpeed | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ranged_ChainChance | RangedChainChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ranged_ForkChance | RangedForkChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Ranged_IncreasedDamage | RangedIncreasedDamage | UI_ONLY | Partial | 0 | 1 |
| Ranged_PierceChance | RangedPierceChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Spell_AreaOfEffect | SpellAreaOfEffect | RESOLVER_ONLY | Deferred | 0 | 0 |
| Spell_CastSpeed | AttackSpeed | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Spell_ChainChance | SpellChainChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Spell_ForkChance | SpellForkChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Spell_IncreasedDamage | SpellIncreasedDamage | UI_ONLY | Partial | 0 | 1 |
| Spell_PierceChance | SpellPierceChance | IMPLEMENTED_RUNTIME | Done | 1 | 1 |
| Utility_BuffDuration | BuffDuration | UI_ONLY | Partial | 0 | 1 |
| Utility_BuffEffect | BuffEffect | UI_ONLY | Partial | 0 | 1 |
| Utility_CooldownRecoveryRate | CooldownRecoveryRate | UI_ONLY | Partial | 0 | 1 |
| Utility_MovementSpeed | MovementSpeed | IMPLEMENTED_RUNTIME | Done | 1 | 1 |