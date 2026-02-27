using System.Collections.Generic;

namespace Game.Domain.Skills
{
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public SkillCategory Category { get; }
        public UtilitySubCategory SubCategory { get; }
        public string IconAddress { get; }

        // Main skill fields
        public WeaponType RequiredWeapon { get; }
        public float DamageMultiplierPercent { get; }
        public float AttackSpeedMultiplierPercent { get; }
        public IReadOnlyList<SkillEffectType> Effects { get; }

        // Utility skill fields
        public float Cooldown { get; }
        public SkillEffectType EffectType { get; }
        public float EffectValue { get; }

        public SkillDefinition(
            string id,
            string name,
            SkillCategory category,
            UtilitySubCategory subCategory = UtilitySubCategory.None,
            string iconAddress = null,
            WeaponType requiredWeapon = WeaponType.None,
            float damageMultiplierPercent = 100f,
            float attackSpeedMultiplierPercent = 100f,
            IReadOnlyList<SkillEffectType> effects = null,
            float cooldown = 0f,
            SkillEffectType effectType = SkillEffectType.None,
            float effectValue = 0f)
        {
            Id = id;
            Name = name;
            Category = category;
            SubCategory = subCategory;
            IconAddress = iconAddress;
            RequiredWeapon = requiredWeapon;
            DamageMultiplierPercent = damageMultiplierPercent;
            AttackSpeedMultiplierPercent = attackSpeedMultiplierPercent;
            Effects = effects ?? new List<SkillEffectType>();
            Cooldown = cooldown;
            EffectType = effectType;
            EffectValue = effectValue;
        }
    }
}
