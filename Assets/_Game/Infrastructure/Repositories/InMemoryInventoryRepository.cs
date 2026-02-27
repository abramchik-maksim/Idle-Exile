using Game.Application.Ports;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Infrastructure.Repositories
{
    public sealed class InMemoryInventoryRepository : IInventoryRepository
    {
        private InventoryModel _cached;

        public void Save(InventoryModel inventory) => _cached = inventory;

        public InventoryModel Load() => _cached ?? new InventoryModel();

        public void Delete() => _cached = null;
    }
}
