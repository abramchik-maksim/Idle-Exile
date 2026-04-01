using System.Collections.Generic;
using Game.Domain.Progression.TreeTalents;

namespace Game.Application.Ports
{
    public interface ITreeTalentsConfigProvider
    {
        int GrowthDurationSeconds { get; }
        int SeedWeightMultiplier { get; }
        IReadOnlyList<WeightedShapeDefinition> GetShapePool();
        IReadOnlyList<WeightedNodeDefinition> GetNodePool();
        IReadOnlyList<int> GetUnlockHalfWidthsForLevel(int level);
        IReadOnlyList<int> GetAllianceThresholds();
    }

    public sealed class WeightedShapeDefinition
    {
        public string Id { get; set; }
        public float Weight { get; set; }
        public List<GridCoord> Offsets { get; set; } = new();
    }

    public sealed class WeightedNodeDefinition
    {
        public string Id { get; set; }
        public float Weight { get; set; }
        public SeedType SeedAffinity { get; set; }
        public BranchNodeType NodeType { get; set; }
        public NodeAllianceType AllianceType { get; set; }
        public float Value { get; set; }
    }
}
