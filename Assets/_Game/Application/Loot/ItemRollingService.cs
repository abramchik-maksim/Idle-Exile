using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Application.Loot
{
    public sealed class ItemRollingService
    {
        private readonly IConfigProvider _config;
        private readonly IRandomService _random;

        public ItemRollingService(IConfigProvider config, IRandomService random)
        {
            _config = config;
            _random = random;
        }

        public ItemInstance RollRandomItem()
        {
            var allItems = _config.GetAllItems();
            if (allItems.Count == 0) return null;

            var def = allItems[_random.Next(0, allItems.Count)];
            var mods = RollModifiers(def);
            return new ItemInstance(def, mods);
        }

        public List<Modifier> RollModifiers(ItemDefinition def)
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
