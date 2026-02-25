namespace Game.Domain.Items
{
    public static class EquipmentSlotHelper
    {
        public static bool IsSlotMatch(EquipmentSlotType itemSlot, EquipmentSlotType targetSlot,
            Handedness handedness = Handedness.None)
        {
            if (itemSlot == EquipmentSlotType.Ring)
                return targetSlot is EquipmentSlotType.Ring1 or EquipmentSlotType.Ring2;

            if (handedness == Handedness.Versatile)
                return targetSlot is EquipmentSlotType.MainHand or EquipmentSlotType.OffHand;

            return itemSlot == targetSlot;
        }
    }
}
