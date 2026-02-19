using Game.Domain.Items;

namespace Game.Domain.DTOs.Inventory
{
    public readonly struct ItemEquippedDTO
    {
        public string ItemUid { get; }
        public EquipmentSlotType Slot { get; }
        public ItemEquippedDTO(string itemUid, EquipmentSlotType slot)
        {
            ItemUid = itemUid;
            Slot = slot;
        }
    }
}
