using System.Collections.Generic;
using Game.Domain.Stats;

namespace Game.Domain.Items
{
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public Rarity Rarity { get; }
        public EquipmentSlotType Slot { get; }
        public IReadOnlyList<Modifier> ImplicitModifiers { get; }

        public ItemDefinition(
            string id, string name, Rarity rarity,
            EquipmentSlotType slot, IReadOnlyList<Modifier> implicitModifiers)
        {
            Id = id;
            Name = name;
            Rarity = rarity;
            Slot = slot;
            ImplicitModifiers = implicitModifiers;
        }
    }
}
