namespace Game.Domain.Stats
{
    public static class ModifierRollingConfig
    {
        public static readonly StatType[] RollableStats =
        {
            StatType.MaxHealth,
            StatType.PhysicalDamage,
            StatType.Armor,
            StatType.AttackSpeed,
            StatType.CriticalChance,
            StatType.Evasion,
            StatType.MovementSpeed,
        };

        public static (ModifierType type, float min, float max) GetRange(StatType stat) => stat switch
        {
            StatType.MaxHealth          => (ModifierType.Flat,      5f,    50f),
            StatType.PhysicalDamage     => (ModifierType.Flat,      1f,    10f),
            StatType.Armor              => (ModifierType.Flat,      1f,    10f),
            StatType.AttackSpeed        => (ModifierType.Increased, 0.05f, 0.25f),
            StatType.CriticalChance     => (ModifierType.Increased, 0.30f, 1.00f),
            StatType.CriticalMultiplier => (ModifierType.Increased, 0.10f, 0.50f),
            StatType.Evasion            => (ModifierType.Flat,      1f,    8f),
            StatType.MovementSpeed      => (ModifierType.Flat,      0.2f,  1.0f),
            StatType.HealthRegen        => (ModifierType.Flat,      0.5f,  3f),
            _                           => (ModifierType.Flat,      1f,    10f),
        };
    }
}
