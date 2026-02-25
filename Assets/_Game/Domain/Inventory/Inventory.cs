using System.Collections.Generic;
using System.Linq;
using Game.Domain.Items;

namespace Game.Domain.Inventory
{
    public sealed class Inventory
    {
        private readonly List<ItemInstance> _items = new();
        private readonly Dictionary<EquipmentSlotType, ItemInstance> _equipped = new();

        public int Capacity { get; }
        public IReadOnlyList<ItemInstance> Items => _items;
        public IReadOnlyDictionary<EquipmentSlotType, ItemInstance> Equipped => _equipped;
        public bool IsFull => _items.Count >= Capacity;

        public Inventory(int capacity = 30)
        {
            Capacity = capacity;
        }

        public bool TryAdd(ItemInstance item)
        {
            if (IsFull) return false;
            _items.Add(item);
            return true;
        }

        public bool Remove(string uid)
        {
            var item = _items.FirstOrDefault(i => i.Uid == uid);
            if (item == null) return false;
            _items.Remove(item);
            return true;
        }

        public ItemInstance Find(string uid) =>
            _items.FirstOrDefault(i => i.Uid == uid);

        public bool TryEquip(string uid, out ItemInstance previousItem)
        {
            previousItem = null;
            var item = Find(uid);
            if (item == null) return false;
            if (item.Definition.Slot == EquipmentSlotType.None) return false;

            var slot = item.Definition.Slot;
            if (_equipped.TryGetValue(slot, out var prev))
            {
                previousItem = prev;
                _equipped.Remove(slot);
            }

            int index = _items.IndexOf(item);
            _equipped[slot] = item;
            _items.Remove(item);
            if (previousItem != null)
                _items.Insert(index, previousItem);
            return true;
        }

        public bool TryUnequip(EquipmentSlotType slot, out ItemInstance unequipped)
        {
            unequipped = null;
            if (!_equipped.TryGetValue(slot, out var item)) return false;
            if (IsFull) return false;

            _equipped.Remove(slot);
            _items.Add(item);
            unequipped = item;
            return true;
        }
    }
}
