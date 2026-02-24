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
                    assets.Add(existing);
                    continue;
                }

                var so = ScriptableObject.CreateInstance<ItemDefinitionSO>();
                so.id = bp.id;
                so.itemName = bp.name;
                so.rarity = bp.rarity;
                so.slot = bp.slot;
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
            Debug.Log($"[ItemDatabaseCreator] Created {assets.Count} items + database at {DatabasePath}");
        }

        private static List<ItemBlueprint> GetItemBlueprints()
        {
            return new List<ItemBlueprint>
            {
                new("rusty_sword", "Rusty Sword", Rarity.Normal, EquipmentSlotType.Weapon,
                    "Icons/Items/rusty_sword",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.PhysicalDamage, type = ModifierType.Flat, value = 5f }
                    }),

                new("iron_sword", "Iron Sword", Rarity.Magic, EquipmentSlotType.Weapon,
                    "Icons/Items/iron_sword",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.PhysicalDamage, type = ModifierType.Flat, value = 10f }
                    }),

                new("leather_vest", "Leather Vest", Rarity.Normal, EquipmentSlotType.BodyArmor,
                    "Icons/Items/leather_vest",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.Armor, type = ModifierType.Flat, value = 8f },
                        new() { stat = StatType.MaxHealth, type = ModifierType.Flat, value = 15f }
                    }),

                new("iron_helmet", "Iron Helmet", Rarity.Normal, EquipmentSlotType.Helmet,
                    "Icons/Items/iron_helmet",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.Armor, type = ModifierType.Flat, value = 5f }
                    }),

                new("worn_gloves", "Worn Gloves", Rarity.Normal, EquipmentSlotType.Gloves,
                    "Icons/Items/worn_gloves",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.AttackSpeed, type = ModifierType.Increased, value = 0.05f }
                    }),

                new("simple_boots", "Simple Boots", Rarity.Normal, EquipmentSlotType.Boots,
                    "Icons/Items/simple_boots",
                    new List<ModifierEntry>
                    {
                        new() { stat = StatType.MovementSpeed, type = ModifierType.Flat, value = 1f }
                    }),
            };
        }

        private readonly struct ItemBlueprint
        {
            public readonly string id;
            public readonly string name;
            public readonly Rarity rarity;
            public readonly EquipmentSlotType slot;
            public readonly string iconAddress;
            public readonly List<ModifierEntry> modifiers;

            public ItemBlueprint(string id, string name, Rarity rarity,
                EquipmentSlotType slot, string iconAddress, List<ModifierEntry> modifiers)
            {
                this.id = id;
                this.name = name;
                this.rarity = rarity;
                this.slot = slot;
                this.iconAddress = iconAddress;
                this.modifiers = modifiers;
            }
        }
    }
}
