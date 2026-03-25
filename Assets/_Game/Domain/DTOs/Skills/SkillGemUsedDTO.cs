namespace Game.Domain.DTOs.Skills
{
    public readonly struct SkillGemUsedDTO
    {
        public string GemDefinitionId { get; }
        public int RemainingCount { get; }

        public SkillGemUsedDTO(string gemDefinitionId, int remainingCount)
        {
            GemDefinitionId = gemDefinitionId;
            RemainingCount = remainingCount;
        }
    }
}
