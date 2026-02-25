using System.Collections.Generic;

namespace Game.Domain.Combat.Progression
{
    public sealed class MapDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string TierId { get; }
        public IReadOnlyList<string> BattleIds { get; }

        public MapDefinition(string id, string name, string tierId, IReadOnlyList<string> battleIds)
        {
            Id = id;
            Name = name;
            TierId = tierId;
            BattleIds = battleIds;
        }
    }
}
