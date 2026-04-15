using System;
using System.Collections.Generic;
using Game.Domain.Combat.Progression;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat Database", fileName = "CombatDatabase")]
    public sealed class CombatDatabaseSO : ScriptableObject
    {
        public List<EnemyDefinitionSO> enemies = new();
        public List<TierDataSO> tiers = new();
    }

    [Serializable]
    public sealed class TierDataSO
    {
        public string id;
        public string displayName;
        public float scaling = 1f;

        [Header("Maps")]
        [Tooltip("The maps that belong to this tier (linear sequence)")]
        public List<MapDataSO> maps = new();

        [Tooltip("If true, the first map in the list is forced (no player choice)")]
        public bool hasForcedStartMap;

        [Tooltip("Map choice points: each entry offers 2 map options. Maps chosen are appended to the tier's active map sequence.")]
        public List<MapChoiceDataSO> mapChoices = new();
    }

    [Serializable]
    public sealed class MapChoiceDataSO
    {
        public MapDataSO option1;
        public MapDataSO option2;
    }

    [Serializable]
    public sealed class MapDataSO
    {
        public string id;
        public string displayName;
        [TextArea(1, 3)]
        public string description;
        [Tooltip("Addressable key for the location prefab to load for this map")]
        public string locationId;
        public List<BattleDataSO> battles = new();

        [Header("Loot Bias")]
        public float itemWeightMultiplier = 1f;
        public float currencyWeightMultiplier = 1f;

        [Header("Map Modifiers")]
        public List<MapModifierDataSO> modifiers = new();

        [Tooltip("If true, the last battle on this map is a boss encounter")]
        public bool isBossMap;
    }

    [Serializable]
    public struct MapModifierDataSO
    {
        public MapModifierType type;
        public float value;
    }

    [Serializable]
    public sealed class BattleDataSO
    {
        public string id;
        public List<WaveDataSO> waves = new();
    }

    [Serializable]
    public sealed class WaveDataSO
    {
        public float delayBeforeWave = 1f;
        public List<WaveSpawnEntrySO> spawns = new();
    }

    [Serializable]
    public struct WaveSpawnEntrySO
    {
        public EnemyDefinitionSO enemy;
        public int count;
    }
}
