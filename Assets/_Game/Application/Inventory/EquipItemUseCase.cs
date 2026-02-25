using System.Collections.Generic;
using Game.Domain.Characters;
using Game.Domain.Items;
using Game.Domain.Stats;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Inventory
{
    public sealed class EquipItemUseCase
    {
        private readonly Stats.CalculateHeroStatsUseCase _calcStats;

        public EquipItemUseCase(Stats.CalculateHeroStatsUseCase calcStats)
        {
            _calcStats = calcStats;
        }

        public EquipItemResult Execute(InventoryModel inventory, HeroState hero, string itemUid,
            EquipmentSlotType targetSlot = EquipmentSlotType.None)
        {
            if (!inventory.TryEquip(itemUid, targetSlot, out _, out var resolvedSlot, out _))
                return new EquipItemResult(false);

            var finalStats = _calcStats.Execute(hero, inventory.Equipped);

            return new EquipItemResult(true, resolvedSlot, finalStats);
        }
    }

    public sealed class EquipItemResult
    {
        public bool Success { get; }
        public EquipmentSlotType? Slot { get; }
        public Dictionary<StatType, float> FinalStats { get; }

        public EquipItemResult(bool success,
            EquipmentSlotType? slot = null,
            Dictionary<StatType, float> finalStats = null)
        {
            Success = success;
            Slot = slot;
            FinalStats = finalStats;
        }
    }
}
