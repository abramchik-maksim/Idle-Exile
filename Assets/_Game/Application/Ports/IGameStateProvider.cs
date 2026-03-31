using Game.Domain.Characters;
using Game.Domain.Progression.TreeTalents;
using Game.Domain.Skills;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Ports
{
    public interface IGameStateProvider
    {
        HeroState Hero { get; }
        InventoryModel Inventory { get; }
        PlayerProgressData Progress { get; }
        SkillCollection Skills { get; }
        SkillLoadout Loadout { get; }
        TreeTalentsState TreeTalents { get; }
    }
}
