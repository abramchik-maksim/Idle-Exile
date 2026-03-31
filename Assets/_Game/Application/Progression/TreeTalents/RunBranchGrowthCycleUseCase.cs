using System.Collections.Generic;
using Game.Domain.Progression.TreeTalents;

namespace Game.Application.Progression.TreeTalents
{
    public sealed class RunBranchGrowthCycleUseCase
    {
        private readonly BranchGenerationService _generator;

        public RunBranchGrowthCycleUseCase(BranchGenerationService generator)
        {
            _generator = generator;
        }

        public RunBranchGrowthCycleResult Execute(
            TreeTalentsState treeState,
            IReadOnlyList<SeedType> seeds,
            int generationLevel)
        {
            if (treeState == null || seeds == null || seeds.Count != 3)
                return new RunBranchGrowthCycleResult(false, null);

            var branch = _generator.GenerateBranch(seeds, generationLevel);
            treeState.AddGeneratedBranch(branch);
            return new RunBranchGrowthCycleResult(true, branch);
        }
    }

    public sealed class RunBranchGrowthCycleResult
    {
        public bool Success { get; }
        public BranchInstance Branch { get; }

        public RunBranchGrowthCycleResult(bool success, BranchInstance branch)
        {
            Success = success;
            Branch = branch;
        }
    }
}
