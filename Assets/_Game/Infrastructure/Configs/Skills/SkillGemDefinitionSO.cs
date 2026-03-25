using System;
using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.Skills.Crafting;
using UnityEngine;

namespace Game.Infrastructure.Configs.Skills
{
    [CreateAssetMenu(menuName = "Idle Exile/Skills/Skill Gem Definition", fileName = "NewSkillGem")]
    public sealed class SkillGemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string gemName;
        public SkillGemElement element;
        public SkillGemLevel level;

        [Header("Visuals")]
        public string iconAddress;

        [Header("Tier Range")]
        [Tooltip("Lower number = better tier (T1 best, T9 worst)")]
        [Range(1, 9)] public int minTier = 7;
        [Range(1, 9)] public int maxTier = 9;

        [Header("Rarity Weights")]
        public List<RarityWeightEntry> rarityWeights = new();

        [Header("Affix Pool")]
        public List<SkillAffixDefinitionSO> affixPool = new();

        public SkillGemDefinition ToDomain()
        {
            var weights = new Dictionary<Rarity, float>();
            foreach (var entry in rarityWeights)
                weights[entry.rarity] = entry.weight;

            var affixIds = new List<string>(affixPool.Count);
            foreach (var affix in affixPool)
            {
                if (affix != null)
                    affixIds.Add(affix.id);
            }

            return new SkillGemDefinition(
                id, gemName, element, level, iconAddress,
                minTier, maxTier, weights, affixIds.AsReadOnly());
        }
    }

    [Serializable]
    public struct RarityWeightEntry
    {
        public Rarity rarity;
        [Range(0f, 1f)] public float weight;
    }
}
