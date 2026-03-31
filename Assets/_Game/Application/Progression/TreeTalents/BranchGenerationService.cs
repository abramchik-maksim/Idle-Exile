using System;
using System.Collections.Generic;
using System.Linq;
using Game.Application.Ports;
using Game.Domain.Progression.TreeTalents;

namespace Game.Application.Progression.TreeTalents
{
    public sealed class BranchGenerationService
    {
        private readonly ITreeTalentsConfigProvider _config;
        private readonly IRandomService _random;

        public BranchGenerationService(ITreeTalentsConfigProvider config, IRandomService random)
        {
            _config = config;
            _random = random;
        }

        public BranchInstance GenerateBranch(IReadOnlyList<SeedType> seeds, int generationLevel)
        {
            var safeSeeds = seeds?.Take(3).ToList() ?? new List<SeedType>();
            if (safeSeeds.Count == 0)
                safeSeeds.Add(SeedType.Universal);

            var shape = PickWeightedShape(_config.GetShapePool());
            var tiles = new List<BranchTile>(shape.Offsets.Count);
            for (var i = 0; i < shape.Offsets.Count; i++)
            {
                var nodeDef = PickWeightedNode(_config.GetNodePool(), safeSeeds, _config.SeedWeightMultiplier);
                var node = new BranchNode(nodeDef.Id, nodeDef.NodeType, nodeDef.AllianceType, nodeDef.Value);
                tiles.Add(new BranchTile(shape.Offsets[i], node));
            }

            return new BranchInstance(Guid.NewGuid().ToString("N"), generationLevel, safeSeeds, tiles);
        }

        private WeightedShapeDefinition PickWeightedShape(IReadOnlyList<WeightedShapeDefinition> pool)
        {
            if (pool == null || pool.Count == 0)
                return new WeightedShapeDefinition
                {
                    Id = "fallback_line_3",
                    Weight = 1f,
                    Offsets = new List<GridCoord> { new(0, 0), new(1, 0), new(2, 0) }
                };

            return WeightedPick(pool, x => Math.Max(0.001f, x.Weight));
        }

        private WeightedNodeDefinition PickWeightedNode(
            IReadOnlyList<WeightedNodeDefinition> pool,
            IReadOnlyList<SeedType> seeds,
            int seedMultiplier)
        {
            if (pool == null || pool.Count == 0)
            {
                return new WeightedNodeDefinition
                {
                    Id = "fallback_fire_small",
                    Weight = 1f,
                    SeedAffinity = SeedType.Fire,
                    NodeType = BranchNodeType.SmallStat,
                    AllianceType = NodeAllianceType.Fire,
                    Value = 5f
                };
            }

            return WeightedPick(pool, def =>
            {
                var weight = Math.Max(0.001f, def.Weight);
                var matches = seeds.Count(seed => seed == def.SeedAffinity);
                if (matches > 0)
                    weight *= (float)Math.Pow(Math.Max(1, seedMultiplier), matches);
                return weight;
            });
        }

        private T WeightedPick<T>(IReadOnlyList<T> items, Func<T, float> weightSelector)
        {
            var total = 0f;
            for (var i = 0; i < items.Count; i++)
                total += Math.Max(0f, weightSelector(items[i]));

            if (total <= 0f)
                return items[0];

            var roll = _random.NextFloat(0f, total);
            var cumulative = 0f;
            for (var i = 0; i < items.Count; i++)
            {
                cumulative += Math.Max(0f, weightSelector(items[i]));
                if (roll <= cumulative)
                    return items[i];
            }

            return items[items.Count - 1];
        }
    }
}
