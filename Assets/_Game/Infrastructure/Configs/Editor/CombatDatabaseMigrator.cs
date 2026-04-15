using System;
using System.Collections.Generic;
using System.IO;
using Game.Infrastructure.Configs.Combat;
using UnityEditor;
using UnityEngine;

namespace Game.Infrastructure.Configs.Editor
{
    /// <summary>
    /// One-shot migration: deletes the old monolithic CombatDatabase.asset
    /// and rebuilds everything as a pyramid of individual ScriptableObject assets.
    /// Re-uses the same wave composition logic from the original CombatDatabaseCreator.
    /// </summary>
    public static class CombatDatabaseMigrator
    {
        private const string CombatFolder = "Assets/_Game/Infrastructure/Configs/Combat/Data";
        private const string BattlesFolder = "Assets/_Game/Infrastructure/Configs/Combat/Data/Battles";
        private const string MapsFolder = "Assets/_Game/Infrastructure/Configs/Combat/Data/Maps";
        private const string TiersFolder = "Assets/_Game/Infrastructure/Configs/Combat/Data/Tiers";
        private const string DatabasePath = "Assets/_Game/Infrastructure/Configs/Combat/Data/CombatDatabase.asset";

        [MenuItem("Tools/Idle Exile/Migrate CombatDatabase to SO Pyramid", priority = 201)]
        public static void Migrate()
        {
            EnsureFolders();

            var oldDb = AssetDatabase.LoadAssetAtPath<CombatDatabaseSO>(DatabasePath);
            if (oldDb == null)
            {
                Debug.LogWarning("[CombatMigrator] No CombatDatabase.asset found. Running fresh creator instead.");
                CombatDatabaseCreator.CreateAll();
                return;
            }

            var enemies = oldDb.enemies;
            if (enemies == null || enemies.Count == 0)
            {
                Debug.LogError("[CombatMigrator] CombatDatabase has no enemies. Aborting.");
                return;
            }

            var skeleton = enemies.Find(e => e != null && e.id == "skeleton");
            var zombie = enemies.Find(e => e != null && e.id == "zombie");
            var ghost = enemies.Find(e => e != null && e.id == "ghost");

            var battles = CreateBattleAssets(skeleton, zombie, ghost);
            var map = CreateMapAsset("map_1_1", "Twilight Shore", "1", battles);
            var tier = CreateTierAsset("tier_1", "Act I", 1f, true, new List<MapDefinitionSO> { map });

            oldDb.tiers = new List<TierDefinitionSO> { tier };
            EditorUtility.SetDirty(oldDb);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CombatMigrator] Migration complete. " +
                      $"Created {battles.Count} battles, 1 map, 1 tier as separate SO assets.");
        }

        private static void EnsureFolders()
        {
            foreach (var folder in new[] { BattlesFolder, MapsFolder, TiersFolder })
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
            }
        }

        private static List<BattleDefinitionSO> CreateBattleAssets(
            EnemyDefinitionSO skeleton, EnemyDefinitionSO zombie, EnemyDefinitionSO ghost)
        {
            var result = new List<BattleDefinitionSO>();

            for (int b = 0; b < 10; b++)
            {
                string id = $"battle_1_1_{b:D2}";
                string path = $"{BattlesFolder}/Battle_{id}.asset";

                var existing = AssetDatabase.LoadAssetAtPath<BattleDefinitionSO>(path);
                if (existing != null)
                {
                    result.Add(existing);
                    continue;
                }

                var so = ScriptableObject.CreateInstance<BattleDefinitionSO>();
                so.id = id;
                so.waves = BuildWaves(b, skeleton, zombie, ghost);

                AssetDatabase.CreateAsset(so, path);
                result.Add(so);
            }

            return result;
        }

        private static MapDefinitionSO CreateMapAsset(
            string id, string displayName, string locationId,
            List<BattleDefinitionSO> battles)
        {
            string path = $"{MapsFolder}/Map_{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(path);
            if (existing != null)
            {
                existing.battles = battles;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var so = ScriptableObject.CreateInstance<MapDefinitionSO>();
            so.id = id;
            so.displayName = displayName;
            so.locationId = locationId;
            so.battles = battles;
            so.itemWeightMultiplier = 1f;
            so.currencyWeightMultiplier = 1f;

            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static TierDefinitionSO CreateTierAsset(
            string id, string displayName, float scaling, bool hasForcedStartMap,
            List<MapDefinitionSO> maps)
        {
            string path = $"{TiersFolder}/Tier_{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TierDefinitionSO>(path);
            if (existing != null)
            {
                existing.maps = maps;
                existing.hasForcedStartMap = hasForcedStartMap;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var so = ScriptableObject.CreateInstance<TierDefinitionSO>();
            so.id = id;
            so.displayName = displayName;
            so.scaling = scaling;
            so.hasForcedStartMap = hasForcedStartMap;
            so.maps = maps;
            so.mapChoices = new List<MapChoiceDataSO>();

            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static List<WaveDataSO> BuildWaves(
            int battleIndex,
            EnemyDefinitionSO skeleton,
            EnemyDefinitionSO zombie,
            EnemyDefinitionSO ghost)
        {
            var waves = new List<WaveDataSO>();
            int waveCount = Math.Min(2 + battleIndex / 4, 4);

            for (int w = 0; w < waveCount; w++)
            {
                int enemyCount = Math.Min(2 + battleIndex / 3 + w, 8);
                var mainEnemy = battleIndex < 5 ? skeleton : (w == waveCount - 1 ? zombie : skeleton);

                var spawns = new List<WaveSpawnEntrySO>
                {
                    new() { enemy = mainEnemy, count = enemyCount }
                };

                if (battleIndex >= 7 && w >= 1 && ghost != null)
                    spawns.Add(new WaveSpawnEntrySO { enemy = ghost, count = 1 + battleIndex / 8 });

                waves.Add(new WaveDataSO
                {
                    delayBeforeWave = w == 0 ? 1.0f : 2.0f,
                    spawns = spawns
                });
            }

            return waves;
        }
    }
}
