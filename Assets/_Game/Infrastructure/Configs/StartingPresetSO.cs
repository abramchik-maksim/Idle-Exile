using System;
using System.Collections.Generic;
using Game.Domain.Items;
using Game.Infrastructure.Configs.Skills;
using UnityEngine;

namespace Game.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Idle Exile/Starting Preset", fileName = "StartingPreset")]
    public sealed class StartingPresetSO : ScriptableObject
    {
        public List<StartingItem> startingItems = new();
        public List<StartingSkill> startingSkills = new();
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
