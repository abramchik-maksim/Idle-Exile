using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat/Battle Definition", fileName = "NewBattle")]
    public sealed class BattleDefinitionSO : ScriptableObject
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
