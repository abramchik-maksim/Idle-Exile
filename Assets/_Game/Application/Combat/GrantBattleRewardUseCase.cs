using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Application.Combat
{
    public sealed class GrantBattleRewardUseCase
    {
        private readonly IConfigProvider _itemConfig;
        private readonly ICombatConfigProvider _combatConfig;
        private readonly IRandomService _random;

        public GrantBattleRewardUseCase(
            IConfigProvider itemConfig,
            ICombatConfigProvider combatConfig,
            IRandomService random)
        {
            _itemConfig = itemConfig;
            _combatConfig = combatConfig;
            _random = random;
        }

        public List<ItemInstance> Execute(int battleIndex, int tierIndex)
        {
            var drops = new List<ItemInstance>();

            float dropChance = _combatConfig.GetDropChance(battleIndex, tierIndex);

            if (_random.NextDouble() > dropChance)
                return drops;

            var item = RollItem();
            if (item != null)
                drops.Add(item);

            float bonusChance = _combatConfig.GetBonusDropChance(tierIndex);
            if (bonusChance > 0 && _random.NextDouble() < bonusChance)
            {
                var bonus = RollItem();
                if (bonus != null)
                    drops.Add(bonus);
            }

            return drops;
        }

        private ItemInstance RollItem()
        {
            var allItems = _itemConfig.GetAllItems();
            if (allItems.Count == 0) return null;

            var def = allItems[_random.Next(0, allItems.Count)];
            var mods = RollModifiers(def);
            return new ItemInstance(def, mods);
        }

        private List<Modifier> RollModifiers(ItemDefinition def)
        {
            var mods = new List<Modifier>();
            int count = def.Rarity switch
            {
                Rarity.Normal => 0,
                Rarity.Magic => _random.Next(1, 3),
                Rarity.Rare => _random.Next(3, 6),
                _ => 1
            };

            var pool = ModifierRollingConfig.RollableStats;

            for (int i = 0; i < count; i++)
            {
                var stat = pool[_random.Next(0, pool.Length)];
                var (modType, min, max) = ModifierRollingConfig.GetRange(stat);
                float value = _random.NextFloat(min, max);
                mods.Add(new Modifier(stat, modType, value, "rolled"));
            }

            return mods;
        }
    }
}
