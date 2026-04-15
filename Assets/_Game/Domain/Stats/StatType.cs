namespace Game.Domain.Stats
{
    public enum StatType
    {
        // Core
        MaxHealth,
        CurrentHealth,
        PhysicalDamage,
        AttackSpeed,
        CriticalChance,
        CriticalMultiplier,
        Armor,
        Evasion,
        MovementSpeed,
        HealthRegen,

        // Elemental damage
        FireDamage,
        ColdDamage,
        LightningDamage,
        CorrosionDamage,
        GlobalDamage,

        // Resistances
        FireResistance,
        ColdResistance,
        LightningResistance,
        CorrosionResistance,

        // Defense misc
        Barrier,
        BlockChance,
        LifeLeech,
        LifeLeechRate,
        ArmorAppliedToNonPhysical,

        // Ailment chances (0..1 fraction)
        AilmentChanceAll,
        IgniteChance,
        ChillChance,
        ShockChance,
        BleedChance,
        PoisonChance,

        // Ailment effect / duration
        AilmentEffectAll,
        IgniteEffect,
        ChillEffect,
        ShockEffect,
        BleedEffect,
        PoisonEffect,
        AilmentDuration,

        // Ailment misc
        ExtraChillStacks,
        ExtraShockStacks,
        FasterBleed,
        FasterIgnite,
        FasterCorrosion,
        AilmentSpreadArea,

        // Penetration
        FirePenetration,
        ColdPenetration,
        LightningPenetration,
        CorrosionPenetration,

        // Damage mechanics
        IgnoreArmorChance,
        DoubleHitChance,

        // Gain-as-element (from gear, fraction of phys)
        GainAsFirePercent,
        GainAsColdPercent,
        GainAsLightningPercent,
        GainAsPhysicalPercent,
        GainAsCorrosionPercent,

        // Class-specific increased damage
        MeleeIncreasedDamage,
        RangedIncreasedDamage,
        SpellIncreasedDamage,

        // Class-specific mechanics
        MeleeAreaOfEffect,
        SpellAreaOfEffect,
        RangedPierceChance,
        RangedChainChance,
        RangedForkChance,
        SpellPierceChance,
        SpellChainChance,
        SpellForkChance,

        // Utility
        BuffDuration,
        BuffEffect,
        CooldownRecoveryRate
    }
}
