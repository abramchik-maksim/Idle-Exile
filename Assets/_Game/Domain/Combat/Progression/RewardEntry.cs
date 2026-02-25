namespace Game.Domain.Combat.Progression
{
    public readonly struct RewardEntry
    {
        public RewardType Type { get; }
        public string RewardId { get; }
        public int Amount { get; }

        public RewardEntry(RewardType type, string rewardId, int amount)
        {
            Type = type;
            RewardId = rewardId;
            Amount = amount;
        }
    }
}
