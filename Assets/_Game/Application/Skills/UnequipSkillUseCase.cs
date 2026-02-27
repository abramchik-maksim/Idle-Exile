using Game.Domain.Skills;

namespace Game.Application.Skills
{
    public sealed class UnequipSkillUseCase
    {
        public UnequipSkillResult Execute(SkillLoadout loadout, int slotIndex)
        {
            if (!loadout.TryUnequip(slotIndex, out var unequipped))
                return UnequipSkillResult.Fail;

            return new UnequipSkillResult(true, slotIndex, unequipped);
        }
    }

    public sealed class UnequipSkillResult
    {
        public static readonly UnequipSkillResult Fail = new(false, -1, null);

        public bool Success { get; }
        public int SlotIndex { get; }
        public SkillInstance UnequippedSkill { get; }

        public UnequipSkillResult(bool success, int slotIndex, SkillInstance unequippedSkill)
        {
            Success = success;
            SlotIndex = slotIndex;
            UnequippedSkill = unequippedSkill;
        }
    }
}
