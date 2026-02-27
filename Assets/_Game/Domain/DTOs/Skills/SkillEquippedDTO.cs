namespace Game.Domain.DTOs.Skills
{
    public readonly struct SkillEquippedDTO
    {
        public string SkillUid { get; }
        public int SlotIndex { get; }

        public SkillEquippedDTO(string skillUid, int slotIndex)
        {
            SkillUid = skillUid;
            SlotIndex = slotIndex;
        }
    }
}
