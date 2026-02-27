using System.Collections.Generic;
using Game.Application.Loot;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Application.Combat
{
    public sealed class GrantBattleRewardUseCase
    {
        private readonly ICombatConfigProvider _combatConfig;
        private readonly IRandomService _random;
        private readonly ItemRollingService _itemRolling;

        public GrantBattleRewardUseCase(
            ICombatConfigProvider combatConfig,
            IRandomService random,
            ItemRollingService itemRolling)
        {
            _combatConfig = combatConfig;
            _random = random;
            _itemRolling = itemRolling;
        }

        public List<ItemInstance> Execute(int battleIndex, int tierIndex)
        {
            var drops = new List<ItemInstance>();

            float dropChance = _combatConfig.GetDropChance(battleIndex, tierIndex);

            if (_random.NextDouble() > dropChance)
                return drops;

            var item = _itemRolling.RollRandomItem();
            if (item != null)
                drops.Add(item);

            float bonusChance = _combatConfig.GetBonusDropChance(tierIndex);
            if (bonusChance > 0 && _random.NextDouble() < bonusChance)
            {
                var bonus = _itemRolling.RollRandomItem();
                if (bonus != null)
                    drops.Add(bonus);
            }

            return drops;
        }
    }
}
