using System.Collections.Generic;
using Game.Domain.Stats;

namespace Game.Domain.DTOs.Stats
{
    public readonly struct HeroStatsChangedDTO
    {
        public IReadOnlyDictionary<StatType, float> FinalStats { get; }
        public HeroStatsChangedDTO(IReadOnlyDictionary<StatType, float> finalStats)
        {
            FinalStats = finalStats;
        }
    }
}
