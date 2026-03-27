using Game.Application.Ports;
using Game.Domain.Skills;
using Game.Domain.Skills.Crafting;

namespace Game.Application.Skills
{
    public sealed class ApplySkillGemUseCase
    {
        private const int MaxRerollAttempts = 10;

        private readonly ISkillGemConfigProvider _gemConfig;
        private readonly SkillAffixRollingService _rollingService;
        private readonly SkillGemInventory _gemInventory;

        public ApplySkillGemUseCase(
            ISkillGemConfigProvider gemConfig,
            SkillAffixRollingService rollingService,
            SkillGemInventory gemInventory)
        {
            _gemConfig = gemConfig;
            _rollingService = rollingService;
            _gemInventory = gemInventory;
        }

        public ApplySkillGemResult Execute(SkillInstance skill, string gemDefinitionId)
        {
            if (skill.Definition.Category != SkillCategory.Main)
                return ApplySkillGemResult.Fail(ApplySkillGemFailReason.NotMainSkill);

            if (skill.Affixes.IsFull)
                return ApplySkillGemResult.Fail(ApplySkillGemFailReason.SlotsFull);

            var gemDef = _gemConfig.GetGemDefinition(gemDefinitionId);
            if (gemDef == null)
                return ApplySkillGemResult.Fail(ApplySkillGemFailReason.GemNotFound);

            if (!_gemInventory.TryConsume(gemDefinitionId))
                return ApplySkillGemResult.Fail(ApplySkillGemFailReason.NoGemsAvailable);

            for (int attempt = 0; attempt < MaxRerollAttempts; attempt++)
            {
                var affix = _rollingService.Roll(gemDef);
                if (affix == null) continue;

                if (skill.Affixes.TryAdd(affix))
                {
                    int slotIndex = FindAffixSlot(skill, affix);
                    return ApplySkillGemResult.Success(affix, slotIndex, _gemInventory.GetCount(gemDefinitionId));
                }
            }

            return ApplySkillGemResult.Fail(ApplySkillGemFailReason.AllAffixesDuplicated);
        }

        private static int FindAffixSlot(SkillInstance skill, SkillAffix affix)
        {
            for (int i = 0; i < SkillAffixSlots.MaxSlots; i++)
            {
                if (skill.Affixes.GetSlot(i) == affix)
                    return i;
            }
            return -1;
        }
    }
}
