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
        public int PlacedRotationQuarterTurns { get; private set; }

        public BranchInstance(string id, int generationLevel, List<SeedType> seedTypes, List<BranchTile> tiles)
        {
            Id = id;
            GenerationLevel = generationLevel;
            _seedTypes = seedTypes ?? new List<SeedType>();
            _tiles = tiles ?? new List<BranchTile>();
            Anchor = new GridCoord(0, 0);
            PlacedRotationQuarterTurns = 0;
        }

        public void PlaceAt(GridCoord anchor, int rotationQuarterTurns = 0)
        {
            Anchor = anchor;
            PlacedRotationQuarterTurns = NormalizeRotation(rotationQuarterTurns);
            IsPlaced = true;
        }

        public IEnumerable<GridCoord> GetWorldCoords(int rotationQuarterTurns = -1)
        {
            var rotation = rotationQuarterTurns >= 0
                ? NormalizeRotation(rotationQuarterTurns)
                : (IsPlaced ? PlacedRotationQuarterTurns : 0);

            foreach (var tile in _tiles)
                yield return Anchor.Add(GetRotatedOffset(tile.Offset, rotation));
        }

        public static GridCoord GetRotatedOffset(GridCoord offset, int rotationQuarterTurns)
        {
            var r = NormalizeRotation(rotationQuarterTurns);
            return r switch
            {
                1 => new GridCoord(-offset.Y, offset.X),
                2 => new GridCoord(-offset.X, -offset.Y),
                3 => new GridCoord(offset.Y, -offset.X),
                _ => offset
            };
        }

        private static int NormalizeRotation(int rotationQuarterTurns)
        {
            var r = rotationQuarterTurns % 4;
            return r < 0 ? r + 4 : r;
        }
    }
}
