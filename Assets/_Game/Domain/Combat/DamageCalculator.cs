using System;
using Game.Domain.Stats;

namespace Game.Domain.Combat
{
    public static class DamageCalculator
    {
        public static DamageResult Calculate(
            StatCollection attacker,
            StatCollection defender,
            DamageType damageType,
            Func<double> nextRandom)
        {
            float baseDamage = attacker.GetFinal(StatType.PhysicalDamage);
            float critChance = Math.Clamp(attacker.GetFinal(StatType.CriticalChance), 0f, 1f);
            float critMulti = attacker.GetFinal(StatType.CriticalMultiplier);

            bool isCrit = nextRandom() < critChance;
            float raw = baseDamage * (isCrit ? critMulti : 1f);

            float armor = defender.GetFinal(StatType.Armor);
            float reduction = armor / (armor + 10f * raw);
            float mitigated = raw * (1f - reduction);

            return new DamageResult(raw, Math.Max(mitigated, 0f), isCrit, damageType);
        }
    }
}
