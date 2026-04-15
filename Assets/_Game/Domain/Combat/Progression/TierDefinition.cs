using System.Collections.Generic;

namespace Game.Domain.Combat.Progression
{
    public sealed class TierDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public int Order { get; }
        public IReadOnlyList<string> MapIds { get; }
        public bool HasForcedStartMap { get; }
        /// <summary>Number of map choice points in this tier (each offers 2 options).</summary>
        public int MapChoiceCount { get; }

        public TierDefinition(string id, string name, int order, IReadOnlyList<string> mapIds,
            bool hasForcedStartMap = false, int mapChoiceCount = 0)
        {
            Id = id;
            Name = name;
            Order = order;
            MapIds = mapIds;
            HasForcedStartMap = hasForcedStartMap;
            MapChoiceCount = mapChoiceCount > 0 ? mapChoiceCount : (hasForcedStartMap ? mapIds.Count - 1 : mapIds.Count);
        }
    }
}
