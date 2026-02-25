namespace Game.Domain.DTOs.Combat
{
    public readonly struct BattleStartedDTO
    {
        public int TierIndex { get; }
        public int MapIndex { get; }
        public int BattleIndex { get; }
        public int TotalBattles { get; }
        public string TierName { get; }

        public BattleStartedDTO(int tierIndex, int mapIndex, int battleIndex, int totalBattles, string tierName)
        {
            TierIndex = tierIndex;
            MapIndex = mapIndex;
            BattleIndex = battleIndex;
            TotalBattles = totalBattles;
            TierName = tierName;
        }
    }
}
