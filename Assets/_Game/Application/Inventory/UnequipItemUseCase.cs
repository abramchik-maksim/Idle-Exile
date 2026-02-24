using System.Collections.Generic;
using Game.Domain.Characters;
using Game.Domain.Items;
using Game.Domain.Stats;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Application.Inventory
{
    public sealed class UnequipItemUseCase
    {
        private readonly Stats.CalculateHeroStatsUseCase _calcStats;

        public UnequipItemUseCase(Stats.CalculateHeroStatsUseCase calcStats)
        {
            _calcStats = calcStats;
        }

        public UnequipItemResult Execute(InventoryModel inventory, HeroState hero, EquipmentSlotType slot)
        {
            if (!inventory.TryUnequip(slot, out var unequipped))
                return new UnequipItemResult(false);

            var finalStats = _calcStats.Execute(hero, inventory.Equipped);
            return new UnequipItemResult(true, slot, unequipped, finalStats);
        }
    }

    public sealed class UnequipItemResult
    {
        public bool Success { get; }
        public EquipmentSlotType? Slot { get; }
        public ItemInstance UnequippedItem { get; }
        public Dictionary<StatType, float> FinalStats { get; }

        public UnequipItemResult(bool success,
            EquipmentSlotType? slot = null,
            ItemInstance unequippedItem = null,
            Dictionary<StatType, float> finalStats = null)
        {
            Success = success;
            Slot = slot;
            UnequippedItem = unequippedItem;
            FinalStats = finalStats;
        }
    }
}
