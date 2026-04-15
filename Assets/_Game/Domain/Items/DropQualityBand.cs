namespace Game.Domain.Items
{
    /// <summary>
    /// One row from DropQualityProgression: controls rarity weights and affix tier window
    /// for a given progression stage range.
    /// </summary>
    public readonly struct DropQualityBand
    {
        public int BandId { get; }
        public int StageMin { get; }
        public int StageMax { get; }
        public int AllowedTierMin { get; }
        public int AllowedTierMax { get; }
        public float TierBias { get; }
        public float QualityMultiplier { get; }
        public int WeightNormal { get; }
        public int WeightMagic { get; }
        public int WeightRare { get; }
        public int WeightMythic { get; }

        public DropQualityBand(
            int bandId, int stageMin, int stageMax,
            int allowedTierMin, int allowedTierMax,
            float tierBias, float qualityMultiplier,
            int weightNormal, int weightMagic, int weightRare, int weightMythic)
        {
            BandId = bandId;
            StageMin = stageMin;
            StageMax = stageMax;
            AllowedTierMin = allowedTierMin;
            AllowedTierMax = allowedTierMax;
            TierBias = tierBias;
            QualityMultiplier = qualityMultiplier;
            WeightNormal = weightNormal;
            WeightMagic = weightMagic;
            WeightRare = weightRare;
            WeightMythic = weightMythic;
        }

        public int TotalRarityWeight => WeightNormal + WeightMagic + WeightRare + WeightMythic;
    }
}
