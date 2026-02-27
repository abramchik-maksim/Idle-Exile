using System;
using System.Collections.Generic;
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
        public List<MapDataSO> maps = new();
    }

    [Serializable]
    public sealed class MapDataSO
    {
        public string id;
        public string displayName;
        public List<BattleDataSO> battles = new();
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
