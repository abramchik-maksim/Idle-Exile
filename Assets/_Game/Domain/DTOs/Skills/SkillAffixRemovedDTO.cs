namespace Game.Domain.DTOs.Skills
{
    public readonly struct SkillAffixRemovedDTO
    {
        public string SkillUid { get; }
        public int SlotIndex { get; }

        public SkillAffixRemovedDTO(string skillUid, int slotIndex)
        {
            SkillUid = skillUid;
            SlotIndex = slotIndex;
        }
    }
}
