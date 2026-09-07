using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public interface IDungeonGenerator { DungeonMap Generate(Random random, int width, int height, int roomCount); }

    public sealed class RoomCorridorGenerator : IDungeonGenerator
    {
        private readonly struct Room
        {
            public readonly int X, Y, Width, Height;
            public GridPosition Center => new GridPosition(X + Width / 2, Y + Height / 2);
            public Room(int x, int y, int width, int height) { X = x; Y = y; Width = width; Height = height; }
            public bool Overlaps(Room other) => X <= other.X + other.Width && X + Width >= other.X && Y <= other.Y + other.Height && Y + Height >= other.Y;
        }
        public DungeonMap Generate(Random random, int width, int height, int roomCount)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (width < 16 || height < 12 || roomCount < 2 || roomCount > 40) throw new ArgumentException("Map needs at least 16 x 12 and 2..40 rooms.");
            var map = new DungeonMap(width, height); var rooms = new List<Room>();
            for (int attempt = 0; attempt < roomCount * 30 && rooms.Count < roomCount; attempt++)
            {
                int w = random.Next(4, Math.Min(9, width - 3)); int h = random.Next(4, Math.Min(8, height - 3));
                var room = new Room(random.Next(1, width - w), random.Next(1, height - h), w, h);
                bool overlaps = false; foreach (var existing in rooms) if (room.Overlaps(existing)) { overlaps = true; break; }
                if (!overlaps) rooms.Add(room);
            }
            if (rooms.Count < 2)
            {
                rooms.Clear(); rooms.Add(new Room(1, 1, 5, 5)); rooms.Add(new Room(width - 6, height - 6, 5, 5));
            }
            for (int i = 0; i < rooms.Count; i++)
            {
                var r = rooms[i];
                for (int y = r.Y; y < r.Y + r.Height; y++) for (int x = r.X; x < r.X + r.Width; x++)
                    map.SetTile(new GridPosition(x, y), new TileData(Terrain.Room, i));
            }
            for (int i = 1; i < rooms.Count; i++)
            {
                var p = rooms[i - 1].Center; var goal = rooms[i].Center;
                bool horizontalFirst = random.Next(2) == 0;
                while (p != goal)
                {
                    if ((horizontalFirst && p.X != goal.X) || p.Y == goal.Y) p += new GridPosition(Math.Sign(goal.X - p.X), 0);
                    else p += new GridPosition(0, Math.Sign(goal.Y - p.Y));
                    if (!map.IsWalkable(p)) map.SetTile(p, new TileData(Terrain.Corridor));
                }
            }
            return map;
        }
    }
}
