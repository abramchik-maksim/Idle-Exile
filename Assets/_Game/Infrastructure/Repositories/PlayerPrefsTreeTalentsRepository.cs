using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Progression.TreeTalents;
using UnityEngine;

namespace Game.Infrastructure.Repositories
{
    public sealed class PlayerPrefsTreeTalentsRepository : ITreeTalentsRepository
    {
        private const string Key = "tree_talents_state";

        public void Save(TreeTalentsState state)
        {
            if (state == null) return;

            var data = new TreeTalentsData
            {
                level = state.Level,
                currentXp = state.CurrentXp,
                xpToNextLevel = state.XpToNextLevel,
                inventory = SerializeBranches(state.BranchInventory, false),
                placed = SerializeBranches(state.PlacedBranches, true)
            };

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public TreeTalentsState Load()
        {
            if (!PlayerPrefs.HasKey(Key))
                return new TreeTalentsState();

            var raw = PlayerPrefs.GetString(Key);
            if (string.IsNullOrWhiteSpace(raw))
                return new TreeTalentsState();

            var data = JsonUtility.FromJson<TreeTalentsData>(raw);
            if (data == null)
                return new TreeTalentsState();

            return new TreeTalentsState(
                data.level,
                data.currentXp,
                data.xpToNextLevel,
                DeserializeBranches(data.inventory),
                DeserializeBranches(data.placed));
        }

        private static List<BranchData> SerializeBranches(IReadOnlyList<BranchInstance> branches, bool forcePlaced)
        {
            var result = new List<BranchData>();
            if (branches == null) return result;

            foreach (var branch in branches)
            {
                var branchData = new BranchData
                {
                    id = branch.Id,
                    generationLevel = branch.GenerationLevel,
                    seedTypes = new List<int>(),
                    anchorX = branch.Anchor.X,
                    anchorY = branch.Anchor.Y,
                    rotationQuarterTurns = branch.PlacedRotationQuarterTurns,
                    isPlaced = forcePlaced || branch.IsPlaced,
                    tiles = new List<TileData>()
                };

                foreach (var seed in branch.SeedTypes)
                    branchData.seedTypes.Add((int)seed);

                foreach (var tile in branch.Tiles)
                {
                    branchData.tiles.Add(new TileData
                    {
                        x = tile.Offset.X,
                        y = tile.Offset.Y,
                        nodeId = tile.Node.Id,
                        nodeType = (int)tile.Node.NodeType,
                        allianceType = (int)tile.Node.AllianceType,
                        value = tile.Node.Value
                    });
                }

                result.Add(branchData);
            }

            return result;
        }

        private static List<BranchInstance> DeserializeBranches(List<BranchData> data)
        {
            var result = new List<BranchInstance>();
            if (data == null) return result;

            foreach (var branchData in data)
            {
                var seeds = new List<SeedType>();
                if (branchData.seedTypes != null)
                {
                    foreach (var seed in branchData.seedTypes)
                        seeds.Add((SeedType)seed);
                }

                var tiles = new List<BranchTile>();
                if (branchData.tiles != null)
                {
                    foreach (var tile in branchData.tiles)
                    {
                        var node = new BranchNode(
                            tile.nodeId,
                            (BranchNodeType)tile.nodeType,
                            (NodeAllianceType)tile.allianceType,
                            tile.value);
                        tiles.Add(new BranchTile(new GridCoord(tile.x, tile.y), node));
                    }
                }

                var branch = new BranchInstance(branchData.id, branchData.generationLevel, seeds, tiles);
                if (branchData.isPlaced)
                    branch.PlaceAt(
                        new GridCoord(branchData.anchorX, branchData.anchorY),
                        branchData.rotationQuarterTurns);

                result.Add(branch);
            }

            return result;
        }

        [Serializable]
        private sealed class TreeTalentsData
        {
            public int level;
            public int currentXp;
            public int xpToNextLevel;
            public List<BranchData> inventory;
            public List<BranchData> placed;
        }

        [Serializable]
        private sealed class BranchData
        {
            public string id;
            public int generationLevel;
            public List<int> seedTypes;
            public bool isPlaced;
            public int anchorX;
            public int anchorY;
            public int rotationQuarterTurns;
            public List<TileData> tiles;
        }

        [Serializable]
        private sealed class TileData
        {
            public int x;
            public int y;
            public string nodeId;
            public int nodeType;
            public int allianceType;
            public float value;
        }
    }
}
