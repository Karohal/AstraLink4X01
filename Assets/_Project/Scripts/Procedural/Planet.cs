using System;

namespace Game.Procedural
{
    public struct GridPosition : IEquatable<GridPosition>
    {
        public int X;
        public int Y;

        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => (X, Y).GetHashCode();
        public override string ToString() => $"({X},{Y})";
    }

    public sealed class Planet
    {
        public Guid Id { get; }
        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }
        public Zone[,] Zones { get; }

        public Planet(Guid id, int seed, int width, int height, Zone[,] zones)
        {
            Id = id;
            Seed = seed;
            Width = width;
            Height = height;
            Zones = zones;
        }

        public bool TryGetZone(int x, int y, out Zone zone)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                zone = null;
                return false;
            }

            zone = Zones[x, y];
            return zone != null;
        }
    }
}
