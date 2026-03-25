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

        /// <summary>
        /// Multi-type damage calculation with elemental support and gain-as conversions.
        /// 1. Physical calculated with physical-specific modifiers only
        /// 2. Gain-as conversions inject extra flat into elemental types (double-dip: benefits from element increases)
        /// 3. Each elemental type calculated with element-specific modifiers
        /// 4. All types summed
        /// 5. GlobalDamage modifiers applied once to total (no double-dip)
        /// 6. Crit applied uniformly
        /// </summary>
        public static DamageBreakdown CalculateMultiType(
            StatCollection attacker,
            StatCollection defender,
            GainAsElementData gainAs,
            Func<double> nextRandom)
        {
            float physDmg = attacker.GetFinal(StatType.PhysicalDamage);

            float extraFireFlat = physDmg * gainAs.GainAsFirePercent;
            float extraColdFlat = physDmg * gainAs.GainAsColdPercent;
            float extraLtngFlat = physDmg * gainAs.GainAsLightningPercent;

            float fireDmg = attacker.GetFinalWithExtraFlat(StatType.FireDamage, extraFireFlat);
            float coldDmg = attacker.GetFinalWithExtraFlat(StatType.ColdDamage, extraColdFlat);
            float ltngDmg = attacker.GetFinalWithExtraFlat(StatType.LightningDamage, extraLtngFlat);

            float rawTotal = physDmg + fireDmg + coldDmg + ltngDmg;

            float globalIncreased = 0f;
            float globalMore = 1f;
            foreach (var mod in attacker.Modifiers)
            {
                if (mod.Stat != StatType.GlobalDamage) continue;
                switch (mod.Type)
                {
                    case ModifierType.Increased:
                        globalIncreased += mod.Value;
                        break;
                    case ModifierType.More:
                        globalMore *= 1f + mod.Value;
                        break;
                }
            }

            rawTotal = rawTotal * (1f + globalIncreased) * globalMore;

            float critChance = Math.Clamp(attacker.GetFinal(StatType.CriticalChance), 0f, 1f);
            float critMulti = attacker.GetFinal(StatType.CriticalMultiplier);
            bool isCrit = nextRandom() < critChance;

            if (isCrit)
                rawTotal *= critMulti;

            float mitigated = ApplyArmorReduction(rawTotal, defender.GetFinal(StatType.Armor));

            return new DamageBreakdown(
                physDmg, fireDmg, coldDmg, ltngDmg,
                Math.Max(mitigated, 0f),
                isCrit);
        }

        public static float ApplyArmorReduction(float rawDamage, float armor)
        {
            if (rawDamage <= 0f) return 0f;
            float reduction = armor / (armor + ArmorDivisor * rawDamage);
            return rawDamage * (1f - reduction);
        }
    }
}
