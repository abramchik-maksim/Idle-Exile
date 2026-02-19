namespace Game.Domain.Stats
{
    public readonly struct Modifier
    {
        public StatType Stat { get; }
        public ModifierType Type { get; }
        public float Value { get; }
        public string Source { get; }

        public Modifier(StatType stat, ModifierType type, float value, string source = "")
        {
            Stat = stat;
            Type = type;
            Value = value;
            Source = source;
        }
    }
}
