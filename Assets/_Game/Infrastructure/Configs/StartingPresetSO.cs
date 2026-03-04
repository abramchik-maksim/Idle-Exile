using System;
using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.Stats;
using Game.Infrastructure.Configs.Skills;
using UnityEngine;

namespace Game.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Idle Exile/Starting Preset", fileName = "StartingPreset")]
    public sealed class StartingPresetSO : ScriptableObject
    {
        [Header("Hero Base Stats")]
        public List<HeroBaseStat> heroBaseStats = new();

        [Header("Starting Items")]
        public List<StartingItem> startingItems = new();

        [Header("Starting Skills")]
        public List<StartingSkill> startingSkills = new();

        public Dictionary<StatType, float> GetBaseStatsDictionary()
        {
            var dict = new Dictionary<StatType, float>();
            foreach (var entry in heroBaseStats)
                dict[entry.stat] = entry.value;
            return dict;
        }
    }

    [Serializable]
    public struct HeroBaseStat
    {
        public StatType stat;
        public float value;
    }

    [Serializable]
    public struct StartingItem
    {
        public ItemDefinitionSO item;
        public bool autoEquip;
        public EquipmentSlotType equipSlot;
    }

    [Serializable]
    public struct StartingSkill
    {
        public SkillDefinitionSO skill;
        public bool autoEquip;
        [Tooltip("0 = Main slot, 1-4 = Utility slots")]
        public int slotIndex;
    }
}
