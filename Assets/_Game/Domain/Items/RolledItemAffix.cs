namespace Game.Domain.Items
{
    /// <summary>
    /// One rolled line from <see cref="ItemAffixPoolEntry"/> (resolved tier row).
    /// </summary>
    public readonly struct RolledItemAffix
    {
        public string AffixId { get; }
        public string ModId { get; }
        public int Tier { get; }
        public float RolledValue { get; }
        /// <summary>From pool row (FlatNumber, Percent, ChancePercent, …).</summary>
        public string ValueFormat { get; }

        public RolledItemAffix(string affixId, string modId, int tier, float rolledValue, string valueFormat)
        {
            AffixId = affixId;
            ModId = modId;
            Tier = tier;
            RolledValue = rolledValue;
            ValueFormat = valueFormat ?? string.Empty;
        }
    }
}
