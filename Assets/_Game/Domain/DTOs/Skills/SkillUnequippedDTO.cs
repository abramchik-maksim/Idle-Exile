namespace Game.Domain.DTOs.Skills
{
    public readonly struct SkillUnequippedDTO
    {
        public int SlotIndex { get; }

        public SkillUnequippedDTO(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }
}
