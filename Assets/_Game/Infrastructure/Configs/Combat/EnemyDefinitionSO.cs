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

        public EnemyDefinition ToDomain() =>
            new(id, displayName, baseHealth, baseDamage, baseArmor, baseSpeed);
    }
}
