namespace Game.Domain.DTOs.Progression
{
    public readonly struct BranchPlacedDTO
    {
        public string BranchId { get; }

        public BranchPlacedDTO(string branchId)
        {
            BranchId = branchId;
        }
    }
}
