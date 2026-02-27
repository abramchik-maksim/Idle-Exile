using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Skills
{
    [CreateAssetMenu(menuName = "Idle Exile/Skill Database", fileName = "SkillDatabase")]
    public sealed class SkillDatabaseSO : ScriptableObject
    {
        public List<SkillDefinitionSO> skills = new();
    }
}
