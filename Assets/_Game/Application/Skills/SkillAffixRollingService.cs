using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Skills.Crafting;

namespace Game.Application.Skills
{
    public sealed class SkillAffixRollingService
    {
        private readonly ISkillGemConfigProvider _gemConfig;
        private readonly IRandomService _random;

        public SkillAffixRollingService(ISkillGemConfigProvider gemConfig, IRandomService random)
        {
            _gemConfig = gemConfig;
            _random = random;
        }

        public SkillAffix Roll(SkillGemDefinition gem)
        {
            var rarity = RollRarity(gem.RarityWeights);
            var affixDef = RollAffixDefinition(gem, rarity);
            if (affixDef == null) return null;

            int tier = RollTier(gem.MinTier, gem.MaxTier);
            var tierRange = affixDef.GetTierRange(tier);

            float value1 = _random.NextFloat(tierRange.MinValue1, tierRange.MaxValue1);
            float value2 = tierRange.MaxValue2 > 0f
                ? _random.NextFloat(tierRange.MinValue2, tierRange.MaxValue2)
                : 0f;

            return new SkillAffix(affixDef, rarity, tier, value1, value2);
        }

        private Rarity RollRarity(IReadOnlyDictionary<Rarity, float> weights)
        {
            float totalWeight = 0f;
            foreach (var kvp in weights)
                totalWeight += kvp.Value;

            float roll = _random.NextFloat(0f, totalWeight);
            float cumulative = 0f;

            foreach (var kvp in weights)
            {
                cumulative += kvp.Value;
                if (roll <= cumulative)
                    return kvp.Key;
            }

            foreach (var kvp in weights)
                return kvp.Key;

            return Rarity.Normal;
        }

        private SkillAffixDefinition RollAffixDefinition(SkillGemDefinition gem, Rarity rarity)
        {
            var candidates = new List<SkillAffixDefinition>();

            foreach (var affixId in gem.AffixPoolIds)
            {
                var affixDef = _gemConfig.GetAffixDefinition(affixId);
                if (affixDef == null) continue;
                if (affixDef.MinimumRarity <= rarity)
                    candidates.Add(affixDef);
            }

            if (candidates.Count == 0) return null;

            return candidates[_random.Next(0, candidates.Count)];
        }

        private int RollTier(int minTier, int maxTier)
        {
            return _random.Next(minTier, maxTier + 1);
        }
    }
}
