namespace Game.Domain.DTOs.Progression
{
    public readonly struct TreeLevelChangedDTO
    {
        public int Level { get; }

        public TreeLevelChangedDTO(int level)
        {
            Level = level;
        }
    }
}
