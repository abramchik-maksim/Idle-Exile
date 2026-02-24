using System.Collections.Generic;
using UnityEngine.UIElements;
using Game.Domain.Stats;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class CharacterTabView : LayoutView
    {
        private VisualElement _statsCombat;
        private VisualElement _statsDefense;
        private VisualElement _statsUtility;

        private static readonly StatType[] CombatStats =
        {
            StatType.PhysicalDamage,
            StatType.AttackSpeed,
            StatType.CriticalChance,
            StatType.CriticalMultiplier
        };

        private static readonly StatType[] DefenseStats =
        {
            StatType.MaxHealth,
            StatType.Armor,
            StatType.Evasion,
            StatType.HealthRegen
        };

        private static readonly StatType[] UtilityStats =
        {
            StatType.MovementSpeed
        };

        protected override void OnBind()
        {
            _statsCombat = Q("stats-combat");
            _statsDefense = Q("stats-defense");
            _statsUtility = Q("stats-utility");
        }

        public void RenderStats(IReadOnlyDictionary<StatType, float> stats)
        {
            RenderGroup(_statsCombat, CombatStats, stats);
            RenderGroup(_statsDefense, DefenseStats, stats);
            RenderGroup(_statsUtility, UtilityStats, stats);
        }

        private static void RenderGroup(VisualElement container, StatType[] types,
            IReadOnlyDictionary<StatType, float> stats)
        {
            container.Clear();

            foreach (var stat in types)
            {
                if (!stats.TryGetValue(stat, out var value))
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

        private static string FormatStatName(StatType stat) => stat switch
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
            _ => stat.ToString()
        };

        private static string FormatStatValue(StatType stat, float val) => stat switch
        {
            StatType.CriticalChance => $"{val * 100f:F1}%",
            StatType.CriticalMultiplier => $"x{val:F2}",
            StatType.AttackSpeed => $"{val:F2}/s",
            StatType.HealthRegen => $"{val:F1}/s",
            _ => $"{val:F0}"
        };
    }
}
