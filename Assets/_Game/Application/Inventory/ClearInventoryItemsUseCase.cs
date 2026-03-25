using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Inventory
{
    public sealed class ClearInventoryItemsUseCase
    {
        public bool Execute(InventoryModel inventory)
        {
            if (inventory == null || inventory.Items.Count == 0)
                return false;

            inventory.ClearItems();
            return true;
        }
    }
}
