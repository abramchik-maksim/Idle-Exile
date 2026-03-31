using System;

namespace Game.Domain.Progression.TreeTalents
{
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public int X { get; }
        public int Y { get; }

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public GridCoord Add(GridCoord offset) => new(X + offset.X, Y + offset.Y);

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);

        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);

        public override string ToString() => $"({X}, {Y})";
    }
}
