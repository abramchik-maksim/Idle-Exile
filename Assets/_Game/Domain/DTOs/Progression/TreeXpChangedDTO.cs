namespace Game.Domain.DTOs.Progression
{
    public readonly struct TreeXpChangedDTO
    {
        public int CurrentXp { get; }
        public int XpToNextLevel { get; }

        public TreeXpChangedDTO(int currentXp, int xpToNextLevel)
        {
            CurrentXp = currentXp;
            XpToNextLevel = xpToNextLevel;
        }
    }
}
