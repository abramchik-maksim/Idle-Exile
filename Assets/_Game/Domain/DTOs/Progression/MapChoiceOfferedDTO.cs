namespace Game.Domain.DTOs.Progression
{
    public readonly struct MapChoiceOfferedDTO
    {
        public MapOptionInfo Option1 { get; }
        public MapOptionInfo Option2 { get; }
        public int TierIndex { get; }
        public int ChoiceIndex { get; }

        public MapChoiceOfferedDTO(MapOptionInfo option1, MapOptionInfo option2, int tierIndex, int choiceIndex)
        {
            Option1 = option1;
            Option2 = option2;
            TierIndex = tierIndex;
            ChoiceIndex = choiceIndex;
        }
    }

    public readonly struct MapOptionInfo
    {
        public string MapId { get; }
        public string Name { get; }
        public string Description { get; }

        public MapOptionInfo(string mapId, string name, string description)
        {
            MapId = mapId;
            Name = name;
            Description = description ?? string.Empty;
        }
    }
}
