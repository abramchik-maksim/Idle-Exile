using System.Collections.Generic;
using Game.Domain.Combat;

namespace Game.Domain.Skills.Crafting
{
    public sealed class SkillAffixSlots
    {
        public const int MaxSlots = 6;

        private readonly SkillAffix[] _slots = new SkillAffix[MaxSlots];

        public bool IsFull => FilledCount >= MaxSlots;

        public int FilledCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (_slots[i] != null) count++;
                }
                return count;
            }
        }

        public SkillAffix GetSlot(int index) => _slots[index];

        public bool TryAdd(SkillAffix affix)
        {
            if (IsFull) return false;
            if (HasDuplicate(affix.Definition.Type, affix.Definition.DamageType)) return false;

            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = affix;
                    return true;
                }
            }

            return false;
        }

        public SkillAffix Remove(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return null;

            var removed = _slots[slotIndex];
            _slots[slotIndex] = null;
            return removed;
        }

        public bool HasDuplicate(SkillAffixType type, DamageType damageType)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == null) continue;
                if (_slots[i].Definition.Type == type &&
                    _slots[i].Definition.DamageType == damageType)
                    return true;
            }

            return false;
        }

        public IReadOnlyList<SkillAffix> GetAll()
        {
            var list = new List<SkillAffix>();
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != null) list.Add(_slots[i]);
            }
            return list.AsReadOnly();
        }

        public bool IsSlotOccupied(int index) =>
            index >= 0 && index < MaxSlots && _slots[index] != null;
    }
}
