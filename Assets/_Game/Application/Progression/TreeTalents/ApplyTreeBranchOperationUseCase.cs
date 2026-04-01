using Game.Domain.Progression.TreeTalents;

namespace Game.Application.Progression.TreeTalents
{
    public sealed class ApplyTreeBranchOperationUseCase
    {
        private readonly TreeUnlockProfileService _unlockProfile;

        public ApplyTreeBranchOperationUseCase(TreeUnlockProfileService unlockProfile)
        {
            _unlockProfile = unlockProfile;
        }

        public ApplyTreeBranchOperationResult Execute(TreeTalentsState state, TreeBranchOperation operation)
        {
            if (state == null || operation == null)
                return new ApplyTreeBranchOperationResult(false, operation?.BranchId);

            bool success;
            if (operation.OperationType == TreeBranchOperationType.Place)
            {
                var halfWidths = _unlockProfile.GetHalfWidthsForLevel(state.Level);
                success = state.TryPlaceBranch(
                    operation.BranchId,
                    new GridCoord(operation.AnchorX, operation.AnchorY),
                    halfWidths,
                    operation.RotationQuarterTurns);
            }
            else
            {
                success = state.TryRemovePlacedBranch(operation.BranchId);
            }

            return new ApplyTreeBranchOperationResult(success, operation.BranchId);
        }
    }

    public enum TreeBranchOperationType
    {
        Place = 0,
        Remove = 1,
    }

    public sealed class TreeBranchOperation
    {
        public TreeBranchOperationType OperationType { get; }
        public string BranchId { get; }
        public int AnchorX { get; }
        public int AnchorY { get; }
        public int RotationQuarterTurns { get; }

        public TreeBranchOperation(
            TreeBranchOperationType operationType,
            string branchId,
            int anchorX = 0,
            int anchorY = 0,
            int rotationQuarterTurns = 0)
        {
            OperationType = operationType;
            BranchId = branchId;
            AnchorX = anchorX;
            AnchorY = anchorY;
            RotationQuarterTurns = rotationQuarterTurns;
        }
    }

    public sealed class ApplyTreeBranchOperationResult
    {
        public bool Success { get; }
        public string BranchId { get; }

        public ApplyTreeBranchOperationResult(bool success, string branchId)
        {
            Success = success;
            BranchId = branchId;
        }
    }
}
