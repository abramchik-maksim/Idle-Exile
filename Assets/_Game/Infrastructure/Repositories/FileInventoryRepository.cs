using System;
using System.Collections.Generic;
using System.IO;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Stats;
using UnityEngine;
using InventoryModel = Game.Domain.Inventory.Inventory;

namespace Game.Infrastructure.Repositories
{
    public sealed class FileInventoryRepository : IInventoryRepository
    {
        private readonly IConfigProvider _configProvider;
        private readonly IItemAffixModifierResolver _affixResolver;
        private readonly ISaveSlotManager _slotManager;

        public FileInventoryRepository(
            IConfigProvider configProvider,
            IItemAffixModifierResolver affixResolver,
            ISaveSlotManager slotManager)
        {
            _configProvider = configProvider;
            _affixResolver = affixResolver;
            _slotManager = slotManager;
        }

        public void Save(InventoryModel inventory)
        {
            var data = new InventorySaveData
            {
                capacity = inventory.Capacity,
                items = SerializeItems(inventory.Items),
                equipped = SerializeEquipped(inventory.Equipped)
            };

            Directory.CreateDirectory(FileSavePaths.SlotDirectory(_slotManager.ActiveSlotIndex));
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FileSavePaths.InventoryPath(_slotManager.ActiveSlotIndex), json);
        }

        public InventoryModel Load()
        {
            var path = FileSavePaths.InventoryPath(_slotManager.ActiveSlotIndex);
            if (!File.Exists(path))
                return new InventoryModel();

            try
            {
                var data = JsonUtility.FromJson<InventorySaveData>(File.ReadAllText(path));
                return Deserialize(data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FileInventoryRepository] Failed to load inventory: {ex.Message}. Creating empty.");
                return new InventoryModel();
            }
        }

        public void Delete()
        {
            var path = FileSavePaths.InventoryPath(_slotManager.ActiveSlotIndex);
            if (File.Exists(path))
                File.Delete(path);
        }

        private InventoryModel Deserialize(InventorySaveData data)
        {
            if (data == null)
                return new InventoryModel();

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
                Debug.LogWarning($"[FileInventoryRepository] Item definition '{si.defId}' not found, skipping.");
                return null;
            }

            var affixes = new List<RolledItemAffix>();
            if (si.affixes != null && si.affixes.Length > 0)
            {
                foreach (var a in si.affixes)
                    affixes.Add(new RolledItemAffix(a.affixId, a.modId, a.tier, a.value, a.valueFormat));
            }

            var mods = new List<Modifier>();
            if (affixes.Count > 0)
            {
                foreach (var ax in affixes)
                {
                    foreach (var m in _affixResolver.ResolveModifiers(ax))
                        mods.Add(m);
                }
            }
            else if (si.mods != null)
            {
                foreach (var sm in si.mods)
                    mods.Add(new Modifier((StatType)sm.stat, (ModifierType)sm.type, sm.value, sm.source));
            }

            return new ItemInstance(si.uid, def, affixes, mods);
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
            var affixes = new RolledAffixSaveData[item.RolledAffixes.Count];
            for (int i = 0; i < item.RolledAffixes.Count; i++)
            {
                var a = item.RolledAffixes[i];
                affixes[i] = new RolledAffixSaveData
                {
                    affixId = a.AffixId,
                    modId = a.ModId,
                    tier = a.Tier,
                    value = a.RolledValue,
                    valueFormat = a.ValueFormat
                };
            }

            return new ItemSaveData
            {
                uid = item.Uid,
                defId = item.Definition.Id,
                affixes = affixes,
                mods = null
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
        private class ItemSaveData
        {
            public string uid;
            public string defId;
            public RolledAffixSaveData[] affixes;
            public ModSaveData[] mods;
        }

        [Serializable]
        private struct EquippedSaveData
        {
            public int slot;
            public ItemSaveData item;
        }

        [Serializable]
        private struct RolledAffixSaveData
        {
            public string affixId;
            public string modId;
            public int tier;
            public float value;
            public string valueFormat;
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
