using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Progression.TreeTalents
{
    public sealed class TreeTalentsState
    {
        public const int TrunkWidth = 2;
        public const int TrunkHeight = 4;

        private readonly List<BranchInstance> _branchInventory;
        private readonly List<BranchInstance> _placedBranches;
        private readonly Dictionary<NodeAllianceType, int> _allianceCounts;

        public int Level { get; private set; }
        public int CurrentXp { get; private set; }
        public int XpToNextLevel { get; private set; }

        public IReadOnlyList<BranchInstance> BranchInventory => _branchInventory.AsReadOnly();
        public IReadOnlyList<BranchInstance> PlacedBranches => _placedBranches.AsReadOnly();
        public IReadOnlyDictionary<NodeAllianceType, int> AllianceCounts => _allianceCounts;

        public TreeTalentsState(
            int level = 1,
            int currentXp = 0,
            int xpToNextLevel = 100,
            List<BranchInstance> branchInventory = null,
            List<BranchInstance> placedBranches = null)
        {
            Level = Math.Max(1, level);
            CurrentXp = Math.Max(0, currentXp);
            XpToNextLevel = Math.Max(1, xpToNextLevel);
            _branchInventory = branchInventory ?? new List<BranchInstance>();
            _placedBranches = placedBranches ?? new List<BranchInstance>();
            _allianceCounts = new Dictionary<NodeAllianceType, int>();
            RecalculateAlliances();
        }

        public void AddGeneratedBranch(BranchInstance branch)
        {
            if (branch == null) return;
            _branchInventory.Add(branch);
        }

        public BranchInstance FindInventoryBranch(string branchId) =>
            _branchInventory.FirstOrDefault(b => b.Id == branchId);

        public bool TryPlaceBranch(
            string branchId,
            GridCoord anchor,
            IReadOnlyList<int> halfWidthsByRow,
            int rotationQuarterTurns = 0)
        {
            var branch = FindInventoryBranch(branchId);
            if (branch == null || branch.IsPlaced) return false;
            if (!CanPlace(branch, anchor, halfWidthsByRow, rotationQuarterTurns)) return false;

            branch.PlaceAt(anchor, rotationQuarterTurns);
            _branchInventory.Remove(branch);
            _placedBranches.Add(branch);
            RecalculateAlliances();
            return true;
        }

        public bool TryRemovePlacedBranch(string branchId)
        {
            var branch = _placedBranches.FirstOrDefault(b => b.Id == branchId);
            if (branch == null) return false;
            if (HasDependentBranches(branch)) return false;

            _placedBranches.Remove(branch);
            RecalculateAlliances();
            return true;
        }

        public bool GainXp(int amount)
        {
            if (amount <= 0) return false;

            CurrentXp += amount;
            var leveledUp = false;
            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;
                XpToNextLevel = CalculateNextXp(Level);
                leveledUp = true;
            }

            return leveledUp;
        }

        public void RecalculateAlliances()
        {
            _allianceCounts.Clear();
            foreach (var branch in _placedBranches)
            {
                foreach (var tile in branch.Tiles)
                {
                    if (tile.Node == null || tile.Node.IsSocket) continue;
                    var type = tile.Node.AllianceType;
                    if (type == NodeAllianceType.None) continue;

                    if (!_allianceCounts.TryAdd(type, 1))
                        _allianceCounts[type]++;
                }
            }
        }

        public IReadOnlyCollection<GridCoord> GetAvailableSockets()
        {
            var occupied = GetOccupiedCoords();
            var trunk = GetTrunkCoords();
            var sockets = new List<GridCoord>();

            foreach (var s in GetAllPotentialSockets())
            {
                if (!occupied.Contains(s) && !trunk.Contains(s))
                    sockets.Add(s);
            }

            return sockets;
        }

        private bool CanPlace(
            BranchInstance branch,
            GridCoord anchor,
            IReadOnlyList<int> halfWidthsByRow,
            int rotationQuarterTurns)
        {
            var occupied = GetOccupiedCoords();
            var trunkCoords = GetTrunkCoords();

            var potentialSockets = GetAllPotentialSockets();
            if (!potentialSockets.Contains(anchor) || occupied.Contains(anchor))
                return false;

            foreach (var tile in branch.Tiles)
            {
                var rotatedOffset = BranchInstance.GetRotatedOffset(tile.Offset, rotationQuarterTurns);
                var coord = anchor.Add(rotatedOffset);
                if (!IsInsideUnlockedArea(coord, halfWidthsByRow))
                    return false;
                if (occupied.Contains(coord) || trunkCoords.Contains(coord))
                    return false;
            }

            return true;
        }

        private HashSet<GridCoord> GetOccupiedCoords()
        {
            var set = new HashSet<GridCoord>();
            foreach (var placed in _placedBranches)
            foreach (var coord in placed.GetWorldCoords())
                set.Add(coord);
            return set;
        }

        private static HashSet<GridCoord> GetTrunkCoords()
        {
            var set = new HashSet<GridCoord>();
            for (var y = 0; y < TrunkHeight; y++)
            for (var x = 0; x < TrunkWidth; x++)
                set.Add(new GridCoord(x, y));
            return set;
        }

        private HashSet<GridCoord> GetAllPotentialSockets()
        {
            var sockets = new HashSet<GridCoord>
            {
                // Trunk is at the bottom (y: 0..3). Sockets are on its upper half.
                new(-1, 2),
                new(2, 2),
                new(-1, 3),
                new(2, 3),
            };

            foreach (var branch in _placedBranches)
            {
                foreach (var tile in branch.Tiles)
                {
                    if (tile.Node != null && tile.Node.IsSocket)
                    {
                        var rotated = BranchInstance.GetRotatedOffset(tile.Offset, branch.PlacedRotationQuarterTurns);
                        var center = branch.Anchor.Add(rotated);

                        // Socket on a branch exposes adjacent free attachment cells.
                        sockets.Add(center.Add(new GridCoord(1, 0)));
                        sockets.Add(center.Add(new GridCoord(-1, 0)));
                        sockets.Add(center.Add(new GridCoord(0, 1)));
                        sockets.Add(center.Add(new GridCoord(0, -1)));
                    }
                }
            }

            return sockets;
        }

        private bool HasDependentBranches(BranchInstance parent)
        {
            var providedSockets = GetProvidedSocketsForBranch(parent);
            foreach (var candidate in _placedBranches)
            {
                if (candidate.Id == parent.Id) continue;
                if (providedSockets.Contains(candidate.Anchor))
                    return true;
            }

            return false;
        }

        private static HashSet<GridCoord> GetProvidedSocketsForBranch(BranchInstance branch)
        {
            var result = new HashSet<GridCoord>();
            foreach (var tile in branch.Tiles)
            {
                if (tile.Node == null || !tile.Node.IsSocket) continue;

                var rotated = BranchInstance.GetRotatedOffset(tile.Offset, branch.PlacedRotationQuarterTurns);
                var center = branch.Anchor.Add(rotated);
                result.Add(center.Add(new GridCoord(1, 0)));
                result.Add(center.Add(new GridCoord(-1, 0)));
                result.Add(center.Add(new GridCoord(0, 1)));
                result.Add(center.Add(new GridCoord(0, -1)));
            }

            return result;
        }

        private static bool IsInsideUnlockedArea(GridCoord coord, IReadOnlyList<int> halfWidthsByRow)
        {
            if (halfWidthsByRow == null || halfWidthsByRow.Count == 0) return false;
            if (coord.Y < 0 || coord.Y >= halfWidthsByRow.Count) return false;
            var sideWidth = Math.Max(0, halfWidthsByRow[coord.Y]);
            // halfWidthsByRow stores side width from trunk.
            // Full row width = left side + trunk(2) + right side.
            return coord.X >= -sideWidth && coord.X <= sideWidth + 1;
        }

        private static int CalculateNextXp(int level) => 100 + (level - 1) * 25;
    }
}
