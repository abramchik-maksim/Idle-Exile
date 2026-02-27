using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Infrastructure.Configs
{
    public sealed class HardcodedConfigProvider : IConfigProvider
    {
        private readonly Dictionary<string, ItemDefinition> _items;

        public HardcodedConfigProvider()
        {
            _items = BuildItemTable();
        }

        public ItemDefinition GetItemDefinition(string id) =>
            _items.TryGetValue(id, out var def) ? def : null;

        public IReadOnlyList<ItemDefinition> GetAllItems() =>
            new List<ItemDefinition>(_items.Values);

        private static Dictionary<string, ItemDefinition> BuildItemTable()
        {
            return new Dictionary<string, ItemDefinition>
            {
                ["rusty_sword"] = new("rusty_sword", "Rusty Sword", Rarity.Normal, EquipmentSlotType.MainHand,
                    new[] { new Modifier(StatType.PhysicalDamage, ModifierType.Flat, 5f, "implicit") },
                    handedness: Handedness.Versatile),

                ["iron_sword"] = new("iron_sword", "Iron Sword", Rarity.Magic, EquipmentSlotType.MainHand,
                    new[] { new Modifier(StatType.PhysicalDamage, ModifierType.Flat, 10f, "implicit") },
                    handedness: Handedness.Versatile),

                ["leather_vest"] = new("leather_vest", "Leather Vest", Rarity.Normal, EquipmentSlotType.BodyArmor,
                    new[] { new Modifier(StatType.Armor, ModifierType.Flat, 8f, "implicit"),
                            new Modifier(StatType.MaxHealth, ModifierType.Flat, 15f, "implicit") }),

                ["iron_helmet"] = new("iron_helmet", "Iron Helmet", Rarity.Normal, EquipmentSlotType.Helmet,
                    new[] { new Modifier(StatType.Armor, ModifierType.Flat, 5f, "implicit") }),

                ["worn_gloves"] = new("worn_gloves", "Worn Gloves", Rarity.Normal, EquipmentSlotType.Gloves,
                    new[] { new Modifier(StatType.AttackSpeed, ModifierType.Increased, 0.05f, "implicit") }),

                ["simple_boots"] = new("simple_boots", "Simple Boots", Rarity.Normal, EquipmentSlotType.Boots,
                    new[] { new Modifier(StatType.MovementSpeed, ModifierType.Flat, 1f, "implicit") }),

                ["jade_amulet"] = new("jade_amulet", "Jade Amulet", Rarity.Magic, EquipmentSlotType.Amulet,
                    new[] { new Modifier(StatType.MaxHealth, ModifierType.Flat, 20f, "implicit") }),

                ["leather_belt"] = new("leather_belt", "Leather Belt", Rarity.Normal, EquipmentSlotType.Belt,
                    new[] { new Modifier(StatType.MaxHealth, ModifierType.Flat, 10f, "implicit") }),

                ["iron_ring"] = new("iron_ring", "Iron Ring", Rarity.Normal, EquipmentSlotType.Ring,
                    new[] { new Modifier(StatType.PhysicalDamage, ModifierType.Flat, 2f, "implicit") }),

                ["wooden_shield"] = new("wooden_shield", "Wooden Shield", Rarity.Normal, EquipmentSlotType.OffHand,
                    new[] { new Modifier(StatType.Armor, ModifierType.Flat, 12f, "implicit") },
                    handedness: Handedness.OffHandOnly),

                ["great_axe"] = new("great_axe", "Great Axe", Rarity.Rare, EquipmentSlotType.MainHand,
                    new[] { new Modifier(StatType.PhysicalDamage, ModifierType.Flat, 18f, "implicit") },
                    handedness: Handedness.TwoHanded),
            };
        }
    }
}
