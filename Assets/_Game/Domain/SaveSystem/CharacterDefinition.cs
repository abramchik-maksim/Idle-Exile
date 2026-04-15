using Game.Domain.Items;

namespace Game.Domain.SaveSystem
{
    public sealed class CharacterDefinition
    {
        public HeroItemClass HeroClass { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string PresetId { get; }
        public int VisualId { get; }
        public int ProjectileVisualId { get; }

        public CharacterDefinition(
            HeroItemClass heroClass,
            string displayName,
            string description,
            string presetId,
            int visualId = 0,
            int projectileVisualId = 0)
        {
            HeroClass = heroClass;
            DisplayName = displayName;
            Description = description;
            PresetId = presetId;
            VisualId = visualId;
            ProjectileVisualId = projectileVisualId;
        }
    }
}
