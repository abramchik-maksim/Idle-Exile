using System.Collections.Generic;
using System.Linq;
using Game.Application.Ports;
using Game.Domain.Combat;
using Game.Domain.Skills.Crafting;

namespace Game.Infrastructure.Configs.Skills
{
    public sealed class ScriptableObjectSkillGemConfigProvider : ISkillGemConfigProvider
    {
        private readonly Dictionary<string, SkillGemDefinition> _gems = new();
        private readonly List<SkillGemDefinition> _allGems = new();
        private readonly Dictionary<string, SkillAffixDefinition> _affixes = new();
        private readonly List<SkillAffixDefinition> _allAffixes = new();

        public ScriptableObjectSkillGemConfigProvider(SkillGemDatabaseSO database)
        {
            foreach (var so in database.allAffixes)
            {
                if (so == null) continue;
                var def = so.ToDomain();
                _affixes[def.Id] = def;
                _allAffixes.Add(def);
            }

            foreach (var so in database.gems)
            {
                if (so == null) continue;
                var def = so.ToDomain();
                _gems[def.Id] = def;
                _allGems.Add(def);
            }
        }

        public SkillGemDefinition GetGemDefinition(string id) =>
            _gems.TryGetValue(id, out var def) ? def : null;

        public IReadOnlyList<SkillGemDefinition> GetAllGems() => _allGems;

        public SkillAffixDefinition GetAffixDefinition(string id) =>
            _affixes.TryGetValue(id, out var def) ? def : null;

        public IReadOnlyList<SkillAffixDefinition> GetAffixesByElement(SkillGemElement element)
        {
            return _allAffixes.Where(a =>
            {
                var damageElement = a.DamageType switch
                {
                    DamageType.Physical => SkillGemElement.Physical,
                    DamageType.Fire => SkillGemElement.Fire,
                    DamageType.Cold => SkillGemElement.Cold,
                    DamageType.Lightning => SkillGemElement.Lightning,
                    _ => SkillGemElement.Generic
                };
                return damageElement == element || element == SkillGemElement.Generic;
            }).ToList();
        }
    }
}
