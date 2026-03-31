using System.Collections.Generic;

namespace Game.Domain.Progression.TreeTalents
{
    public sealed class BranchInstance
    {
        private readonly List<BranchTile> _tiles;
        private readonly List<SeedType> _seedTypes;

        public string Id { get; }
        public int GenerationLevel { get; }
        public IReadOnlyList<SeedType> SeedTypes => _seedTypes.AsReadOnly();
        public IReadOnlyList<BranchTile> Tiles => _tiles.AsReadOnly();
        public bool IsPlaced { get; private set; }
        public GridCoord Anchor { get; private set; }

        public BranchInstance(string id, int generationLevel, List<SeedType> seedTypes, List<BranchTile> tiles)
        {
            Id = id;
            GenerationLevel = generationLevel;
            _seedTypes = seedTypes ?? new List<SeedType>();
            _tiles = tiles ?? new List<BranchTile>();
            Anchor = new GridCoord(0, 0);
        }

        public void PlaceAt(GridCoord anchor)
        {
            Anchor = anchor;
            IsPlaced = true;
        }

        public IEnumerable<GridCoord> GetWorldCoords()
        {
            foreach (var tile in _tiles)
                yield return Anchor.Add(tile.Offset);
        }
    }
}
