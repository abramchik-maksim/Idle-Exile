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
        private readonly Dictionary<(int tierIndex, int choiceIndex), (MapDefinition, MapDefinition)> _mapChoices = new();

        private readonly float _baseDropChance;
        private readonly float _dropChancePerBattle;
        private readonly float _maxDropChance;
        private readonly float _bonusDropChancePerTier;

        public ScriptableObjectCombatConfigProvider(CombatDatabaseSO database, LootTableSO lootTable)
        {
            BuildFromDatabase(database);

            _baseDropChance = lootTable.baseDropChance;
            _dropChancePerBattle = lootTable.dropChancePerBattle;
            _maxDropChance = lootTable.maxDropChance;
            _bonusDropChancePerTier = lootTable.bonusDropChancePerTier;
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

        public (MapDefinition Option1, MapDefinition Option2) GetMapChoice(int tierIndex, int choiceIndex)
        {
            if (_mapChoices.TryGetValue((tierIndex, choiceIndex), out var pair))
                return pair;
            return (null, null);
        }

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
                if (tierSO == null) continue;

                var mapIds = new List<string>();
                _tierScalings.Add(tierSO.scaling);

                for (int m = 0; m < tierSO.maps.Count; m++)
                {
                    var mapSO = tierSO.maps[m];
                    if (mapSO == null) continue;
                    BuildMap(tierSO, mapSO, mapIds);
                }

                for (int c = 0; c < tierSO.mapChoices.Count; c++)
                {
                    var choice = tierSO.mapChoices[c];
                    MapDefinition opt1 = null, opt2 = null;

                    if (choice?.option1 != null)
                        opt1 = BuildMapDefinition(tierSO, choice.option1);
                    if (choice?.option2 != null)
                        opt2 = BuildMapDefinition(tierSO, choice.option2);

                    _mapChoices[(t, c)] = (opt1, opt2);
                }

                int mapChoiceCount = tierSO.mapChoices.Count;
                _tiers.Add(new TierDefinition(tierSO.id, tierSO.displayName, t, mapIds,
                    tierSO.hasForcedStartMap, mapChoiceCount));
            }
        }

        private void BuildMap(TierDefinitionSO tierSO, MapDefinitionSO mapSO, List<string> mapIds)
        {
            mapIds.Add(mapSO.id);
            var mapDef = BuildMapDefinition(tierSO, mapSO);
            _maps.TryAdd(tierSO.id, new List<MapDefinition>());
            _maps[tierSO.id].Add(mapDef);
        }

        private MapDefinition BuildMapDefinition(TierDefinitionSO tierSO, MapDefinitionSO mapSO)
        {
            var battleIds = new List<string>();
            var battleList = new List<BattleDefinition>();

            for (int b = 0; b < mapSO.battles.Count; b++)
            {
                var battleSO = mapSO.battles[b];
                if (battleSO == null) continue;

                battleIds.Add(battleSO.id);
                battleList.Add(BuildBattle(battleSO, mapSO.id, b));
            }

            _battles[mapSO.id] = battleList;

            var modifiers = new List<MapModifier>();
            if (mapSO.modifiers != null)
            {
                foreach (var mod in mapSO.modifiers)
                    modifiers.Add(new MapModifier(mod.type, mod.value));
            }

            var lootBias = new LootBias(mapSO.itemWeightMultiplier, mapSO.currencyWeightMultiplier);

            return new MapDefinition(
                mapSO.id, mapSO.displayName, tierSO.id, battleIds,
                mapSO.locationId, mapSO.description, lootBias, modifiers, mapSO.isBossMap);
        }

        private static BattleDefinition BuildBattle(BattleDefinitionSO battleSO, string mapId, int order)
        {
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

            return new BattleDefinition(battleSO.id, mapId, order, waves, new List<RewardEntry>());
        }
    }
}
