using Game.Domain.Combat;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat/Spell Definition", fileName = "NewSpell")]
    public sealed class SpellDefinitionSO : ScriptableObject
    {
        public string id;
        public string displayName;

        [Header("Cast")]
        public float castDuration = 2f;

        [Header("Effect")]
        [Tooltip("Multiplier on enemy's base damage")]
        public float damageMultiplier = 2f;
        public float aoERadius = 1.5f;
        public float detonationDelay = 0.5f;
        public DamageType damageType = DamageType.Fire;

        public SpellDefinition ToDomain() =>
            new(id, displayName, castDuration, damageMultiplier,
                aoERadius, detonationDelay, damageType);
    }
}
