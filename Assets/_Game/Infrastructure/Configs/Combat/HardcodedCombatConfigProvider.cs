using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Combat.Progression;

namespace Game.Infrastructure.Configs.Combat
{
    public sealed class HardcodedCombatConfigProvider : ICombatConfigProvider
    {
        private readonly List<TierDefinition> _tiers = new();
        private readonly Dictionary<string, List<MapDefinition>> _maps = new();
        private readonly Dictionary<string, List<BattleDefinition>> _battles = new();
        private readonly Dictionary<string, EnemyDefinition> _enemies = new();

        public HardcodedCombatConfigProvider()
        {
            BuildEnemies();
            BuildContent();
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

        public float GetTierScaling(int tierIndex) => 1f + tierIndex * 0.5f;

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

        private void BuildEnemies()
        {
            _enemies["skeleton"] = new EnemyDefinition("skeleton", "Skeleton", 30f, 5f, 2f, 2f);
            _enemies["zombie"] = new EnemyDefinition("zombie", "Zombie", 50f, 8f, 4f, 1.2f);
            _enemies["ghost"] = new EnemyDefinition("ghost", "Ghost", 20f, 10f, 0f, 3f);
        }

        private void BuildContent()
        {
            var mapId = "map_1_1";
            var battleIds = new List<string>();
            var battleList = new List<BattleDefinition>();

            for (int b = 0; b < 10; b++)
            {
                var battleId = $"battle_1_1_{b:D2}";
                battleIds.Add(battleId);

                var waves = BuildWavesForBattle(b);
                var rewards = BuildRewardsForBattle(b);

                battleList.Add(new BattleDefinition(battleId, mapId, b, waves, rewards));
            }

            var map = new MapDefinition(mapId, "Twilight Shore", "tier_1", battleIds);
            _maps["tier_1"] = new List<MapDefinition> { map };
            _battles[mapId] = battleList;

            var tier = new TierDefinition("tier_1", "Act I", 0, new List<string> { mapId });
            _tiers.Add(tier);
        }

        private List<WaveDefinition> BuildWavesForBattle(int battleIndex)
        {
            var waves = new List<WaveDefinition>();
            int waveCount = 2 + battleIndex / 4;
            waveCount = Math.Min(waveCount, 4);

            for (int w = 0; w < waveCount; w++)
            {
                int enemyCount = 2 + battleIndex / 3 + w;
                enemyCount = Math.Min(enemyCount, 8);

                string enemyId = battleIndex < 5 ? "skeleton" : (w == waveCount - 1 ? "zombie" : "skeleton");
                float delay = w == 0 ? 1.0f : 2.0f;

                var spawns = new List<WaveSpawnEntry> { new(enemyId, enemyCount) };

                if (battleIndex >= 7 && w >= 1)
                    spawns.Add(new WaveSpawnEntry("ghost", 1 + battleIndex / 8));

                waves.Add(new WaveDefinition(spawns, delay));
            }

            return waves;
        }

        private List<RewardEntry> BuildRewardsForBattle(int battleIndex)
        {
            var rewards = new List<RewardEntry>
            {
                new(RewardType.Experience, "xp", 10 + battleIndex * 5)
            };

            if (battleIndex % 3 == 2)
                rewards.Add(new RewardEntry(RewardType.Currency, "gold", 5 + battleIndex * 2));

            return rewards;
        }
    }
}
