using Game.Domain.Items;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Inventory
{
    public sealed class InventoryCommandService
    {
        public bool TryAddItem(InventoryModel inventory, ItemInstance item)
        {
            if (inventory == null || item == null) return false;
            return inventory.TryAdd(item);
        }

        public bool RemoveItem(InventoryModel inventory, string itemUid)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(itemUid)) return false;
            return inventory.Remove(itemUid);
        }

        public bool ClearItems(InventoryModel inventory)
        {
            if (inventory == null || inventory.Items.Count == 0) return false;
            inventory.ClearItems();
            return true;
        }
    }
}

