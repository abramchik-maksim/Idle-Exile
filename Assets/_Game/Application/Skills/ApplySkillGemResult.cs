using Game.Domain.Skills;
using Game.Domain.Skills.Crafting;

namespace Game.Application.Skills
{
    public sealed class ApplySkillGemResult
    {
        public bool IsSuccess { get; }
        public SkillAffix RolledAffix { get; }
        public int SlotIndex { get; }
        public int RemainingGemCount { get; }
        public ApplySkillGemFailReason FailReason { get; }

        private ApplySkillGemResult(
            bool success,
            SkillAffix affix,
            int slotIndex,
            int remainingGems,
            ApplySkillGemFailReason failReason)
        {
            IsSuccess = success;
            RolledAffix = affix;
            SlotIndex = slotIndex;
            RemainingGemCount = remainingGems;
            FailReason = failReason;
        }

        public static ApplySkillGemResult Success(SkillAffix affix, int slotIndex, int remainingGems) =>
            new(true, affix, slotIndex, remainingGems, ApplySkillGemFailReason.None);

        public static ApplySkillGemResult Fail(ApplySkillGemFailReason reason) =>
            new(false, null, -1, 0, reason);
    }
}

