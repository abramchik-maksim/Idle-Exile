namespace Game.Domain.Combat
{
    public readonly struct GainAsElementData
    {
        public float GainAsFirePercent { get; }
        public float GainAsColdPercent { get; }
        public float GainAsLightningPercent { get; }

        public GainAsElementData(
            float gainAsFirePercent = 0f,
            float gainAsColdPercent = 0f,
            float gainAsLightningPercent = 0f)
        {
            GainAsFirePercent = gainAsFirePercent;
            GainAsColdPercent = gainAsColdPercent;
            GainAsLightningPercent = gainAsLightningPercent;
        }

        public static readonly GainAsElementData None = new();
    }
}
