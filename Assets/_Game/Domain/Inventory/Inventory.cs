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

        public Inventory(int capacity = 32)
        {
            Capacity = capacity;
        }

        public Inventory(int capacity, List<ItemInstance> items,
            Dictionary<EquipmentSlotType, ItemInstance> equipped)
        {
            Capacity = capacity;
            _items = new List<ItemInstance>(items);
            _equipped = new Dictionary<EquipmentSlotType, ItemInstance>(equipped);
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

        public void ClearItems()
        {
            _items.Clear();
        }

        public void ClearAll()
        {
            _items.Clear();
            _equipped.Clear();
        }

        public bool TryEquip(string uid, out ItemInstance previousItem)
        {
            return TryEquip(uid, EquipmentSlotType.None, out previousItem, out _, out _);
        }

        public bool TryEquip(string uid, out ItemInstance previousItem,
            out EquipmentSlotType resolvedSlot, out List<ItemInstance> additionalUnequipped)
        {
            return TryEquip(uid, EquipmentSlotType.None, out previousItem, out resolvedSlot, out additionalUnequipped);
        }

        public bool TryEquip(string uid, EquipmentSlotType targetSlotOverride,
            out ItemInstance previousItem, out EquipmentSlotType resolvedSlot,
            out List<ItemInstance> additionalUnequipped)
        {
            previousItem = null;
            resolvedSlot = EquipmentSlotType.None;
            additionalUnequipped = null;

            var item = Find(uid);
            if (item == null) return false;

            var targetSlot = targetSlotOverride != EquipmentSlotType.None
                ? ValidateOverride(item, targetSlotOverride)
                : ResolveTargetSlot(item);

            if (targetSlot == EquipmentSlotType.None) return false;

            if (targetSlot == EquipmentSlotType.OffHand &&
                _equipped.TryGetValue(EquipmentSlotType.MainHand, out var mainHand) &&
                mainHand.Definition.Handedness == Handedness.TwoHanded)
                return false;

            int index = _items.IndexOf(item);
            _items.Remove(item);

            if (_equipped.TryGetValue(targetSlot, out var prev))
            {
                previousItem = prev;
                _equipped.Remove(targetSlot);
                _items.Insert(index, previousItem);
            }

            if (item.Definition.Handedness == Handedness.TwoHanded &&
                _equipped.TryGetValue(EquipmentSlotType.OffHand, out var offHand))
            {
                _equipped.Remove(EquipmentSlotType.OffHand);
                additionalUnequipped = new List<ItemInstance> { offHand };
                _items.Add(offHand);
            }

            _equipped[targetSlot] = item;
            resolvedSlot = targetSlot;
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

        public ItemInstance GetEquippedFor(EquipmentSlotType itemSlot)
        {
            if (itemSlot == EquipmentSlotType.Ring)
            {
                if (_equipped.TryGetValue(EquipmentSlotType.Ring1, out var r1)) return r1;
                if (_equipped.TryGetValue(EquipmentSlotType.Ring2, out var r2)) return r2;
                return null;
            }

            _equipped.TryGetValue(itemSlot, out var found);
            return found;
        }

        private EquipmentSlotType ResolveTargetSlot(ItemInstance item)
        {
            var slot = item.Definition.Slot;
            var handedness = item.Definition.Handedness;

            if (slot == EquipmentSlotType.Ring)
            {
                if (!_equipped.ContainsKey(EquipmentSlotType.Ring1))
                    return EquipmentSlotType.Ring1;
                if (!_equipped.ContainsKey(EquipmentSlotType.Ring2))
                    return EquipmentSlotType.Ring2;
                return EquipmentSlotType.Ring1;
            }

            if (handedness == Handedness.Versatile)
            {
                if (!_equipped.ContainsKey(EquipmentSlotType.MainHand))
                    return EquipmentSlotType.MainHand;
                if (!_equipped.ContainsKey(EquipmentSlotType.OffHand))
                    return EquipmentSlotType.OffHand;
                return EquipmentSlotType.MainHand;
            }

            return slot;
        }

        private EquipmentSlotType ValidateOverride(ItemInstance item, EquipmentSlotType requested)
        {
            if (EquipmentSlotHelper.IsSlotMatch(item.Definition.Slot, requested, item.Definition.Handedness))
                return requested;

            return EquipmentSlotType.None;
        }
    }
}
