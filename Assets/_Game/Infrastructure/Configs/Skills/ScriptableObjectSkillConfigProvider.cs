using System.Collections.Generic;
using System.Linq;
using Game.Application.Ports;
using Game.Domain.Skills;

namespace Game.Infrastructure.Configs.Skills
{
    public sealed class ScriptableObjectSkillConfigProvider : ISkillConfigProvider
    {
        private readonly Dictionary<string, SkillDefinition> _skills = new();
        private readonly List<SkillDefinition> _allSkills = new();

        public ScriptableObjectSkillConfigProvider(SkillDatabaseSO database)
        {
            foreach (var so in database.skills)
            {
                if (so == null) continue;
                var def = so.ToDomain();
                _skills[def.Id] = def;
                _allSkills.Add(def);
            }
        }

        public SkillDefinition GetSkillDefinition(string id) =>
            _skills.TryGetValue(id, out var def) ? def : null;

        public IReadOnlyList<SkillDefinition> GetAllSkills() => _allSkills;

        public IReadOnlyList<SkillDefinition> GetSkillsByCategory(SkillCategory category) =>
            _allSkills.Where(s => s.Category == category).ToList();
    }
}
