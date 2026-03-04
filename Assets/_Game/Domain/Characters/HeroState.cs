using System.Collections.Generic;
using Game.Domain.Stats;

namespace Game.Domain.Characters
{
    public sealed class HeroState
    {
        public string Id { get; }
        public StatCollection Stats { get; }

        public HeroState(string id, IReadOnlyDictionary<StatType, float> baseStats = null)
        {
            Id = id;
            Stats = new StatCollection();

            if (baseStats != null)
            {
                foreach (var kvp in baseStats)
                    Stats.SetBase(kvp.Key, kvp.Value);

                if (!baseStats.ContainsKey(StatType.CurrentHealth) && baseStats.ContainsKey(StatType.MaxHealth))
                    Stats.SetBase(StatType.CurrentHealth, baseStats[StatType.MaxHealth]);
            }
            else
            {
                SetDefaults();
            }
        }

        private void SetDefaults()
        {
            Stats.SetBase(StatType.MaxHealth, 100f);
            Stats.SetBase(StatType.CurrentHealth, 100f);
            Stats.SetBase(StatType.PhysicalDamage, 10f);
            Stats.SetBase(StatType.AttackSpeed, 1f);
            Stats.SetBase(StatType.CriticalChance, 0.05f);
            Stats.SetBase(StatType.CriticalMultiplier, 1.5f);
            Stats.SetBase(StatType.Armor, 5f);
            Stats.SetBase(StatType.Evasion, 0f);
            Stats.SetBase(StatType.MovementSpeed, 1f);
            Stats.SetBase(StatType.HealthRegen, 1f);
        }
    }
}
