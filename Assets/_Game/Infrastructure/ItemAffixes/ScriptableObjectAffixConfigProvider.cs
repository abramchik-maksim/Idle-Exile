using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Infrastructure.ItemAffixes
{
    public sealed class ScriptableObjectAffixConfigProvider : IAffixConfigProvider
    {
        private readonly List<ItemAffixPoolEntry> _pool = new();
        private readonly Dictionary<(string modId, EquipmentSlotType slot), float> _slotWeight = new();
        private readonly HashSet<(string modId, EquipmentSlotType slot)> _allowedSlots = new();
        private readonly bool _hasSlotRules;

        public ScriptableObjectAffixConfigProvider(ItemAffixDatabaseSO database)
        {
            if (database?.poolRows == null) return;

            foreach (var r in database.poolRows)
            {
                if (string.IsNullOrEmpty(r.affixId) || string.IsNullOrEmpty(r.modId)) continue;
                _pool.Add(new ItemAffixPoolEntry(
                    r.affixId,
                    r.modId,
                    r.classSpecific ?? string.Empty,
                    r.tier,
                    r.weight,
                    r.min,
                    r.max,
                    r.valueFormat ?? string.Empty,
                    r.templateId ?? string.Empty,
                    r.progressBand ?? string.Empty));
            }

            if (database.slotRows == null || database.slotRows.Count == 0)
            {
                _hasSlotRules = false;
                return;
            }

            _hasSlotRules = true;
            foreach (var r in database.slotRows)
            {
                if (!r.enabled || string.IsNullOrEmpty(r.modId) || string.IsNullOrEmpty(r.slotId)) continue;
                if (!TryParseSlotId(r.slotId, out var slot)) continue;
                var key = (r.modId, slot);
                _allowedSlots.Add(key);
                _slotWeight[key] = r.weightMultiplier <= 0f ? 1f : r.weightMultiplier;
            }
        }

        public IReadOnlyList<ItemAffixPoolEntry> PoolEntries => _pool;

        public bool IsModAllowedOnSlot(string modId, EquipmentSlotType slot)
        {
            if (!_hasSlotRules) return true;
            return _allowedSlots.Contains((modId, slot));
        }

        public float GetSlotWeightMultiplier(string modId, EquipmentSlotType slot)
        {
            if (!_hasSlotRules) return 1f;
            if (_slotWeight.TryGetValue((modId, slot), out float w)) return w;
            return IsModAllowedOnSlot(modId, slot) ? 1f : 0f;
        }

        private static bool TryParseSlotId(string slotId, out EquipmentSlotType slot) =>
            Enum.TryParse(slotId, true, out slot);
    }
}
