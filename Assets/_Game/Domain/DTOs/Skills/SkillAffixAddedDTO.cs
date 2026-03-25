using Game.Domain.Skills.Crafting;

namespace Game.Domain.DTOs.Skills
{
    public readonly struct SkillAffixAddedDTO
    {
        public string SkillUid { get; }
        public SkillAffix Affix { get; }
        public int SlotIndex { get; }

        public SkillAffixAddedDTO(string skillUid, SkillAffix affix, int slotIndex)
        {
            SkillUid = skillUid;
            Affix = affix;
            SlotIndex = slotIndex;
        }
    }
}
