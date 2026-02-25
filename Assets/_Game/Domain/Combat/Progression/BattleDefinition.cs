using System.Collections.Generic;

namespace Game.Domain.Combat.Progression
{
    public sealed class BattleDefinition
    {
        public string Id { get; }
        public string MapId { get; }
        public int Order { get; }
        public IReadOnlyList<WaveDefinition> Waves { get; }
        public IReadOnlyList<RewardEntry> Rewards { get; }

        public BattleDefinition(
            string id, string mapId, int order,
            IReadOnlyList<WaveDefinition> waves,
            IReadOnlyList<RewardEntry> rewards)
        {
            Id = id;
            MapId = mapId;
            Order = order;
            Waves = waves;
            Rewards = rewards;
        }
    }
}
