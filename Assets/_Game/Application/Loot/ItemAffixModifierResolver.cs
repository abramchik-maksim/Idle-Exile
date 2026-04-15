using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Application.Loot
{
    public sealed class ItemAffixModifierResolver : IItemAffixModifierResolver
    {
        private const string SourcePrefix = "affix_";

        private enum ConvertMode
        {
            None,
            PercentToFraction,
            ChanceToFraction
        }

        private readonly struct ModMapping
        {
            public readonly StatType Stat;
            public readonly ModifierType Type;
            public readonly ConvertMode Convert;

            public ModMapping(StatType stat, ModifierType type, ConvertMode convert = ConvertMode.None)
            {
                Stat = stat;
                Type = type;
                Convert = convert;
            }
        }

        private static readonly Dictionary<string, ModMapping> Map = new(StringComparer.Ordinal)
        {
            // ── Flat damage ──
            ["Damage_Flat_Physical"]    = new(StatType.PhysicalDamage,   ModifierType.Flat),
            ["Damage_Flat_Fire"]        = new(StatType.FireDamage,       ModifierType.Flat),
            ["Damage_Flat_Cold"]        = new(StatType.ColdDamage,       ModifierType.Flat),
            ["Damage_Flat_Lightning"]   = new(StatType.LightningDamage,  ModifierType.Flat),
            ["Damage_Flat_Corrosion"]   = new(StatType.CorrosionDamage,  ModifierType.Flat),

            // ── Increased damage (per-element) ──
            ["Damage_Increased_Physical"]   = new(StatType.PhysicalDamage,  ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Damage_Increased_Fire"]       = new(StatType.FireDamage,      ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Damage_Increased_Cold"]       = new(StatType.ColdDamage,      ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Damage_Increased_Lightning"]  = new(StatType.LightningDamage, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Damage_Increased_Corrosion"]  = new(StatType.CorrosionDamage, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Damage_Increased_All"]        = new(StatType.GlobalDamage,    ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Gain-as-element (% of phys added as element) ──
            ["Damage_GainAs_Fire"]       = new(StatType.GainAsFirePercent,      ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_GainAs_Cold"]       = new(StatType.GainAsColdPercent,      ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_GainAs_Lightning"]  = new(StatType.GainAsLightningPercent, ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_GainAs_Physical"]   = new(StatType.GainAsPhysicalPercent,  ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_GainAs_Corrosion"]  = new(StatType.GainAsCorrosionPercent, ModifierType.Flat, ConvertMode.PercentToFraction),

            // ── Penetration ──
            ["Damage_Penetration_Fire"]       = new(StatType.FirePenetration,       ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_Penetration_Cold"]       = new(StatType.ColdPenetration,       ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_Penetration_Lightning"]  = new(StatType.LightningPenetration,  ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Damage_Penetration_Corrosion"]  = new(StatType.CorrosionPenetration,  ModifierType.Flat, ConvertMode.PercentToFraction),

            // ── Double hit ──
            ["Damage_DoubleHit_All"]       = new(StatType.DoubleHitChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Damage_DoubleHit_Physical"]  = new(StatType.DoubleHitChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Damage_DoubleHit_Fire"]      = new(StatType.DoubleHitChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Damage_DoubleHit_Cold"]      = new(StatType.DoubleHitChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Damage_DoubleHit_Lightning"] = new(StatType.DoubleHitChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Damage_DoubleHit_Corrosion"] = new(StatType.DoubleHitChance, ModifierType.Flat, ConvertMode.ChanceToFraction),

            // ── Ignore armor ──
            ["Damage_IgnoreArmor"] = new(StatType.IgnoreArmorChance, ModifierType.Flat, ConvertMode.ChanceToFraction),

            // ── Defense flat ──
            ["Defense_Flat_Armor"]   = new(StatType.Armor,     ModifierType.Flat),
            ["Defense_Flat_Evasion"] = new(StatType.Evasion,   ModifierType.Flat),
            ["Defense_Flat_Health"]  = new(StatType.MaxHealth,  ModifierType.Flat),
            ["Defense_Flat_Barrier"] = new(StatType.Barrier,    ModifierType.Flat),

            // ── Defense increased ──
            ["Defense_Increased_Armor"]     = new(StatType.Armor,         ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Defense_Increased_Evasion"]   = new(StatType.Evasion,       ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Defense_Increased_Health"]    = new(StatType.MaxHealth,     ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Defense_Increased_Barrier"]   = new(StatType.Barrier,       ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Defense_Increased_LeechRate"] = new(StatType.LifeLeechRate, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Life leech ──
            ["Defense_LifeLeech"] = new(StatType.LifeLeech, ModifierType.Flat, ConvertMode.PercentToFraction),

            // ── Armor applies to non-physical ──
            ["Defense_ArmorToNonPhysical"] = new(StatType.ArmorAppliedToNonPhysical, ModifierType.Flat, ConvertMode.PercentToFraction),

            // ── Resistances ──
            ["Defense_Resist_Fire"]       = new(StatType.FireResistance,       ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Defense_Resist_Cold"]       = new(StatType.ColdResistance,       ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Defense_Resist_Lightning"]  = new(StatType.LightningResistance,  ModifierType.Flat, ConvertMode.PercentToFraction),
            ["Defense_Resist_Corrosion"]  = new(StatType.CorrosionResistance,  ModifierType.Flat, ConvertMode.PercentToFraction),

            // ── Block ──
            ["Block_Chance"]           = new(StatType.BlockChance, ModifierType.Flat,      ConvertMode.ChanceToFraction),
            ["Block_Chance_Increased"]  = new(StatType.BlockChance, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Crit ──
            ["Crit_Chance"]           = new(StatType.CriticalChance,     ModifierType.Flat,      ConvertMode.ChanceToFraction),
            ["Crit_Chance_Increased"] = new(StatType.CriticalChance,     ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Crit_Multiplier"]       = new(StatType.CriticalMultiplier, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Attack speed / cast speed ──
            ["Melee_AttackSpeed"]  = new(StatType.AttackSpeed, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ranged_AttackSpeed"] = new(StatType.AttackSpeed, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Spell_CastSpeed"]    = new(StatType.AttackSpeed, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Class-specific increased damage ──
            ["Melee_IncreasedDamage"]  = new(StatType.MeleeIncreasedDamage,  ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ranged_IncreasedDamage"] = new(StatType.RangedIncreasedDamage, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Spell_IncreasedDamage"]  = new(StatType.SpellIncreasedDamage,  ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Class-specific AoE ──
            ["Melee_AreaOfEffect"] = new(StatType.MeleeAreaOfEffect, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Spell_AreaOfEffect"] = new(StatType.SpellAreaOfEffect, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Projectile mechanics ──
            ["Ranged_PierceChance"] = new(StatType.RangedPierceChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ranged_ChainChance"]  = new(StatType.RangedChainChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ranged_ForkChance"]   = new(StatType.RangedForkChance,   ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Spell_PierceChance"]  = new(StatType.SpellPierceChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Spell_ChainChance"]   = new(StatType.SpellChainChance,   ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Spell_ForkChance"]    = new(StatType.SpellForkChance,    ModifierType.Flat, ConvertMode.ChanceToFraction),

            // ── Ailment chances ──
            ["Ailment_Chance_All"]    = new(StatType.AilmentChanceAll, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_Chance_Ignite"] = new(StatType.IgniteChance,     ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_Chance_Chill"]  = new(StatType.ChillChance,      ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_Chance_Shock"]  = new(StatType.ShockChance,      ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_Chance_Bleed"]  = new(StatType.BleedChance,      ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_Chance_Poison"] = new(StatType.PoisonChance,     ModifierType.Flat, ConvertMode.ChanceToFraction),

            // ── Ailment effect ──
            ["Ailment_Effect_All"]    = new(StatType.AilmentEffectAll, ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Effect_Ignite"] = new(StatType.IgniteEffect,     ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Effect_Chill"]  = new(StatType.ChillEffect,      ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Effect_Shock"]  = new(StatType.ShockEffect,      ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Effect_Bleed"]  = new(StatType.BleedEffect,      ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Effect_Poison"] = new(StatType.PoisonEffect,     ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Ailment duration ──
            ["Ailment_Duration_Generic"] = new(StatType.AilmentDuration, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Extra ailment stacks ──
            ["Ailment_ExtraStack_Chill"] = new(StatType.ExtraChillStacks, ModifierType.Flat),
            ["Ailment_ExtraStack_Shock"] = new(StatType.ExtraShockStacks, ModifierType.Flat),

            // ── Faster DoT ──
            ["Ailment_Faster_Bleed"]     = new(StatType.FasterBleed,     ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Faster_Ignite"]    = new(StatType.FasterIgnite,    ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Ailment_Faster_Corrosion"] = new(StatType.FasterCorrosion, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Ailment spread area ──
            ["Ailment_SpreadArea"] = new(StatType.AilmentSpreadArea, ModifierType.Increased, ConvertMode.PercentToFraction),

            // ── Ailment spread on hit (chance) ──
            ["Ailment_SpreadOnHit_Bleed"]   = new(StatType.BleedChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnHit_Ignite"]  = new(StatType.IgniteChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnHit_Chill"]   = new(StatType.ChillChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnHit_Shock"]   = new(StatType.ShockChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnHit_Poison"]  = new(StatType.PoisonChance, ModifierType.Flat, ConvertMode.ChanceToFraction),

            // ── Ailment spread on kill (chance) ──
            ["Ailment_SpreadOnKill_Bleed"]  = new(StatType.BleedChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnKill_Ignite"] = new(StatType.IgniteChance, ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnKill_Chill"]  = new(StatType.ChillChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnKill_Shock"]  = new(StatType.ShockChance,  ModifierType.Flat, ConvertMode.ChanceToFraction),
            ["Ailment_SpreadOnKill_Poison"] = new(StatType.PoisonChance, ModifierType.Flat, ConvertMode.ChanceToFraction),

            // ── Utility ──
            ["Utility_MovementSpeed"]       = new(StatType.MovementSpeed,       ModifierType.Flat),
            ["Utility_BuffDuration"]        = new(StatType.BuffDuration,         ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Utility_BuffEffect"]          = new(StatType.BuffEffect,           ModifierType.Increased, ConvertMode.PercentToFraction),
            ["Utility_CooldownRecoveryRate"] = new(StatType.CooldownRecoveryRate, ModifierType.Increased, ConvertMode.PercentToFraction),
        };

        public IReadOnlyList<Modifier> ResolveModifiers(RolledItemAffix affix)
        {
            string mod = affix.ModId ?? string.Empty;
            if (!Map.TryGetValue(mod, out var mapping))
                return Array.Empty<Modifier>();

            float v = affix.RolledValue;
            string src = SourcePrefix + affix.AffixId;

            float resolved = mapping.Convert switch
            {
                ConvertMode.PercentToFraction => v / 100f,
                ConvertMode.ChanceToFraction  => v / 100f,
                _ => v
            };

            return new[] { new Modifier(mapping.Stat, mapping.Type, resolved, src) };
        }
    }
}
