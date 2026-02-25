using Game.Domain.Characters;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Ports
{
    public interface IGameStateProvider
    {
        HeroState Hero { get; }
        InventoryModel Inventory { get; }
        PlayerProgressData Progress { get; }
    }
}
