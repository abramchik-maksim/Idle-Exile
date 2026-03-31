using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Domain.Progression.TreeTalents;

namespace Game.Infrastructure.Configs.Progression
{
    [CreateAssetMenu(menuName = "Idle Exile/Progression/Tree Talents Database", fileName = "TreeTalentsDatabase")]
    public sealed class TreeTalentsDatabaseSO : ScriptableObject
    {
        [Min(1)] public int growthDurationSeconds = 20;
        [Min(1)] public int seedWeightMultiplier = 5;
        public List<ShapeEntry> shapes = new();
        public List<NodeEntry> nodes = new();

        [Serializable]
        public sealed class ShapeEntry
        {
            public string id = "shape";
            public float weight = 1f;
            public List<Vector2Int> offsets = new();
        }

        [Serializable]
        public sealed class NodeEntry
        {
            public string id = "node";
            public float weight = 1f;
            public SeedType seedAffinity = SeedType.Universal;
            public BranchNodeType nodeType = BranchNodeType.SmallStat;
            public NodeAllianceType allianceType = NodeAllianceType.None;
            public float value = 1f;
        }
    }
}
