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

        public IReadOnlyList<Modifier> ResolveModifiers(RolledItemAffix affix)
        {
            var list = new List<Modifier>();
            float v = affix.RolledValue;
            string fmt = affix.ValueFormat ?? string.Empty;
            string mod = affix.ModId ?? string.Empty;
            string src = SourcePrefix + affix.AffixId;

            if (TryResolveDamageFlat(mod, v, src, list)) return list;
            if (TryResolveDamageIncreased(mod, v, fmt, src, list)) return list;
            if (TryResolveDefenseFlat(mod, v, src, list)) return list;
            if (TryResolveDefenseIncreased(mod, v, fmt, src, list)) return list;
            if (TryResolveCrit(mod, v, fmt, src, list)) return list;
            if (TryResolveSpeed(mod, v, fmt, src, list)) return list;
            if (TryResolveUtility(mod, v, fmt, src, list)) return list;

            return list;
        }

        private static bool IsPercentFormat(string fmt) =>
            string.Equals(fmt, "Percent", StringComparison.OrdinalIgnoreCase);

        private static bool IsChancePercentFormat(string fmt) =>
            string.Equals(fmt, "ChancePercent", StringComparison.OrdinalIgnoreCase);

        private static float AsIncreasedFraction(string fmt, float rolled)
        {
            if (IsPercentFormat(fmt) || string.Equals(fmt, "FlatNumber", StringComparison.OrdinalIgnoreCase) == false && rolled > 2f)
                return rolled / 100f;
            return rolled;
        }

        private static bool TryResolveDamageFlat(string mod, float v, string src, List<Modifier> list)
        {
            if (!mod.StartsWith("Damage_Flat_", StringComparison.Ordinal))
                return false;

            StatType? st = mod switch
            {
                "Damage_Flat_Physical" => StatType.PhysicalDamage,
                "Damage_Flat_Fire" => StatType.FireDamage,
                "Damage_Flat_Cold" => StatType.ColdDamage,
                "Damage_Flat_Lightning" => StatType.LightningDamage,
                _ => null
            };
            if (st == null) return true;
            list.Add(new Modifier(st.Value, ModifierType.Flat, v, src));
            return true;
        }

        private static bool TryResolveDamageIncreased(string mod, float v, string fmt, string src, List<Modifier> list)
        {
            if (!mod.StartsWith("Damage_Increased_", StringComparison.Ordinal)) return false;

            float inc = AsIncreasedFraction(fmt, v);
            if (mod == "Damage_Increased_All")
            {
                list.Add(new Modifier(StatType.GlobalDamage, ModifierType.Increased, inc, src));
                return true;
            }

            StatType? st = mod switch
            {
                "Damage_Increased_Physical" => StatType.PhysicalDamage,
                "Damage_Increased_Fire" => StatType.FireDamage,
                "Damage_Increased_Cold" => StatType.ColdDamage,
                "Damage_Increased_Lightning" => StatType.LightningDamage,
                _ => null
            };
            if (st != null)
                list.Add(new Modifier(st.Value, ModifierType.Increased, inc, src));
            return true;
        }

        private static bool TryResolveDefenseFlat(string mod, float v, string src, List<Modifier> list)
        {
            switch (mod)
            {
                case "Defense_Flat_Armor":
                    list.Add(new Modifier(StatType.Armor, ModifierType.Flat, v, src));
                    return true;
                case "Defense_Flat_Evasion":
                    list.Add(new Modifier(StatType.Evasion, ModifierType.Flat, v, src));
                    return true;
                case "Defense_Flat_Health":
                    list.Add(new Modifier(StatType.MaxHealth, ModifierType.Flat, v, src));
                    return true;
                case "Defense_Flat_Barrier":
                    return true;
            }
            return false;
        }

        private static bool TryResolveDefenseIncreased(string mod, float v, string fmt, string src, List<Modifier> list)
        {
            float inc = AsIncreasedFraction(fmt, v);
            switch (mod)
            {
                case "Defense_Increased_Armor":
                    list.Add(new Modifier(StatType.Armor, ModifierType.Increased, inc, src));
                    return true;
                case "Defense_Increased_Evasion":
                    list.Add(new Modifier(StatType.Evasion, ModifierType.Increased, inc, src));
                    return true;
                case "Defense_Increased_Health":
                    list.Add(new Modifier(StatType.MaxHealth, ModifierType.Increased, inc, src));
                    return true;
                case "Defense_Increased_Barrier":
                case "Defense_Increased_LeechRate":
                    return true;
            }
            return false;
        }

        private static bool TryResolveCrit(string mod, float v, string fmt, string src, List<Modifier> list)
        {
            switch (mod)
            {
                case "Crit_Chance":
                    float chance = IsChancePercentFormat(fmt) ? v / 100f : v;
                    list.Add(new Modifier(StatType.CriticalChance, ModifierType.Flat, chance, src));
                    return true;
                case "Crit_Chance_Increased":
                    list.Add(new Modifier(StatType.CriticalChance, ModifierType.Increased, AsIncreasedFraction(fmt, v), src));
                    return true;
                case "Crit_Multiplier":
                    list.Add(new Modifier(StatType.CriticalMultiplier, ModifierType.Increased, AsIncreasedFraction(fmt, v), src));
                    return true;
            }
            return false;
        }

        private static bool TryResolveSpeed(string mod, float v, string fmt, string src, List<Modifier> list)
        {
            switch (mod)
            {
                case "Melee_AttackSpeed":
                case "Ranged_AttackSpeed":
                case "Spell_CastSpeed":
                    list.Add(new Modifier(StatType.AttackSpeed, ModifierType.Increased, AsIncreasedFraction(fmt, v), src));
                    return true;
            }
            return false;
        }

        private static bool TryResolveUtility(string mod, float v, string fmt, string src, List<Modifier> list)
        {
            if (mod == "Utility_MovementSpeed")
            {
                list.Add(new Modifier(StatType.MovementSpeed, ModifierType.Flat, v, src));
                return true;
            }
            return false;
        }
    }
}
