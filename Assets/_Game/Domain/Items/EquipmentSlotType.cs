namespace Game.Domain.Items
{
    public enum EquipmentSlotType
    {
        None,

        // Armor
        Helmet,
        BodyArmor,
        Gloves,
        Boots,

        // Jewelry
        Amulet,
        Belt,
        Ring,   // item definition type — maps to Ring1 or Ring2 at equip time
        Ring1,  // equipment position (left column)
        Ring2,  // equipment position (right column)

        // Weapons
        MainHand,
        OffHand
    }
}
