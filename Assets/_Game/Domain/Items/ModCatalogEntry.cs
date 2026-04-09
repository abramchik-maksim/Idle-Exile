namespace Game.Domain.Items
{
    /// <summary>Display row from ModCatalog.csv (UI strings).</summary>
    public readonly struct ModCatalogEntry
    {
        public string ModId { get; }
        public string ValueType { get; }
        public string TextTemplate { get; }

        public ModCatalogEntry(string modId, string valueType, string textTemplate)
        {
            ModId = modId;
            ValueType = valueType ?? string.Empty;
            TextTemplate = textTemplate ?? string.Empty;
        }
    }
}
