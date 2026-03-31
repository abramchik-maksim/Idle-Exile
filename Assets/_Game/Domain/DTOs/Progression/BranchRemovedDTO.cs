namespace Game.Domain.DTOs.Progression
{
    public readonly struct BranchRemovedDTO
    {
        public string BranchId { get; }

        public BranchRemovedDTO(string branchId)
        {
            BranchId = branchId;
        }
    }
}
