namespace Game.Domain.Combat.Progression
{
    /// <summary>
    /// Per-map multipliers that shift reward distribution toward items, currency, etc.
    /// </summary>
    public readonly struct LootBias
    {
        public float ItemWeightMultiplier { get; }
        public float CurrencyWeightMultiplier { get; }

        public LootBias(float itemWeightMultiplier, float currencyWeightMultiplier)
        {
            ItemWeightMultiplier = itemWeightMultiplier;
            CurrencyWeightMultiplier = currencyWeightMultiplier;
        }

        public static LootBias Default => new(1f, 1f);
    }
}
