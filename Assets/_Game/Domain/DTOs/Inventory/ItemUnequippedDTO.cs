using Game.Domain.Items;

namespace Game.Domain.DTOs.Inventory
{
    public readonly struct ItemUnequippedDTO
    {
        public EquipmentSlotType Slot { get; }
        public ItemUnequippedDTO(EquipmentSlotType slot) => Slot = slot;
    }
}
