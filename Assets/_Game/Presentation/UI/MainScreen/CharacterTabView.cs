using System.Collections.Generic;
using UnityEngine.UIElements;
using Game.Domain.Stats;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class CharacterTabView : LayoutView
    {
        private VisualElement _statsCombat;
        private VisualElement _statsElemental;
        private VisualElement _statsDefense;
        private VisualElement _statsResistances;
        private VisualElement _statsAilments;
        private VisualElement _statsUtility;

        private static readonly StatType[] CombatStats =
        {
            StatType.PhysicalDamage,
            StatType.AttackSpeed,
            StatType.CriticalChance,
            StatType.CriticalMultiplier,
            StatType.GlobalDamage,
            StatType.DoubleHitChance,
            StatType.IgnoreArmorChance,
            StatType.LifeLeech,
        };

        private static readonly StatType[] ElementalStats =
        {
            StatType.FireDamage,
            StatType.ColdDamage,
            StatType.LightningDamage,
            StatType.CorrosionDamage,
            StatType.GainAsFirePercent,
            StatType.GainAsColdPercent,
            StatType.GainAsLightningPercent,
        };

        private static readonly StatType[] DefenseStats =
        {
            StatType.MaxHealth,
            StatType.Armor,
            StatType.Evasion,
            StatType.HealthRegen,
            StatType.BlockChance,
            StatType.Barrier,
        };

        private static readonly StatType[] ResistanceStats =
        {
            StatType.FireResistance,
            StatType.ColdResistance,
            StatType.LightningResistance,
            StatType.CorrosionResistance,
        };

        private static readonly StatType[] AilmentStats =
        {
            StatType.IgniteChance,
            StatType.BleedChance,
            StatType.ChillChance,
            StatType.ShockChance,
            StatType.PoisonChance,
            StatType.AilmentEffectAll,
            StatType.AilmentDuration,
        };

        private static readonly StatType[] UtilityStats =
        {
            StatType.MovementSpeed,
            StatType.BuffDuration,
            StatType.BuffEffect,
            StatType.CooldownRecoveryRate,
        };

        protected override void OnBind()
        {
            _statsCombat = Q("stats-combat");
            _statsElemental = Q("stats-elemental");
            _statsDefense = Q("stats-defense");
            _statsResistances = Q("stats-resistances");
            _statsAilments = Q("stats-ailments");
            _statsUtility = Q("stats-utility");
        }

        public void RenderStats(IReadOnlyDictionary<StatType, float> stats)
        {
            RenderGroup(_statsCombat, CombatStats, stats);
            RenderGroup(_statsElemental, ElementalStats, stats);
            RenderGroup(_statsDefense, DefenseStats, stats);
            RenderGroup(_statsResistances, ResistanceStats, stats);
            RenderGroup(_statsAilments, AilmentStats, stats);
            RenderGroup(_statsUtility, UtilityStats, stats);
        }

        private static void RenderGroup(VisualElement container, StatType[] types,
            IReadOnlyDictionary<StatType, float> stats)
        {
            if (container == null) return;
            container.Clear();

            foreach (var stat in types)
            {
                if (!stats.TryGetValue(stat, out var value))
                    continue;

                if (IsHiddenWhenZero(stat) && value == 0f)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("stat-row");

                var nameLabel = new Label(FormatStatName(stat));
                nameLabel.AddToClassList("stat-label");

                var valueLabel = new Label(FormatStatValue(stat, value));
                valueLabel.AddToClassList("stat-value");

                row.Add(nameLabel);
                row.Add(valueLabel);
                container.Add(row);
            }
        }

        private static bool IsHiddenWhenZero(StatType stat) => stat switch
        {
            StatType.MaxHealth => false,
            StatType.Armor => false,
            StatType.PhysicalDamage => false,
            StatType.AttackSpeed => false,
            StatType.CriticalChance => false,
            StatType.CriticalMultiplier => false,
            StatType.MovementSpeed => false,
            StatType.HealthRegen => false,
            StatType.Evasion => false,
            StatType.FireResistance => false,
            StatType.ColdResistance => false,
            StatType.LightningResistance => false,
            StatType.CorrosionResistance => false,
            _ => true
        };

        public static string FormatStatName(StatType stat) => stat switch
        {
            StatType.MaxHealth => "Max Health",
            StatType.CurrentHealth => "Current Health",
            StatType.PhysicalDamage => "Physical Damage",
            StatType.AttackSpeed => "Attack Speed",
            StatType.CriticalChance => "Critical Chance",
            StatType.CriticalMultiplier => "Critical Multiplier",
            StatType.Armor => "Armor",
            StatType.Evasion => "Evasion",
            StatType.MovementSpeed => "Movement Speed",
            StatType.HealthRegen => "Health Regen",
            StatType.FireDamage => "Fire Damage",
            StatType.ColdDamage => "Cold Damage",
            StatType.LightningDamage => "Lightning Damage",
            StatType.CorrosionDamage => "Corrosion Damage",
            StatType.GlobalDamage => "Global Damage",
            StatType.FireResistance => "Fire Resistance",
            StatType.ColdResistance => "Cold Resistance",
            StatType.LightningResistance => "Lightning Resistance",
            StatType.CorrosionResistance => "Corrosion Resistance",
            StatType.Barrier => "Barrier",
            StatType.BlockChance => "Block Chance",
            StatType.LifeLeech => "Life Leech",
            StatType.LifeLeechRate => "Leech Rate",
            StatType.IgniteChance => "Ignite Chance",
            StatType.ChillChance => "Chill Chance",
            StatType.ShockChance => "Shock Chance",
            StatType.BleedChance => "Bleed Chance",
            StatType.PoisonChance => "Poison Chance",
            StatType.AilmentChanceAll => "Ailment Chance",
            StatType.AilmentEffectAll => "Ailment Effect",
            StatType.AilmentDuration => "Ailment Duration",
            StatType.IgniteEffect => "Ignite Effect",
            StatType.ChillEffect => "Chill Effect",
            StatType.ShockEffect => "Shock Effect",
            StatType.BleedEffect => "Bleed Effect",
            StatType.PoisonEffect => "Poison Effect",
            StatType.DoubleHitChance => "Double Hit Chance",
            StatType.IgnoreArmorChance => "Ignore Armor Chance",
            StatType.GainAsFirePercent => "Gain as Fire",
            StatType.GainAsColdPercent => "Gain as Cold",
            StatType.GainAsLightningPercent => "Gain as Lightning",
            StatType.FirePenetration => "Fire Penetration",
            StatType.ColdPenetration => "Cold Penetration",
            StatType.LightningPenetration => "Lightning Penetration",
            StatType.CorrosionPenetration => "Corrosion Penetration",
            StatType.MeleeIncreasedDamage => "Melee Damage",
            StatType.RangedIncreasedDamage => "Ranged Damage",
            StatType.SpellIncreasedDamage => "Spell Damage",
            StatType.BuffDuration => "Buff Duration",
            StatType.BuffEffect => "Buff Effect",
            StatType.CooldownRecoveryRate => "Cooldown Recovery",
            _ => stat.ToString()
        };

        public static string FormatStatValue(StatType stat, float val) => stat switch
        {
            StatType.CriticalChance => $"{val * 100f:F1}%",
            StatType.CriticalMultiplier => $"x{val:F2}",
            StatType.AttackSpeed => $"{val:F2}/s",
            StatType.HealthRegen => $"{val:F1}/s",

            StatType.BlockChance or
            StatType.LifeLeech or
            StatType.IgniteChance or
            StatType.ChillChance or
            StatType.ShockChance or
            StatType.BleedChance or
            StatType.PoisonChance or
            StatType.AilmentChanceAll or
            StatType.DoubleHitChance or
            StatType.IgnoreArmorChance or
            StatType.GainAsFirePercent or
            StatType.GainAsColdPercent or
            StatType.GainAsLightningPercent or
            StatType.FirePenetration or
            StatType.ColdPenetration or
            StatType.LightningPenetration or
            StatType.CorrosionPenetration or
            StatType.FireResistance or
            StatType.ColdResistance or
            StatType.LightningResistance or
            StatType.CorrosionResistance => $"{val * 100f:F1}%",

            StatType.GlobalDamage or
            StatType.AilmentEffectAll or
            StatType.IgniteEffect or
            StatType.ChillEffect or
            StatType.ShockEffect or
            StatType.BleedEffect or
            StatType.PoisonEffect or
            StatType.AilmentDuration or
            StatType.MeleeIncreasedDamage or
            StatType.RangedIncreasedDamage or
            StatType.SpellIncreasedDamage or
            StatType.BuffDuration or
            StatType.BuffEffect or
            StatType.CooldownRecoveryRate => FormatIncreased(val),

            _ => $"{val:F0}"
        };

        private static string FormatIncreased(float val)
        {
            float pct = val * 100f;
            if (pct == 0f) return "0%";
            return pct > 0f ? $"+{pct:F0}%" : $"{pct:F0}%";
        }
    }
}
