namespace Game.Domain.Combat.Progression
{
    public readonly struct MapModifier
    {
        public MapModifierType Type { get; }
        public float Value { get; }

        public MapModifier(MapModifierType type, float value)
        {
            Type = type;
            Value = value;
        }
    }

    public enum MapModifierType
    {
        None,
        EnemyFireResist,
        EnemyColdResist,
        EnemyLightningResist,
        EnemyPhysicalResist,
        EnemyDamageMultiplier,
        EnemyHealthMultiplier,
        EnemySpeedMultiplier
    }
}
