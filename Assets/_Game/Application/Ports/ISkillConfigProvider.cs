using System.Collections.Generic;
using Game.Domain.Skills;

namespace Game.Application.Ports
{
    public interface ISkillConfigProvider
    {
        SkillDefinition GetSkillDefinition(string id);
        IReadOnlyList<SkillDefinition> GetAllSkills();
        IReadOnlyList<SkillDefinition> GetSkillsByCategory(SkillCategory category);
    }
}
