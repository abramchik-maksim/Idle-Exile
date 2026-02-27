using Game.Domain.Items;

namespace Game.Domain.DTOs.Combat
{
    public readonly struct LootDroppedDTO
    {
        public string ItemName { get; }
        public Rarity Rarity { get; }

        public LootDroppedDTO(string itemName, Rarity rarity)
        {
            ItemName = itemName;
            Rarity = rarity;
        }
    }
}
