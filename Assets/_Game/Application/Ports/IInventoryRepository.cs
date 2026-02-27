using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Ports
{
    public interface IInventoryRepository
    {
        void Save(InventoryModel inventory);
        InventoryModel Load();
        void Delete();
    }
}
