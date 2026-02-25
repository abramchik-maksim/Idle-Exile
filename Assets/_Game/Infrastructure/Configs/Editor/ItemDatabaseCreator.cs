using System.Collections.Generic;
using System.IO;
using Game.Domain.Items;
using Game.Domain.Stats;
using UnityEditor;
using UnityEngine;

namespace Game.Infrastructure.Configs.Editor
{
    public static class ItemDatabaseCreator
    {
        private const string ItemsFolder = "Assets/_Game/Infrastructure/Configs/Items";
        private const string DatabasePath = "Assets/_Game/Infrastructure/Configs/Items/ItemDatabase.asset";

        [MenuItem("Idle Exile/Create Item Database", priority = 100)]
        public static void CreateAll()
        {
            if (!Directory.Exists(ItemsFolder))
                Directory.CreateDirectory(ItemsFolder);

            var definitions = GetItemBlueprints();
            var assets = new List<ItemDefinitionSO>();

            foreach (var bp in definitions)
            {
                var path = $"{ItemsFolder}/{bp.id}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ItemDefinitionSO>(path);

                if (existing != null)
                {
                    existing.id = bp.id;
                    existing.itemName = bp.name;
                    existing.rarity = bp.rarity;
                    existing.slot = bp.slot;
                    existing.handedness = bp.handedness;
                    existing.iconAddress = bp.iconAddress;
                    existing.implicitModifiers = bp.modifiers;
                    EditorUtility.SetDirty(existing);
                    assets.Add(existing);
                    continue;
                }

                var so = ScriptableObject.CreateInstance<ItemDefinitionSO>();
                so.id = bp.id;
                so.itemName = bp.name;
                so.rarity = bp.rarity;
                so.slot = bp.slot;
                so.handedness = bp.handedness;
                so.iconAddress = bp.iconAddress;
                so.implicitModifiers = bp.modifiers;

                AssetDatabase.CreateAsset(so, path);
                assets.Add(so);
            }

            var dbExisting = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(DatabasePath);
            if (dbExisting == null)
            {
                var db = ScriptableObject.CreateInstance<ItemDatabaseSO>();
                db.items = assets;
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            else
            {
                dbExisting.items = assets;
                EditorUtility.SetDirty(dbExisting);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ItemDatabaseCreator] Created/updated {assets.Count} items + database at {DatabasePath}");
        }

        private static List<ItemBlueprint> GetItemBlueprints()
        {
            return new List<ItemBlueprint>
            {
                new("rusty_sword", "Rusty Sword", Rarity.Normal, EquipmentSlotType.MainHand,
                    Handedness.Versatile, "Icons/Items/rusty_sword",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.PhysicalDamage, type = ModifierType.Flat, value = 5f }
                    }),

                new("iron_sword", "Iron Sword", Rarity.Magic, EquipmentSlotType.MainHand,
                    Handedness.Versatile, "Icons/Items/iron_sword",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.PhysicalDamage, type = ModifierType.Flat, value = 10f }
                    }),

                new("great_axe", "Great Axe", Rarity.Rare, EquipmentSlotType.MainHand,
                    Handedness.TwoHanded, "Icons/Items/great_axe",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.PhysicalDamage, type = ModifierType.Flat, value = 18f }
                    }),

                new("leather_vest", "Leather Vest", Rarity.Normal, EquipmentSlotType.BodyArmor,
                    Handedness.None, "Icons/Items/leather_vest",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.Armor, type = ModifierType.Flat, value = 8f },
                        new() { stat = StatType.MaxHealth, type = ModifierType.Flat, value = 15f }
                    }),

                new("iron_helmet", "Iron Helmet", Rarity.Normal, EquipmentSlotType.Helmet,
                    Handedness.None, "Icons/Items/iron_helmet",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.Armor, type = ModifierType.Flat, value = 5f }
                    }),

                new("worn_gloves", "Worn Gloves", Rarity.Normal, EquipmentSlotType.Gloves,
                    Handedness.None, "Icons/Items/worn_gloves",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.AttackSpeed, type = ModifierType.Increased, value = 0.05f }
                    }),

                new("simple_boots", "Simple Boots", Rarity.Normal, EquipmentSlotType.Boots,
                    Handedness.None, "Icons/Items/simple_boots",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.MovementSpeed, type = ModifierType.Flat, value = 1f }
                    }),

                new("jade_amulet", "Jade Amulet", Rarity.Magic, EquipmentSlotType.Amulet,
                    Handedness.None, "Icons/Items/jade_amulet",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.MaxHealth, type = ModifierType.Flat, value = 20f }
                    }),

                new("leather_belt", "Leather Belt", Rarity.Normal, EquipmentSlotType.Belt,
                    Handedness.None, "Icons/Items/leather_belt",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.MaxHealth, type = ModifierType.Flat, value = 10f }
                    }),

                new("iron_ring", "Iron Ring", Rarity.Normal, EquipmentSlotType.Ring,
                    Handedness.None, "Icons/Items/iron_ring",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.PhysicalDamage, type = ModifierType.Flat, value = 2f }
                    }),

                new("wooden_shield", "Wooden Shield", Rarity.Normal, EquipmentSlotType.OffHand,
                    Handedness.OffHandOnly, "Icons/Items/wooden_shield",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.Armor, type = ModifierType.Flat, value = 12f }
                    }),
            };
        }

        private readonly struct ItemBlueprint
        {
            public readonly string id;
            public readonly string name;
            public readonly Rarity rarity;
            public readonly EquipmentSlotType slot;
            public readonly Handedness handedness;
            public readonly string iconAddress;
            public readonly List<ModifierEntry> modifiers;

            public ItemBlueprint(string id, string name, Rarity rarity,
                EquipmentSlotType slot, Handedness handedness,
                string iconAddress, List<ModifierEntry> modifiers)
            {
                this.id = id;
                this.name = name;
                this.rarity = rarity;
                this.slot = slot;
                this.handedness = handedness;
                this.iconAddress = iconAddress;
                this.modifiers = modifiers;
            }
        }
    }
}
