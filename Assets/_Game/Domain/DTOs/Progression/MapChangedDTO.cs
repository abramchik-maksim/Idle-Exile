namespace Game.Domain.DTOs.Progression
{
    public readonly struct MapChangedDTO
    {
        public string LocationId { get; }
        public int TierIndex { get; }
        public int MapIndex { get; }

        public MapChangedDTO(string locationId, int tierIndex, int mapIndex)
        {
            LocationId = locationId;
            TierIndex = tierIndex;
            MapIndex = mapIndex;
        }
    }
}
