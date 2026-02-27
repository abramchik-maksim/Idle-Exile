namespace Game.Domain.Skills
{
    public sealed class SkillLoadout
    {
        public const int MainSlotIndex = 0;
        public const int FirstUtilitySlotIndex = 1;
        public const int TotalSlots = 5;

        private readonly SkillInstance[] _slots = new SkillInstance[TotalSlots];

        public SkillInstance GetSlot(int index)
        {
            if (index < 0 || index >= TotalSlots) return null;
            return _slots[index];
        }

        public SkillInstance MainSkill => _slots[MainSlotIndex];

        public bool TryEquipMain(SkillInstance skill, out SkillInstance previous)
        {
            previous = null;
            if (skill == null || skill.Definition.Category != SkillCategory.Main)
                return false;

            previous = _slots[MainSlotIndex];
            _slots[MainSlotIndex] = skill;
            return true;
        }

        public bool TryEquipUtility(SkillInstance skill, int slotIndex, out SkillInstance previous)
        {
            previous = null;
            if (skill == null || skill.Definition.Category != SkillCategory.Utility)
                return false;

            if (slotIndex < FirstUtilitySlotIndex || slotIndex >= TotalSlots)
                return false;

            previous = _slots[slotIndex];
            _slots[slotIndex] = skill;
            return true;
        }

        public bool TryUnequip(int slotIndex, out SkillInstance unequipped)
        {
            unequipped = null;
            if (slotIndex < 0 || slotIndex >= TotalSlots) return false;
            if (_slots[slotIndex] == null) return false;

            unequipped = _slots[slotIndex];
            _slots[slotIndex] = null;
            return true;
        }

        public bool IsSlotEmpty(int index)
        {
            if (index < 0 || index >= TotalSlots) return true;
            return _slots[index] == null;
        }

        public bool IsMainSlot(int index) => index == MainSlotIndex;

        public void Clear()
        {
            for (int i = 0; i < TotalSlots; i++)
                _slots[i] = null;
        }
    }
}
