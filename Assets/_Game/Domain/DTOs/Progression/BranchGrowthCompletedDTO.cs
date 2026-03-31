namespace Game.Domain.DTOs.Progression
{
    public readonly struct BranchGrowthCompletedDTO
    {
        public string BranchId { get; }

        public BranchGrowthCompletedDTO(string branchId)
        {
            BranchId = branchId;
        }
    }
}
