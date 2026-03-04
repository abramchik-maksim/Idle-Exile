namespace Game.Domain.Combat
{
    public sealed class SpellDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public float CastDuration { get; }
        public float DamageMultiplier { get; }
        public float AoERadius { get; }
        public float DetonationDelay { get; }
        public DamageType DamageType { get; }

        public SpellDefinition(
            string id, string name,
            float castDuration, float damageMultiplier,
            float aoERadius, float detonationDelay,
            DamageType damageType)
        {
            Id = id;
            Name = name;
            CastDuration = castDuration;
            DamageMultiplier = damageMultiplier;
            AoERadius = aoERadius;
            DetonationDelay = detonationDelay;
            DamageType = damageType;
        }
    }
}
