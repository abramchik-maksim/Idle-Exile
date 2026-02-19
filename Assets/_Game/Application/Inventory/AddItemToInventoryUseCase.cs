using Game.Domain.Items;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Inventory
{
    public sealed class AddItemToInventoryUseCase
    {
        public bool Execute(InventoryModel inventory, ItemInstance item)
        {
            return inventory.TryAdd(item);
        }
    }
}
