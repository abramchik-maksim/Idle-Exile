using System.Collections.Generic;
using Game.Domain.Items;

namespace Game.Domain.Skills.Crafting
{
    public sealed class SkillGemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public SkillGemElement Element { get; }
        public SkillGemLevel Level { get; }
        public string IconAddress { get; }
        public int MinTier { get; }
        public int MaxTier { get; }
        public IReadOnlyDictionary<Rarity, float> RarityWeights { get; }
        public IReadOnlyList<string> AffixPoolIds { get; }

        public SkillGemDefinition(
            string id,
            string name,
            SkillGemElement element,
            SkillGemLevel level,
            string iconAddress,
            int minTier,
            int maxTier,
            IReadOnlyDictionary<Rarity, float> rarityWeights,
            IReadOnlyList<string> affixPoolIds)
        {
            Id = id;
            Name = name;
            Element = element;
            Level = level;
            IconAddress = iconAddress;
            MinTier = minTier;
            MaxTier = maxTier;
            RarityWeights = rarityWeights;
            AffixPoolIds = affixPoolIds;
        }
    }
}
