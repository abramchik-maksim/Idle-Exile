using System.Collections.Generic;
using Game.Domain.Combat.Progression;

namespace Game.Domain.DTOs.Combat
{
    public readonly struct BattleCompletedDTO
    {
        public int TierIndex { get; }
        public int MapIndex { get; }
        public int BattleIndex { get; }
        public IReadOnlyList<RewardEntry> Rewards { get; }

        public BattleCompletedDTO(int tierIndex, int mapIndex, int battleIndex, IReadOnlyList<RewardEntry> rewards)
        {
            TierIndex = tierIndex;
            MapIndex = mapIndex;
            BattleIndex = battleIndex;
            Rewards = rewards;
        }
    }
}
