using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Inventory
{
    public sealed class RemoveInventoryItemUseCase
    {
        public bool Execute(InventoryModel inventory, string uid)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(uid))
                return false;

            return inventory.Remove(uid);
        }
    }
}
