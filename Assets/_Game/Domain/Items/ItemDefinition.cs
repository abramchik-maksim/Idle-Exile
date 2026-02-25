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
        public Handedness Handedness { get; }
        public string IconAddress { get; }
        public IReadOnlyList<Modifier> ImplicitModifiers { get; }

        public ItemDefinition(
            string id, string name, Rarity rarity,
            EquipmentSlotType slot, IReadOnlyList<Modifier> implicitModifiers,
            string iconAddress = null,
            Handedness handedness = Handedness.None)
        {
            Id = id;
            Name = name;
            Rarity = rarity;
            Slot = slot;
            Handedness = handedness;
            IconAddress = iconAddress;
            ImplicitModifiers = implicitModifiers;
        }
    }
}
