using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Loot Table", fileName = "LootTable")]
    public sealed class LootTableSO : ScriptableObject
    {
        [Header("Drop Chance")]
        [Range(0f, 1f)] public float baseDropChance = 0.3f;
        public float dropChancePerBattle = 0.025f;
        [Range(0f, 1f)] public float maxDropChance = 0.65f;

        [Header("Bonus Drop (per tier)")]
        [Range(0f, 1f)] public float bonusDropChancePerTier = 0.1f;

        [Header("Modifier Quality")]
        public float minModValue = 1f;
        public float maxModValue = 10f;

        [Header("Item Pool (leave empty to use full ItemDatabase)")]
        public List<LootPoolEntry> pool = new();
    }

    [Serializable]
    public struct LootPoolEntry
    {
        public ItemDefinitionSO item;
        [Range(0.01f, 100f)] public float weight;
    }
}
