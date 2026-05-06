namespace Game.Domain.Items
{
    /// <summary>Row from ModCatalog.csv (UI strings + metadata for rolling).</summary>
    public readonly struct ModCatalogEntry
    {
        public string ModId { get; }
        public string Family { get; }
        public ModCatalogElement Element { get; }
        public string ValueType { get; }
        public string TextTemplate { get; }

        public ModCatalogEntry(
            string modId,
            string family,
            ModCatalogElement element,
            string valueType,
            string textTemplate)
        {
            ModId = modId;
            Family = family ?? string.Empty;
            Element = element;
            ValueType = valueType ?? string.Empty;
            TextTemplate = textTemplate ?? string.Empty;
        }
    }
}
