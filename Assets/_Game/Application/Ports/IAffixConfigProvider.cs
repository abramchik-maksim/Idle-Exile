using System.Collections.Generic;
using Game.Domain.Items;

namespace Game.Application.Ports
{
    public interface IAffixConfigProvider
    {
        IReadOnlyList<ItemAffixPoolEntry> PoolEntries { get; }

        /// <summary>Returns false if mod is not allowed on this equipment slot (after Ring normalization).</summary>
        bool IsModAllowedOnSlot(string modId, EquipmentSlotType slot);

        /// <summary>Combined weight from pool row and AffixAllowedSlots (1 if no slot row).</summary>
        float GetSlotWeightMultiplier(string modId, EquipmentSlotType slot);
    }
}
