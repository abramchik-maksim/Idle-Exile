using Game.Domain.Combat;
using Game.Domain.Combat.Progression;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat/Enemy Definition", fileName = "NewEnemy")]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public float baseHealth = 30f;
        public float baseDamage = 5f;
        public float baseArmor = 2f;
        public float baseSpeed = 2f;

        [Header("Behavior")]
        public EnemyArchetype archetype = EnemyArchetype.Melee;
        public float attackRange = 1f;
        public float attackSpeed = 0.8f;

        [Header("Caster (only for Caster archetype)")]
        [Tooltip("Assign a Spell Definition SO. Only used when Archetype = Caster.")]
        public SpellDefinitionSO spell;

        [Header("Visuals")]
        [Tooltip("Index into CombatVisualDatabaseSO entries")]
        public int visualId;
        [Tooltip("Index into CombatVisualDatabaseSO entries for ranged projectile sprite")]
        public int projectileVisualId;

        public EnemyDefinition ToDomain() =>
            new(id, displayName, baseHealth, baseDamage, baseArmor, baseSpeed,
                archetype, attackRange, attackSpeed, spell != null ? spell.ToDomain() : null,
                visualId, projectileVisualId);
    }
}
