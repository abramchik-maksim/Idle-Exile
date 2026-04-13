using Game.Domain.Items;

namespace Game.Domain.SaveSystem
{
    public sealed class CharacterDefinition
    {
        public HeroItemClass HeroClass { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string PresetId { get; }

        public CharacterDefinition(
            HeroItemClass heroClass,
            string displayName,
            string description,
            string presetId)
        {
            HeroClass = heroClass;
            DisplayName = displayName;
            Description = description;
            PresetId = presetId;
        }
    }
}
