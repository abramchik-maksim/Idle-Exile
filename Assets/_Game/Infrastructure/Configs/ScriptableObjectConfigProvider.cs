using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Infrastructure.Configs
{
    public sealed class ScriptableObjectConfigProvider : IConfigProvider
    {
        private readonly Dictionary<string, ItemDefinition> _items = new();
        private readonly List<ItemDefinition> _allItems = new();

        public ScriptableObjectConfigProvider(ItemDatabaseSO database)
        {
            foreach (var so in database.items)
            {
                if (so == null) continue;
                var def = so.ToDomain();
                _items[def.Id] = def;
                _allItems.Add(def);
            }
        }

        public ItemDefinition GetItemDefinition(string id) =>
            _items.TryGetValue(id, out var def) ? def : null;

        public IReadOnlyList<ItemDefinition> GetAllItems() => _allItems;

        public float GetEnemyHealthBase(int waveIndex) => 20f + waveIndex * 8f;

        public float GetEnemyDamageBase(int waveIndex) => 3f + waveIndex * 1.5f;
    }
}
