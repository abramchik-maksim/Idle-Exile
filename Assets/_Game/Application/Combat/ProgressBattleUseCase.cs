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
                progress.CurrentMap = nextMap;
                progress.CurrentBattle = 0;
                var battle = _config.GetBattle(progress.CurrentTier, nextMap, 0);
                return new ProgressionResult(battle, true, false);
            }

            int totalTiers = _config.GetTierCount();
            int nextTier = progress.CurrentTier + 1;

            if (nextTier < totalTiers)
            {
                progress.CurrentTier = nextTier;
                progress.CurrentMap = 0;
                progress.CurrentBattle = 0;
                var battle = _config.GetBattle(nextTier, 0, 0);
                return new ProgressionResult(battle, true, true);
            }

            // Max tier reached, repeat last battle
            var lastBattle = _config.GetBattle(progress.CurrentTier, progress.CurrentMap, progress.CurrentBattle);
            return new ProgressionResult(lastBattle, false, false);
        }
    }

    public sealed class ProgressionResult
    {
        public BattleDefinition NextBattle { get; }
        public bool MapChanged { get; }
        public bool TierChanged { get; }

        public ProgressionResult(BattleDefinition nextBattle, bool mapChanged, bool tierChanged)
        {
            NextBattle = nextBattle;
            MapChanged = mapChanged;
            TierChanged = tierChanged;
        }
    }
}
