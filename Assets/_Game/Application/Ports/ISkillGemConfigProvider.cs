using System.Collections.Generic;
using Game.Domain.Skills.Crafting;

namespace Game.Application.Ports
{
    public interface ISkillGemConfigProvider
    {
        SkillGemDefinition GetGemDefinition(string id);
        IReadOnlyList<SkillGemDefinition> GetAllGems();
        SkillAffixDefinition GetAffixDefinition(string id);
        IReadOnlyList<SkillAffixDefinition> GetAffixesByElement(SkillGemElement element);
    }
}
