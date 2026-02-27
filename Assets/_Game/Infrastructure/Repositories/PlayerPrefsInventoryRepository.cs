using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;
using UnityEngine;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Infrastructure.Repositories
{
    public sealed class PlayerPrefsInventoryRepository : IInventoryRepository
    {
        private const string Key = "player_inventory";
        private readonly IConfigProvider _configProvider;

        public PlayerPrefsInventoryRepository(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public void Save(InventoryModel inventory)
        {
            var data = new InventorySaveData
            {
                capacity = inventory.Capacity,
                items = SerializeItems(inventory.Items),
                equipped = SerializeEquipped(inventory.Equipped)
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public InventoryModel Load()
        {
            if (!PlayerPrefs.HasKey(Key))
                return new InventoryModel();

            try
            {
                var data = JsonUtility.FromJson<InventorySaveData>(PlayerPrefs.GetString(Key));
                return Deserialize(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InventoryRepo] Failed to load inventory: {ex.Message}. Creating empty.");
                return new InventoryModel();
            }
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        private InventoryModel Deserialize(InventorySaveData data)
        {
            var items = new List<ItemInstance>();
            var equipped = new Dictionary<EquipmentSlotType, ItemInstance>();

            if (data.items != null)
            {
                foreach (var si in data.items)
                {
                    var instance = DeserializeItem(si);
                    if (instance != null)
                        items.Add(instance);
                }
            }

            if (data.equipped != null)
            {
                foreach (var se in data.equipped)
                {
                    var instance = DeserializeItem(se.item);
                    if (instance != null)
                        equipped[(EquipmentSlotType)se.slot] = instance;
                }
            }

            int capacity = data.capacity > 0 ? data.capacity : 32;
            return new InventoryModel(capacity, items, equipped);
        }

        private ItemInstance DeserializeItem(ItemSaveData si)
        {
            var def = _configProvider.GetItemDefinition(si.defId);
            if (def == null)
            {
                Debug.LogWarning($"[InventoryRepo] Item definition '{si.defId}' not found, skipping.");
                return null;
            }

            var mods = new List<Modifier>();
            if (si.mods != null)
            {
                foreach (var sm in si.mods)
                    mods.Add(new Modifier((StatType)sm.stat, (ModifierType)sm.type, sm.value, sm.source));
            }

            return new ItemInstance(si.uid, def, mods);
        }

        private static ItemSaveData[] SerializeItems(IReadOnlyList<ItemInstance> items)
        {
            var arr = new ItemSaveData[items.Count];
            for (int i = 0; i < items.Count; i++)
                arr[i] = SerializeItem(items[i]);
            return arr;
        }

        private static EquippedSaveData[] SerializeEquipped(
            IReadOnlyDictionary<EquipmentSlotType, ItemInstance> equipped)
        {
            var list = new List<EquippedSaveData>();
            foreach (var kvp in equipped)
            {
                list.Add(new EquippedSaveData
                {
                    slot = (int)kvp.Key,
                    item = SerializeItem(kvp.Value)
                });
            }
            return list.ToArray();
        }

        private static ItemSaveData SerializeItem(ItemInstance item)
        {
            var mods = new ModSaveData[item.RolledModifiers.Count];
            for (int i = 0; i < item.RolledModifiers.Count; i++)
            {
                var m = item.RolledModifiers[i];
                mods[i] = new ModSaveData
                {
                    stat = (int)m.Stat,
                    type = (int)m.Type,
                    value = m.Value,
                    source = m.Source
                };
            }

            return new ItemSaveData
            {
                uid = item.Uid,
                defId = item.Definition.Id,
                mods = mods
            };
        }

        [Serializable]
        private class InventorySaveData
        {
            public int capacity;
            public ItemSaveData[] items;
            public EquippedSaveData[] equipped;
        }

        [Serializable]
        private struct ItemSaveData
        {
            public string uid;
            public string defId;
            public ModSaveData[] mods;
        }

        [Serializable]
        private struct EquippedSaveData
        {
            public int slot;
            public ItemSaveData item;
        }

        [Serializable]
        private struct ModSaveData
        {
            public int stat;
            public int type;
            public float value;
            public string source;
        }
    }
}
