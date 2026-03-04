using System;
using Game.Domain.Stats;

namespace Game.Domain.Combat
{
    public static class DamageCalculator
    {
        public const float ArmorDivisor = 10f;

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

            float mitigated = ApplyArmorReduction(raw, defender.GetFinal(StatType.Armor));

            return new DamageResult(raw, Math.Max(mitigated, 0f), isCrit, damageType);
        }

        public static float ApplyArmorReduction(float rawDamage, float armor)
        {
            if (rawDamage <= 0f) return 0f;
            float reduction = armor / (armor + ArmorDivisor * rawDamage);
            return rawDamage * (1f - reduction);
        }
    }
}
