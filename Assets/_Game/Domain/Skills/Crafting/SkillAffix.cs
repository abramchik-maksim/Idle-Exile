using Game.Domain.Items;

namespace Game.Domain.Skills.Crafting
{
    public sealed class SkillAffix
    {
        public SkillAffixDefinition Definition { get; }
        public Rarity Rarity { get; }
        public int Tier { get; }
        public float Value1 { get; }
        public float Value2 { get; }

        public SkillAffix(
            SkillAffixDefinition definition,
            Rarity rarity,
            int tier,
            float value1,
            float value2 = 0f)
        {
            Definition = definition;
            Rarity = rarity;
            Tier = tier;
            Value1 = value1;
            Value2 = value2;
        }

        public string GetDescription()
        {
            if (string.IsNullOrEmpty(Definition.DescriptionTemplate))
                return Definition.Name ?? Definition.Type.ToString();

            return string.Format(Definition.DescriptionTemplate, Value1, Value2);
        }
    }
}
