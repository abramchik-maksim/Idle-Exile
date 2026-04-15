using System.Collections.Generic;

namespace Game.Domain.Combat.Progression
{
    public sealed class MapDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string TierId { get; }
        public IReadOnlyList<string> BattleIds { get; }
        public string LocationId { get; }
        public LootBias LootBias { get; }
        public IReadOnlyList<MapModifier> Modifiers { get; }
        public bool IsBossMap { get; }

        public MapDefinition(
            string id, string name, string tierId,
            IReadOnlyList<string> battleIds,
            string locationId = null,
            string description = null,
            LootBias lootBias = default,
            IReadOnlyList<MapModifier> modifiers = null,
            bool isBossMap = false)
        {
            Id = id;
            Name = name;
            Description = description ?? string.Empty;
            TierId = tierId;
            BattleIds = battleIds;
            LocationId = locationId;
            LootBias = lootBias.ItemWeightMultiplier == 0f && lootBias.CurrencyWeightMultiplier == 0f
                ? LootBias.Default
                : lootBias;
            Modifiers = modifiers ?? System.Array.Empty<MapModifier>();
            IsBossMap = isBossMap;
        }
    }
}
