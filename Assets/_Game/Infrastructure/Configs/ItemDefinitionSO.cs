using System;
using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.Stats;
using UnityEngine;

namespace Game.Infrastructure.Configs
{
    [CreateAssetMenu(menuName = "Idle Exile/Item Definition", fileName = "NewItemDefinition")]
    public sealed class ItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string itemName;
        public Rarity rarity;
        public EquipmentSlotType slot;

        [Header("Visuals")]
        public string iconAddress;

        [Header("Modifiers")]
        public List<ModifierEntry> implicitModifiers = new();

        public ItemDefinition ToDomain()
        {
            var mods = new Modifier[implicitModifiers.Count];
            for (int i = 0; i < implicitModifiers.Count; i++)
            {
                var e = implicitModifiers[i];
                mods[i] = new Modifier(e.stat, e.type, e.value, "implicit");
            }

            return new ItemDefinition(id, itemName, rarity, slot, mods, iconAddress);
        }
    }

    [Serializable]
    public struct ModifierEntry
    {
        public StatType stat;
        public ModifierType type;
        public float value;
    }
}
