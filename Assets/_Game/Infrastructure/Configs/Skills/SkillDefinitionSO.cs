using System.Collections.Generic;
using Game.Domain.Skills;
using UnityEngine;

namespace Game.Infrastructure.Configs.Skills
{
    [CreateAssetMenu(menuName = "Idle Exile/Skill Definition", fileName = "NewSkillDefinition")]
    public sealed class SkillDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string skillName;
        public SkillCategory category;
        public UtilitySubCategory subCategory;

        [Header("Visuals")]
        public string iconAddress;

        [Header("Main Skill")]
        public WeaponType requiredWeapon;
        [Tooltip("100 = base damage")]
        public float damageMultiplierPercent = 100f;
        [Tooltip("100 = base attack speed")]
        public float attackSpeedMultiplierPercent = 100f;
        public List<SkillEffectType> effects = new();

        [Header("Utility Skill")]
        public float cooldown;
        public SkillEffectType effectType;
        public float effectValue;

        public SkillDefinition ToDomain()
        {
            return new SkillDefinition(
                id: id,
                name: skillName,
                category: category,
                subCategory: subCategory,
                iconAddress: iconAddress,
                requiredWeapon: requiredWeapon,
                damageMultiplierPercent: damageMultiplierPercent,
                attackSpeedMultiplierPercent: attackSpeedMultiplierPercent,
                effects: effects,
                cooldown: cooldown,
                effectType: effectType,
                effectValue: effectValue
            );
        }
    }
}
