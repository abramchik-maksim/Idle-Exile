using Game.Domain.Skills;
using Game.Domain.Skills.Crafting;

namespace Game.Application.Skills
{
    public sealed class RemoveSkillAffixUseCase
    {
        private readonly SkillGemInventory _gemInventory;

        public RemoveSkillAffixUseCase(SkillGemInventory gemInventory)
        {
            _gemInventory = gemInventory;
        }

        public RemoveSkillAffixResult Execute(SkillInstance skill, int slotIndex)
        {
            if (!skill.Affixes.IsSlotOccupied(slotIndex))
                return RemoveSkillAffixResult.Fail(RemoveAffixFailReason.SlotEmpty);

            if (!_gemInventory.TryConsumeRemovalCurrency())
                return RemoveSkillAffixResult.Fail(RemoveAffixFailReason.NoRemovalCurrency);

            var removed = skill.Affixes.Remove(slotIndex);
            return RemoveSkillAffixResult.Success(removed, slotIndex);
        }
    }

    public sealed class RemoveSkillAffixResult
    {
        public bool IsSuccess { get; }
        public SkillAffix RemovedAffix { get; }
        public int SlotIndex { get; }
        public RemoveAffixFailReason FailReason { get; }

        private RemoveSkillAffixResult(bool success, SkillAffix removed, int slotIndex,
            RemoveAffixFailReason failReason)
        {
            IsSuccess = success;
            RemovedAffix = removed;
            SlotIndex = slotIndex;
            FailReason = failReason;
        }

        public static RemoveSkillAffixResult Success(SkillAffix removed, int slotIndex) =>
            new(true, removed, slotIndex, RemoveAffixFailReason.None);

        public static RemoveSkillAffixResult Fail(RemoveAffixFailReason reason) =>
            new(false, null, -1, reason);
    }

    public enum RemoveAffixFailReason
    {
        None,
        SlotEmpty,
        NoRemovalCurrency
    }
}
