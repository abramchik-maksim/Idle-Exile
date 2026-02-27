using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Skills
{
    public sealed class SkillCollection
    {
        private readonly List<SkillInstance> _skills = new();

        public IReadOnlyList<SkillInstance> Skills => _skills;

        public SkillCollection() { }

        public SkillCollection(List<SkillInstance> skills)
        {
            _skills = skills ?? new List<SkillInstance>();
        }

        public bool TryAdd(SkillInstance skill)
        {
            if (skill == null) return false;
            if (_skills.Any(s => s.Uid == skill.Uid)) return false;
            _skills.Add(skill);
            return true;
        }

        public bool Remove(string uid)
        {
            var skill = _skills.FirstOrDefault(s => s.Uid == uid);
            if (skill == null) return false;
            _skills.Remove(skill);
            return true;
        }

        public SkillInstance Find(string uid) =>
            _skills.FirstOrDefault(s => s.Uid == uid);

        public SkillInstance FindByDefinitionId(string definitionId) =>
            _skills.FirstOrDefault(s => s.Definition.Id == definitionId);

        public IReadOnlyList<SkillInstance> GetByCategory(SkillCategory category) =>
            _skills.Where(s => s.Definition.Category == category).ToList();

        public IReadOnlyList<SkillInstance> GetBySubCategory(UtilitySubCategory subCategory) =>
            _skills.Where(s => s.Definition.SubCategory == subCategory).ToList();

        public void Clear() => _skills.Clear();
    }
}
