using System;

namespace LanternDepths
{
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public int X { get; }
        public int Y { get; }
        public GridPosition(int x, int y) { X = x; Y = y; }
        public static GridPosition operator +(GridPosition a, GridPosition b) => new GridPosition(a.X + b.X, a.Y + b.Y);
        public static GridPosition operator -(GridPosition a, GridPosition b) => new GridPosition(a.X - b.X, a.Y - b.Y);
        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition p && Equals(p);
        public override int GetHashCode() => unchecked(X * 397 ^ Y);
        public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
        public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
        public int Distance(GridPosition other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
        public override string ToString() => $"({X}, {Y})";
    }
}
