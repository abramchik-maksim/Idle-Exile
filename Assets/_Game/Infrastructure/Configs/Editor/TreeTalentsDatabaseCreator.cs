using System.Collections.Generic;
using System.IO;
using Game.Domain.Progression.TreeTalents;
using Game.Infrastructure.Configs.Progression;
using UnityEditor;
using UnityEngine;

namespace Game.Infrastructure.Configs.Editor
{
    public static class TreeTalentsDatabaseCreator
    {
        private const string DataFolder = "Assets/_Game/Infrastructure/Configs/Progression/Data";
        private const string DatabasePath = "Assets/_Game/Infrastructure/Configs/Progression/Data/TreeTalentsDatabase.asset";
        private const string UnlockProfilePath = "Assets/_Game/Infrastructure/Configs/Progression/Data/TreeUnlockProfile.asset";

        [MenuItem("Idle Exile/Create Tree Talents Database & Unlock Profile", priority = 210)]
        public static void CreateAll()
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            var database = EnsureDatabase();
            var unlockProfile = EnsureUnlockProfile();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TreeTalentsDatabaseCreator] Created/updated database at {DatabasePath} and unlock profile at {UnlockProfilePath}. " +
                      $"Shapes: {database.shapes.Count}, Nodes: {database.nodes.Count}, Unlock levels: {unlockProfile.levels.Count}");
        }

        private static TreeTalentsDatabaseSO EnsureDatabase()
        {
            var db = AssetDatabase.LoadAssetAtPath<TreeTalentsDatabaseSO>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<TreeTalentsDatabaseSO>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            db.growthDurationSeconds = 20;
            db.seedWeightMultiplier = 5;
            db.shapes = BuildShapeEntries();
            db.nodes = BuildNodeEntries();

            EditorUtility.SetDirty(db);
            return db;
        }

        private static TreeUnlockProfileSO EnsureUnlockProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<TreeUnlockProfileSO>(UnlockProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<TreeUnlockProfileSO>();
                AssetDatabase.CreateAsset(profile, UnlockProfilePath);
            }

            profile.levels = BuildUnlockLevels();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static List<TreeTalentsDatabaseSO.ShapeEntry> BuildShapeEntries()
        {
            return new List<TreeTalentsDatabaseSO.ShapeEntry>
            {
                new()
                {
                    id = "line_3",
                    weight = 4.0f,
                    offsets = new List<Vector2Int>
                    {
                        new(0, 0),
                        new(1, 0),
                        new(2, 0),
                    }
                },
                new()
                {
                    id = "line_4",
                    weight = 2.2f,
                    offsets = new List<Vector2Int>
                    {
                        new(0, 0),
                        new(1, 0),
                        new(2, 0),
                        new(3, 0),
                    }
                },
                new()
                {
                    id = "l_4",
                    weight = 1.8f,
                    offsets = new List<Vector2Int>
                    {
                        new(0, 0),
                        new(1, 0),
                        new(2, 0),
                        new(2, 1),
                    }
                },
                new()
                {
                    id = "z_4",
                    weight = 1.0f,
                    offsets = new List<Vector2Int>
                    {
                        new(0, 0),
                        new(1, 0),
                        new(1, 1),
                        new(2, 1),
                    }
                }
            };
        }

        private static List<TreeTalentsDatabaseSO.NodeEntry> BuildNodeEntries()
        {
            var entries = new List<TreeTalentsDatabaseSO.NodeEntry>
            {
                // Small stat nodes (main pool)
                BuildNode("fire_small", 3.2f, SeedType.Fire, BranchNodeType.SmallStat, NodeAllianceType.Fire, 5f),
                BuildNode("speed_small", 3.2f, SeedType.Speed, BranchNodeType.SmallStat, NodeAllianceType.Speed, 5f),
                BuildNode("defense_small", 3.2f, SeedType.Defense, BranchNodeType.SmallStat, NodeAllianceType.Defense, 5f),
                BuildNode("crit_small", 2.7f, SeedType.Crit, BranchNodeType.SmallStat, NodeAllianceType.Crit, 4f),
                BuildNode("bleed_small", 2.4f, SeedType.Bleed, BranchNodeType.SmallStat, NodeAllianceType.Bleed, 4f),
                BuildNode("utility_small", 2.0f, SeedType.Utility, BranchNodeType.SmallStat, NodeAllianceType.Utility, 3f),
                BuildNode("growth_small", 1.8f, SeedType.Growth, BranchNodeType.SmallStat, NodeAllianceType.Growth, 3f),
                BuildNode("universal_small", 1.2f, SeedType.Universal, BranchNodeType.SmallStat, NodeAllianceType.Universal, 2f),

                // Special nodes (rare pool)
                BuildNode("socket_node", 0.35f, SeedType.Growth, BranchNodeType.Socket, NodeAllianceType.None, 0f),
                BuildNode("hybrid_node", 0.20f, SeedType.Universal, BranchNodeType.Hybrid, NodeAllianceType.Universal, 2f),
                BuildNode("growth_modifier_node", 0.15f, SeedType.Growth, BranchNodeType.GrowthModifier, NodeAllianceType.Growth, 1.5f),
                BuildNode("alliance_amplifier_node", 0.10f, SeedType.Universal, BranchNodeType.AllianceAmplifier, NodeAllianceType.Universal, 1f),
            };

            return entries;
        }

        private static TreeTalentsDatabaseSO.NodeEntry BuildNode(
            string id,
            float weight,
            SeedType affinity,
            BranchNodeType type,
            NodeAllianceType alliance,
            float value)
        {
            return new TreeTalentsDatabaseSO.NodeEntry
            {
                id = id,
                weight = weight,
                seedAffinity = affinity,
                nodeType = type,
                allianceType = alliance,
                value = value
            };
        }

        private static List<TreeUnlockProfileSO.LevelUnlockEntry> BuildUnlockLevels()
        {
            return new List<TreeUnlockProfileSO.LevelUnlockEntry>
            {
                new()
                {
                    level = 1,
                    halfWidthsByRow = new List<int> { 2, 3, 4, 4 }
                },
                // GD-prescribed starting preset when tree opens at level 2.
                new()
                {
                    level = 2,
                    halfWidthsByRow = new List<int> { 4, 6, 8, 10, 10, 10 }
                },
                new()
                {
                    level = 3,
                    halfWidthsByRow = new List<int> { 4, 6, 8, 10, 11, 11, 10 }
                },
                new()
                {
                    level = 4,
                    halfWidthsByRow = new List<int> { 5, 7, 9, 11, 12, 12, 11, 9 }
                }
            };
        }
    }
}
