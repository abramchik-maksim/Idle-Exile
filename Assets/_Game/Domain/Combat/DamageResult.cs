namespace Game.Domain.Combat
{
    public readonly struct DamageResult
    {
        public float RawDamage { get; }
        public float MitigatedDamage { get; }
        public bool IsCritical { get; }
        public DamageType Type { get; }

        public DamageResult(float rawDamage, float mitigatedDamage, bool isCritical, DamageType type)
        {
            RawDamage = rawDamage;
            MitigatedDamage = mitigatedDamage;
            IsCritical = isCritical;
            Type = type;
        }
    }
}
