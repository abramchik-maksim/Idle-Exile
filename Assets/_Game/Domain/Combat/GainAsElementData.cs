namespace Game.Domain.Combat
{
    public readonly struct GainAsElementData
    {
        public float GainAsFirePercent { get; }
        public float GainAsColdPercent { get; }
        public float GainAsLightningPercent { get; }
        public float GainAsPhysicalPercent { get; }
        public float GainAsCorrosionPercent { get; }

        public GainAsElementData(
            float gainAsFirePercent = 0f,
            float gainAsColdPercent = 0f,
            float gainAsLightningPercent = 0f,
            float gainAsPhysicalPercent = 0f,
            float gainAsCorrosionPercent = 0f)
        {
            GainAsFirePercent = gainAsFirePercent;
            GainAsColdPercent = gainAsColdPercent;
            GainAsLightningPercent = gainAsLightningPercent;
            GainAsPhysicalPercent = gainAsPhysicalPercent;
            GainAsCorrosionPercent = gainAsCorrosionPercent;
        }

        public static readonly GainAsElementData None = new();
    }
}
