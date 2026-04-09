namespace Game.Domain.Items
{
    /// <summary>
    /// One row from ResolvedItemAffixPool (single tier line for a mod).
    /// </summary>
    public readonly struct ItemAffixPoolEntry
    {
        public string AffixId { get; }
        public string ModId { get; }
        public string ClassSpecific { get; }
        public int Tier { get; }
        public int Weight { get; }
        public float Min { get; }
        public float Max { get; }
        public string ValueFormat { get; }
        public string TemplateId { get; }
        public string ProgressBand { get; }

        public ItemAffixPoolEntry(
            string affixId,
            string modId,
            string classSpecific,
            int tier,
            int weight,
            float min,
            float max,
            string valueFormat,
            string templateId,
            string progressBand)
        {
            AffixId = affixId;
            ModId = modId;
            ClassSpecific = classSpecific;
            Tier = tier;
            Weight = weight;
            Min = min;
            Max = max;
            ValueFormat = valueFormat;
            TemplateId = templateId;
            ProgressBand = progressBand;
        }
    }
}
