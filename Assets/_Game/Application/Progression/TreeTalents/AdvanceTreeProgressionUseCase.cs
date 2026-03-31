using Game.Domain.Progression.TreeTalents;

namespace Game.Application.Progression.TreeTalents
{
    public sealed class AdvanceTreeProgressionUseCase
    {
        public AdvanceTreeProgressionResult Execute(TreeTalentsState treeState, int gainedXp)
        {
            if (treeState == null || gainedXp <= 0)
                return new AdvanceTreeProgressionResult(false, false, 0, 0, 0);

            var leveledUp = treeState.GainXp(gainedXp);
            return new AdvanceTreeProgressionResult(
                true,
                leveledUp,
                treeState.Level,
                treeState.CurrentXp,
                treeState.XpToNextLevel);
        }
    }

    public sealed class AdvanceTreeProgressionResult
    {
        public bool Success { get; }
        public bool LeveledUp { get; }
        public int Level { get; }
        public int CurrentXp { get; }
        public int XpToNextLevel { get; }

        public AdvanceTreeProgressionResult(bool success, bool leveledUp, int level, int currentXp, int xpToNextLevel)
        {
            Success = success;
            LeveledUp = leveledUp;
            Level = level;
            CurrentXp = currentXp;
            XpToNextLevel = xpToNextLevel;
        }
    }
}
