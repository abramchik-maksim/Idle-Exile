namespace Game.Domain.Combat
{
    public readonly struct DamageBreakdown
    {
        public float PhysicalDamage { get; }
        public float FireDamage { get; }
        public float ColdDamage { get; }
        public float LightningDamage { get; }
        public float TotalRaw { get; }
        public float TotalMitigated { get; }
        public bool IsCritical { get; }

        public DamageBreakdown(
            float physicalDamage,
            float fireDamage,
            float coldDamage,
            float lightningDamage,
            float totalMitigated,
            bool isCritical)
        {
            PhysicalDamage = physicalDamage;
            FireDamage = fireDamage;
            ColdDamage = coldDamage;
            LightningDamage = lightningDamage;
            TotalRaw = physicalDamage + fireDamage + coldDamage + lightningDamage;
            TotalMitigated = totalMitigated;
            IsCritical = isCritical;
        }
    }
}
