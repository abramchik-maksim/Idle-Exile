using System;
using Game.Domain.Skills.Crafting;

namespace Game.Domain.Skills
{
    public sealed class SkillInstance
    {
        public string Uid { get; }
        public SkillDefinition Definition { get; }
        public int Level { get; set; }
        public SkillAffixSlots Affixes { get; } = new();

        public SkillInstance(SkillDefinition definition, int level = 1)
        {
            Uid = Guid.NewGuid().ToString("N");
            Definition = definition;
            Level = level;
        }

        public SkillInstance(string uid, SkillDefinition definition, int level = 1)
        {
            Uid = uid;
            Definition = definition;
            Level = level;
        }
    }
}
