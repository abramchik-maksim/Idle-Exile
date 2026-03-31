using System.Collections.Generic;
using System.Linq;
using Game.Application.Ports;
using Game.Domain.Progression.TreeTalents;

namespace Game.Infrastructure.Configs.Progression
{
    public sealed class ScriptableObjectTreeTalentsConfigProvider : ITreeTalentsConfigProvider
    {
        private readonly TreeTalentsDatabaseSO _database;
        private readonly TreeUnlockProfileSO _unlockProfile;
        private readonly List<WeightedShapeDefinition> _shapes;
        private readonly List<WeightedNodeDefinition> _nodes;
        private readonly Dictionary<int, List<int>> _unlockByLevel;

        public int GrowthDurationSeconds => _database != null ? _database.growthDurationSeconds : 20;
        public int SeedWeightMultiplier => _database != null ? _database.seedWeightMultiplier : 5;

        public ScriptableObjectTreeTalentsConfigProvider(TreeTalentsDatabaseSO database, TreeUnlockProfileSO unlockProfile)
        {
            _database = database;
            _unlockProfile = unlockProfile;
            _shapes = BuildShapes();
            _nodes = BuildNodes();
            _unlockByLevel = BuildUnlockProfile();
        }

        public IReadOnlyList<WeightedShapeDefinition> GetShapePool() => _shapes;
        public IReadOnlyList<WeightedNodeDefinition> GetNodePool() => _nodes;

        public IReadOnlyList<int> GetUnlockHalfWidthsForLevel(int level)
        {
            if (_unlockByLevel.TryGetValue(level, out var exact))
                return exact;

            var bestLevel = _unlockByLevel.Keys.Where(l => l <= level).DefaultIfEmpty(2).Max();
            if (_unlockByLevel.TryGetValue(bestLevel, out var fallback))
                return fallback;

            return new[] { 4, 6, 8, 10, 10, 10 };
        }

        private List<WeightedShapeDefinition> BuildShapes()
        {
            if (_database == null || _database.shapes == null || _database.shapes.Count == 0)
            {
                return new List<WeightedShapeDefinition>
                {
                    new()
                    {
                        Id = "default_line_3",
                        Weight = 1f,
                        Offsets = new List<GridCoord> { new(0, 0), new(1, 0), new(2, 0) }
                    }
                };
            }

            return _database.shapes.Select(shape => new WeightedShapeDefinition
            {
                Id = shape.id,
                Weight = shape.weight,
                Offsets = shape.offsets.Select(x => new GridCoord(x.x, x.y)).ToList()
            }).ToList();
        }

        private List<WeightedNodeDefinition> BuildNodes()
        {
            if (_database == null || _database.nodes == null || _database.nodes.Count == 0)
            {
                return new List<WeightedNodeDefinition>
                {
                    new()
                    {
                        Id = "default_fire_small",
                        Weight = 1f,
                        SeedAffinity = SeedType.Fire,
                        NodeType = BranchNodeType.SmallStat,
                        AllianceType = NodeAllianceType.Fire,
                        Value = 5f
                    }
                };
            }

            return _database.nodes.Select(node => new WeightedNodeDefinition
            {
                Id = node.id,
                Weight = node.weight,
                SeedAffinity = node.seedAffinity,
                NodeType = node.nodeType,
                AllianceType = node.allianceType,
                Value = node.value
            }).ToList();
        }

        private Dictionary<int, List<int>> BuildUnlockProfile()
        {
            var map = new Dictionary<int, List<int>>();
            if (_unlockProfile?.levels != null)
            {
                foreach (var level in _unlockProfile.levels)
                {
                    if (level == null || level.level < 1 || level.halfWidthsByRow == null || level.halfWidthsByRow.Count == 0)
                        continue;
                    map[level.level] = new List<int>(level.halfWidthsByRow);
                }
            }

            if (!map.ContainsKey(2))
                map[2] = new List<int> { 4, 6, 8, 10, 10, 10 };

            if (!map.ContainsKey(1))
                map[1] = new List<int> { 2, 3, 4, 4 };

            return map;
        }
    }
}
