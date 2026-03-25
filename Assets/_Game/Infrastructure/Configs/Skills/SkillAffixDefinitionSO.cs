using System;
using System.Collections.Generic;
using Game.Domain.Combat;
using Game.Domain.Items;
using Game.Domain.Skills.Crafting;
using UnityEngine;

namespace Game.Infrastructure.Configs.Skills
{
    [CreateAssetMenu(menuName = "Idle Exile/Skills/Skill Affix Definition", fileName = "NewSkillAffix")]
    public sealed class SkillAffixDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string affixName;

        [Header("Mechanic")]
        public SkillAffixType type;
        public DamageType damageType;
        public AilmentType ailmentType;
        public Rarity minimumRarity;

        [Header("Description")]
        [Tooltip("Use {0} and {1} for value placeholders")]
        public string descriptionTemplate;

        [Header("Tier Values (T1 = best, T9 = worst)")]
        public List<TierValueRangeEntry> tierRanges = new();

        public SkillAffixDefinition ToDomain()
        {
            var ranges = new List<TierValueRange>(tierRanges.Count);
            foreach (var entry in tierRanges)
            {
                ranges.Add(new TierValueRange(
                    entry.tier, entry.minValue1, entry.maxValue1,
                    entry.minValue2, entry.maxValue2));
            }

            return new SkillAffixDefinition(
                id, affixName, type, damageType, ailmentType,
                minimumRarity, ranges.AsReadOnly(), descriptionTemplate);
        }
    }

    [Serializable]
    public struct TierValueRangeEntry
    {
        public int tier;
        public float minValue1;
        public float maxValue1;
        public float minValue2;
        public float maxValue2;
    }
}
