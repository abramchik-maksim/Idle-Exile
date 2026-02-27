using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Combat.Progression;

namespace Game.Infrastructure.Configs.Combat
{
    public sealed class ScriptableObjectCombatConfigProvider : ICombatConfigProvider
    {
        private readonly List<TierDefinition> _tiers = new();
        private readonly Dictionary<string, List<MapDefinition>> _maps = new();
        private readonly Dictionary<string, List<BattleDefinition>> _battles = new();
        private readonly Dictionary<string, EnemyDefinition> _enemies = new();
        private readonly List<float> _tierScalings = new();

        private readonly float _baseDropChance;
        private readonly float _dropChancePerBattle;
        private readonly float _maxDropChance;
        private readonly float _bonusDropChancePerTier;
        private readonly float _minModValue;
        private readonly float _maxModValue;

        public ScriptableObjectCombatConfigProvider(CombatDatabaseSO database, LootTableSO lootTable)
        {
            BuildFromDatabase(database);

            _baseDropChance = lootTable.baseDropChance;
            _dropChancePerBattle = lootTable.dropChancePerBattle;
            _maxDropChance = lootTable.maxDropChance;
            _bonusDropChancePerTier = lootTable.bonusDropChancePerTier;
            _minModValue = lootTable.minModValue;
            _maxModValue = lootTable.maxModValue;
        }

        public TierDefinition GetTier(int tierIndex) =>
            tierIndex >= 0 && tierIndex < _tiers.Count ? _tiers[tierIndex] : null;

        public MapDefinition GetMap(int tierIndex, int mapIndex)
        {
            var tier = GetTier(tierIndex);
            if (tier == null || mapIndex < 0 || mapIndex >= tier.MapIds.Count) return null;
            return _maps.TryGetValue(tier.Id, out var maps) && mapIndex < maps.Count ? maps[mapIndex] : null;
        }

        public BattleDefinition GetBattle(int tierIndex, int mapIndex, int battleIndex)
        {
            var map = GetMap(tierIndex, mapIndex);
            if (map == null || battleIndex < 0 || battleIndex >= map.BattleIds.Count) return null;
            return _battles.TryGetValue(map.Id, out var battles) && battleIndex < battles.Count
                ? battles[battleIndex]
                : null;
        }

        public EnemyDefinition GetEnemy(string enemyId) =>
            _enemies.TryGetValue(enemyId, out var def) ? def : null;

        public float GetTierScaling(int tierIndex) =>
            tierIndex >= 0 && tierIndex < _tierScalings.Count ? _tierScalings[tierIndex] : 1f;

        public int GetTierCount() => _tiers.Count;

        public int GetMapCount(int tierIndex)
        {
            var tier = GetTier(tierIndex);
            return tier?.MapIds.Count ?? 0;
        }

        public int GetBattleCount(int tierIndex, int mapIndex)
        {
            var map = GetMap(tierIndex, mapIndex);
            return map?.BattleIds.Count ?? 0;
        }

        public float GetDropChance(int battleIndex, int tierIndex) =>
            Math.Min(_baseDropChance + battleIndex * _dropChancePerBattle, _maxDropChance);

        public float GetBonusDropChance(int tierIndex) =>
            tierIndex * _bonusDropChancePerTier;

        public float GetMinModValue() => _minModValue;
        public float GetMaxModValue() => _maxModValue;

        private void BuildFromDatabase(CombatDatabaseSO db)
        {
            foreach (var enemySO in db.enemies)
            {
                if (enemySO == null) continue;
                _enemies[enemySO.id] = enemySO.ToDomain();
            }

            for (int t = 0; t < db.tiers.Count; t++)
            {
                var tierSO = db.tiers[t];
                var mapIds = new List<string>();

                _tierScalings.Add(tierSO.scaling);

                for (int m = 0; m < tierSO.maps.Count; m++)
                {
                    var mapSO = tierSO.maps[m];
                    mapIds.Add(mapSO.id);

                    var battleIds = new List<string>();
                    var battleList = new List<BattleDefinition>();

                    for (int b = 0; b < mapSO.battles.Count; b++)
                    {
                        var battleSO = mapSO.battles[b];
                        battleIds.Add(battleSO.id);

                        var waves = new List<WaveDefinition>();
                        foreach (var waveSO in battleSO.waves)
                        {
                            var spawns = new List<WaveSpawnEntry>();
                            foreach (var spawnSO in waveSO.spawns)
                            {
                                if (spawnSO.enemy == null) continue;
                                spawns.Add(new WaveSpawnEntry(spawnSO.enemy.id, spawnSO.count));
                            }
                            waves.Add(new WaveDefinition(spawns, waveSO.delayBeforeWave));
                        }

                        battleList.Add(new BattleDefinition(
                            battleSO.id, mapSO.id, b, waves, new List<RewardEntry>()));
                    }

                    _battles[mapSO.id] = battleList;
                    _maps.TryAdd(tierSO.id, new List<MapDefinition>());
                    _maps[tierSO.id].Add(new MapDefinition(mapSO.id, mapSO.displayName, tierSO.id, battleIds));
                }

                _tiers.Add(new TierDefinition(tierSO.id, tierSO.displayName, t, mapIds));
            }
        }
    }
}
