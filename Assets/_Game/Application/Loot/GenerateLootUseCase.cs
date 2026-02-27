using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Application.Loot
{
    public sealed class GenerateLootUseCase
    {
        private readonly IRandomService _random;
        private readonly ItemRollingService _itemRolling;

        public GenerateLootUseCase(IRandomService random, ItemRollingService itemRolling)
        {
            _random = random;
            _itemRolling = itemRolling;
        }

        public List<ItemInstance> Execute(string enemyDefinitionId, int waveIndex)
        {
            var drops = new List<ItemInstance>();

            if (_random.NextDouble() > 0.4) return drops;

            var item = _itemRolling.RollRandomItem();
            if (item != null)
                drops.Add(item);

            return drops;
        }
    }
}
