using System;
using System.Collections.Generic;
using System.IO;
using Game.Infrastructure.Configs.Combat;
using UnityEditor;
using UnityEngine;

namespace Game.Infrastructure.Configs.Editor
{
    public static class CombatDatabaseCreator
    {
        private const string CombatFolder = "Assets/_Game/Infrastructure/Configs/Combat/Data";
        private const string DatabasePath = "Assets/_Game/Infrastructure/Configs/Combat/Data/CombatDatabase.asset";
        private const string LootTablePath = "Assets/_Game/Infrastructure/Configs/Combat/Data/LootTable.asset";

        [MenuItem("Idle Exile/Create Combat Database", priority = 200)]
        public static void CreateAll()
        {
            if (!Directory.Exists(CombatFolder))
                Directory.CreateDirectory(CombatFolder);

            var enemies = CreateEnemies();
            var database = CreateDatabase(enemies);
            CreateLootTable();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CombatDatabaseCreator] Created combat database with {enemies.Count} enemies, " +
                      $"{database.tiers.Count} tiers at {DatabasePath}");
        }

        private static List<EnemyDefinitionSO> CreateEnemies()
        {
            var blueprints = new[]
            {
                ("skeleton", "Skeleton", 30f, 5f, 2f, 2f),
                ("zombie", "Zombie", 50f, 8f, 4f, 1.2f),
                ("ghost", "Ghost", 20f, 10f, 0f, 3f),
            };

            var result = new List<EnemyDefinitionSO>();

            foreach (var (id, name, hp, dmg, armor, speed) in blueprints)
            {
                var path = $"{CombatFolder}/Enemy_{id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinitionSO>(path);

                if (existing != null)
                {
                    result.Add(existing);
                    continue;
                }

                var so = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
                so.id = id;
                so.displayName = name;
                so.baseHealth = hp;
                so.baseDamage = dmg;
                so.baseArmor = armor;
                so.baseSpeed = speed;

                AssetDatabase.CreateAsset(so, path);
                result.Add(so);
            }

            return result;
        }

        private static CombatDatabaseSO CreateDatabase(List<EnemyDefinitionSO> enemies)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CombatDatabaseSO>(DatabasePath);
            if (existing != null)
            {
                Debug.Log("[CombatDatabaseCreator] CombatDatabase already exists, skipping.");
                return existing;
            }

            var skeleton = enemies.Find(e => e.id == "skeleton");
            var zombie = enemies.Find(e => e.id == "zombie");
            var ghost = enemies.Find(e => e.id == "ghost");

            var db = ScriptableObject.CreateInstance<CombatDatabaseSO>();
            db.enemies = new List<EnemyDefinitionSO>(enemies);

            var tier = new TierDataSO
            {
                id = "tier_1",
                displayName = "Act I",
                scaling = 1f,
                maps = new List<MapDataSO>()
            };

            var map = new MapDataSO
            {
                id = "map_1_1",
                displayName = "Twilight Shore",
                battles = new List<BattleDataSO>()
            };

            for (int b = 0; b < 10; b++)
            {
                var battle = new BattleDataSO
                {
                    id = $"battle_1_1_{b:D2}",
                    waves = BuildWaves(b, skeleton, zombie, ghost)
                };
                map.battles.Add(battle);
            }

            tier.maps.Add(map);
            db.tiers.Add(tier);

            AssetDatabase.CreateAsset(db, DatabasePath);
            return db;
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

        private static void CreateLootTable()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LootTableSO>(LootTablePath);
            if (existing != null)
            {
                Debug.Log("[CombatDatabaseCreator] LootTable already exists, skipping.");
                return;
            }

            var lt = ScriptableObject.CreateInstance<LootTableSO>();
            lt.baseDropChance = 0.3f;
            lt.dropChancePerBattle = 0.025f;
            lt.maxDropChance = 0.65f;
            lt.bonusDropChancePerTier = 0.1f;
            lt.minModValue = 1f;
            lt.maxModValue = 10f;

            AssetDatabase.CreateAsset(lt, LootTablePath);
        }
    }
}
