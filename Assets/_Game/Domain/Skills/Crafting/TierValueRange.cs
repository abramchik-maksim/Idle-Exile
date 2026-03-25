namespace Game.Domain.Skills.Crafting
{
    public readonly struct TierValueRange
    {
        public int Tier { get; }
        public float MinValue1 { get; }
        public float MaxValue1 { get; }
        public float MinValue2 { get; }
        public float MaxValue2 { get; }

        public TierValueRange(int tier, float minValue1, float maxValue1,
            float minValue2 = 0f, float maxValue2 = 0f)
        {
            Tier = tier;
            MinValue1 = minValue1;
            MaxValue1 = maxValue1;
            MinValue2 = minValue2;
            MaxValue2 = maxValue2;
        }
    }
}
