using System.Collections.Generic;

namespace Game.Domain.Combat.Progression
{
    public sealed class TierDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public int Order { get; }
        public IReadOnlyList<string> MapIds { get; }

        public TierDefinition(string id, string name, int order, IReadOnlyList<string> mapIds)
        {
            Id = id;
            Name = name;
            Order = order;
            MapIds = mapIds;
        }
    }
}
