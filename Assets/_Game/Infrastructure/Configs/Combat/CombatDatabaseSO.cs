using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Combat
{
    [CreateAssetMenu(menuName = "Idle Exile/Combat Database", fileName = "CombatDatabase")]
    public sealed class CombatDatabaseSO : ScriptableObject
    {
        public List<EnemyDefinitionSO> enemies = new();
        public List<TierDefinitionSO> tiers = new();
    }
}
