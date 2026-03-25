using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Configs.Skills
{
    [CreateAssetMenu(menuName = "Idle Exile/Skills/Skill Gem Database", fileName = "SkillGemDatabase")]
    public sealed class SkillGemDatabaseSO : ScriptableObject
    {
        public List<SkillGemDefinitionSO> gems = new();
        public List<SkillAffixDefinitionSO> allAffixes = new();
    }
}
