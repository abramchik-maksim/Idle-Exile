namespace Game.Domain.Items
{
    public static class EquipmentSlotAffixExtensions
    {
        /// <summary>
        /// Ring1/Ring2 use the same affix slot rules as Ring.
        /// </summary>
        public static EquipmentSlotType NormalizeForAffixRules(this EquipmentSlotType slot) =>
            slot switch
            {
                EquipmentSlotType.Ring1 => EquipmentSlotType.Ring,
                EquipmentSlotType.Ring2 => EquipmentSlotType.Ring,
                _ => slot
            };
    }
}
