using Game.Application.Ports;
using Game.Domain.Combat.Progression;

namespace Game.Application.Combat
{
    public sealed class ProgressBattleUseCase
    {
        private readonly ICombatConfigProvider _config;

        public ProgressBattleUseCase(ICombatConfigProvider config)
        {
            _config = config;
        }

        public ProgressionResult Execute(PlayerProgressData progress)
        {
            int totalBattles = _config.GetBattleCount(progress.CurrentTier, progress.CurrentMap);

            int nextBattle = progress.CurrentBattle + 1;

            if (nextBattle < totalBattles)
            {
                progress.CurrentBattle = nextBattle;
                var battle = _config.GetBattle(progress.CurrentTier, progress.CurrentMap, nextBattle);
                return new ProgressionResult(battle, false, false);
            }

            int totalMaps = _config.GetMapCount(progress.CurrentTier);
            int nextMap = progress.CurrentMap + 1;

            if (nextMap < totalMaps)
            {
                var tier = _config.GetTier(progress.CurrentTier);
                bool isForced = tier != null && tier.HasForcedStartMap && nextMap == 0;
                bool needsChoice = !isForced;

                return ProgressionResult.NeedMapAdvance(progress.CurrentTier, nextMap, needsChoice);
            }

            int totalTiers = _config.GetTierCount();
            int nextTier = progress.CurrentTier + 1;

            if (nextTier < totalTiers)
            {
                var nextTierDef = _config.GetTier(nextTier);
                bool hasForcedStart = nextTierDef != null && nextTierDef.HasForcedStartMap;

                if (hasForcedStart)
                {
                    progress.CurrentTier = nextTier;
                    progress.CurrentMap = 0;
                    progress.CurrentBattle = 0;
                    var battle = _config.GetBattle(nextTier, 0, 0);
                    return new ProgressionResult(battle, true, true);
                }

                return ProgressionResult.NeedTierAdvanceWithChoice(nextTier);
            }

            var lastBattle = _config.GetBattle(progress.CurrentTier, progress.CurrentMap, progress.CurrentBattle);
            return new ProgressionResult(lastBattle, false, false);
        }

        /// <summary>
        /// Called after the player picks a map from the choice UI.
        /// Sets the chosen map index on progress and returns the first battle.
        /// </summary>
        public ProgressionResult ApplyMapChoice(PlayerProgressData progress, int tierIndex, int chosenMapIndex)
        {
            progress.CurrentTier = tierIndex;
            progress.CurrentMap = chosenMapIndex;
            progress.CurrentBattle = 0;
            var battle = _config.GetBattle(tierIndex, chosenMapIndex, 0);
            return new ProgressionResult(battle, true, tierIndex != progress.CurrentTier);
        }
    }

    public sealed class ProgressionResult
    {
        public BattleDefinition NextBattle { get; }
        public bool MapChanged { get; }
        public bool TierChanged { get; }
        public bool NeedsMapChoice { get; }
        public int PendingTierIndex { get; }
        public int PendingMapIndex { get; }

        public ProgressionResult(BattleDefinition nextBattle, bool mapChanged, bool tierChanged)
        {
            NextBattle = nextBattle;
            MapChanged = mapChanged;
            TierChanged = tierChanged;
            NeedsMapChoice = false;
        }

        private ProgressionResult(int pendingTierIndex, int pendingMapIndex, bool tierChanged)
        {
            NeedsMapChoice = true;
            PendingTierIndex = pendingTierIndex;
            PendingMapIndex = pendingMapIndex;
            TierChanged = tierChanged;
        }

        public static ProgressionResult NeedMapAdvance(int tierIndex, int mapIndex, bool needsChoice)
        {
            if (!needsChoice)
                return new ProgressionResult(null, true, false);
            return new ProgressionResult(tierIndex, mapIndex, false);
        }

        public static ProgressionResult NeedTierAdvanceWithChoice(int nextTierIndex)
        {
            return new ProgressionResult(nextTierIndex, 0, true);
        }
    }
}
