using System;
using System.Collections.Generic;
using Game.Domain.Items;
using UnityEngine;

namespace Game.Infrastructure.ItemAffixes
{
    [CreateAssetMenu(menuName = "Idle Exile/Item Affix Database", fileName = "ItemAffixDatabase")]
    public sealed class ItemAffixDatabaseSO : ScriptableObject
    {
        [Tooltip("Imported from ResolvedItemAffixPool.csv")]
        public List<AffixPoolSerializedRow> poolRows = new();

        [Tooltip("Imported from AffixAllowedSlots.csv")]
        public List<AffixSlotSerializedRow> slotRows = new();

        [Tooltip("Imported from ModCatalog.csv (UI text templates).")]
        public List<ModCatalogSerializedRow> modCatalogRows = new();

        public bool HasSlotRules => slotRows != null && slotRows.Count > 0;
    }

    [Serializable]
    public sealed class AffixPoolSerializedRow
    {
        public string affixId;
        public string modId;
        public string itemSlots;
        public string classSpecific;
        public int tier;
        public int weight;
        public float min;
        public float max;
        public string valueFormat;
        public string templateId;
        public string progressBand;
    }

    [Serializable]
    public sealed class AffixSlotSerializedRow
    {
        public string modId;
        public string slotId;
        public float weightMultiplier = 1f;
        public bool enabled = true;
        public string notes;
    }

    [Serializable]
    public sealed class ModCatalogSerializedRow
    {
        public string modId;
        public string valueType;
        public string textTemplate;
    }
}
