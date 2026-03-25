using System.Collections.Generic;
using Game.Domain.Combat;
using Game.Domain.Items;

namespace Game.Domain.Skills.Crafting
{
    public sealed class SkillAffixDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public SkillAffixType Type { get; }
        public DamageType DamageType { get; }
        public AilmentType AilmentType { get; }
        public Rarity MinimumRarity { get; }
        public IReadOnlyList<TierValueRange> TierRanges { get; }
        public string DescriptionTemplate { get; }

        public SkillAffixDefinition(
            string id,
            string name,
            SkillAffixType type,
            DamageType damageType,
            AilmentType ailmentType,
            Rarity minimumRarity,
            IReadOnlyList<TierValueRange> tierRanges,
            string descriptionTemplate)
        {
            Id = id;
            Name = name;
            Type = type;
            DamageType = damageType;
            AilmentType = ailmentType;
            MinimumRarity = minimumRarity;
            TierRanges = tierRanges;
            DescriptionTemplate = descriptionTemplate;
        }

        public TierValueRange GetTierRange(int tier)
        {
            foreach (var range in TierRanges)
            {
                if (range.Tier == tier)
                    return range;
            }

            return default;
        }
    }
}
