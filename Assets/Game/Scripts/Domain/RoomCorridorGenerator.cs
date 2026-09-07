using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public interface IDungeonGenerator { DungeonMap Generate(Random random, int width, int height, int roomCount); }

    public sealed class RoomCorridorGenerator : IDungeonGenerator
    {
        private static readonly GridPosition[] CardinalDirections =
        {
            new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0)
        };

        private readonly struct Room
        {
            public readonly int X, Y, Width, Height;
            public GridPosition Center => new GridPosition(X + Width / 2, Y + Height / 2);
            public Room(int x, int y, int width, int height) { X = x; Y = y; Width = width; Height = height; }
            public bool OverlapsWithMargin(Room other) =>
                X - 1 <= other.X + other.Width && X + Width >= other.X - 1 &&
                Y - 1 <= other.Y + other.Height && Y + Height >= other.Y - 1;

            public List<Door> Doors()
            {
                var doors = new List<Door>();
                for (int x = X; x < X + Width; x++)
                {
                    doors.Add(new Door(new GridPosition(x, Y - 1), new GridPosition(0, -1)));
                    doors.Add(new Door(new GridPosition(x, Y + Height), new GridPosition(0, 1)));
                }
                for (int y = Y; y < Y + Height; y++)
                {
                    doors.Add(new Door(new GridPosition(X - 1, y), new GridPosition(-1, 0)));
                    doors.Add(new Door(new GridPosition(X + Width, y), new GridPosition(1, 0)));
                }
                return doors;
            }
        }

        private readonly struct Door
        {
            public readonly GridPosition Outside;
            public readonly GridPosition Direction;
            public Door(GridPosition outside, GridPosition direction) { Outside = outside; Direction = direction; }
        }

        public DungeonMap Generate(Random random, int width, int height, int roomCount)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (width < 16 || height < 12 || roomCount < 2 || roomCount > 40) throw new ArgumentException("Map needs at least 16 x 12 and 2..40 rooms.");

            for (int attempt = 0; attempt < 48; attempt++)
            {
                var rooms = CreateRooms(random, width, height, roomCount);
                if (rooms.Count < 2) continue;
                var map = new DungeonMap(width, height);
                CarveRooms(map, rooms);
                if (AddTreasureBranches(map, rooms, random) == 0) continue;
                if (ConnectRooms(map, rooms, random)) return map;
            }
            throw new InvalidOperationException("Could not create a connected dungeon.");
        }

        private static List<Room> CreateRooms(Random random, int width, int height, int roomCount)
        {
            var rooms = new List<Room>();
            for (int attempt = 0; attempt < roomCount * 30 && rooms.Count < roomCount; attempt++)
            {
                int w = random.Next(4, Math.Min(9, width - 3));
                int h = random.Next(4, Math.Min(8, height - 3));
                var room = new Room(random.Next(1, width - w), random.Next(1, height - h), w, h);
                bool overlaps = false;
                foreach (var existing in rooms) if (room.OverlapsWithMargin(existing)) { overlaps = true; break; }
                if (!overlaps) rooms.Add(room);
            }
            return rooms;
        }

        private static void CarveRooms(DungeonMap map, List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                for (int y = room.Y; y < room.Y + room.Height; y++) for (int x = room.X; x < room.X + room.Width; x++)
                    map.SetTile(new GridPosition(x, y), new TileData(Terrain.Room, i));
            }
        }

        private static bool ConnectRooms(DungeonMap map, List<Room> rooms, Random random)
        {
            var connected = new List<Room> { rooms[0] };
            var remaining = new List<Room>(rooms);
            remaining.RemoveAt(0);
            while (remaining.Count > 0)
            {
                int selected = -1;
                List<GridPosition> selectedPath = null;
                int bestDistance = int.MaxValue;
                foreach (var from in connected) foreach (var to in remaining)
                {
                    int distance = from.Center.Distance(to.Center);
                    if (distance > bestDistance) continue;
                    var path = FindPath(map, from, to, random);
                    if (path == null) continue;
                    selected = remaining.IndexOf(to); selectedPath = path; bestDistance = distance;
                }
                if (selected < 0) return false;
                CarveCorridor(map, selectedPath);
                connected.Add(remaining[selected]); remaining.RemoveAt(selected);
            }
            return true;
        }

        private static List<GridPosition> FindPath(DungeonMap map, Room from, Room to, Random random)
        {
            var starts = from.Doors(); var goals = to.Doors();
            Shuffle(starts, random); Shuffle(goals, random);
            foreach (var start in starts) foreach (var goal in goals)
            {
                var path = FindPath(map, start.Outside, goal.Outside);
                if (path != null) return path;
            }
            return null;
        }

        private static List<GridPosition> FindPath(DungeonMap map, GridPosition start, GridPosition goal)
        {
            if (!CanCarve(map, start, start, goal) || !CanCarve(map, goal, start, goal)) return null;
            var previous = new Dictionary<GridPosition, GridPosition> { [start] = start };
            var queue = new Queue<GridPosition>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == goal) break;
                foreach (var direction in CardinalDirections)
                {
                    var next = current + direction;
                    if (previous.ContainsKey(next) || !CanCarve(map, next, start, goal)) continue;
                    previous.Add(next, current); queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(goal)) return null;
            var path = new List<GridPosition>();
            for (var current = goal; current != start; current = previous[current]) path.Add(current);
            path.Add(start); path.Reverse();
            return IsOneTileWide(path) ? path : null;
        }

        private static bool CanCarve(DungeonMap map, GridPosition position, GridPosition start, GridPosition goal)
        {
            if (!IsInterior(map, position) || map.GetTile(position).Terrain != Terrain.Wall) return false;
            int roomNeighbors = 0;
            foreach (var direction in MovementRules.Directions)
            {
                var terrain = map.GetTile(position + direction).Terrain;
                if (terrain == Terrain.Corridor || (terrain == Terrain.Room && position != start && position != goal)) return false;
                if (terrain == Terrain.Room && (direction.X == 0 || direction.Y == 0)) roomNeighbors++;
            }
            return roomNeighbors <= 1;
        }

        private static bool IsOneTileWide(List<GridPosition> path)
        {
            var cells = new HashSet<GridPosition>(path);
            for (int i = 0; i < path.Count; i++) foreach (var direction in CardinalDirections)
            {
                var adjacent = path[i] + direction;
                if (!cells.Contains(adjacent)) continue;
                bool previous = i > 0 && adjacent == path[i - 1];
                bool next = i + 1 < path.Count && adjacent == path[i + 1];
                if (!previous && !next) return false;
            }
            return true;
        }

        private static void CarveCorridor(DungeonMap map, List<GridPosition> path)
        {
            foreach (var position in path) map.SetTile(position, new TileData(Terrain.Corridor));
        }

        private static int AddTreasureBranches(DungeonMap map, List<Room> rooms, Random random)
        {
            int branches = 0;
            for (int attempt = 0; attempt < rooms.Count * 8 && branches < 2; attempt++)
            {
                var doors = rooms[random.Next(rooms.Count)].Doors(); Shuffle(doors, random);
                foreach (var door in doors)
                {
                    int length = random.Next(1, 4);
                    var path = new List<GridPosition>();
                    for (int step = 0; step < length; step++) path.Add(door.Outside + new GridPosition(door.Direction.X * step, door.Direction.Y * step));
                    if (!CanCarveBranch(map, path)) continue;
                    CarveCorridor(map, path); branches++; break;
                }
            }
            return branches;
        }

        private static bool CanCarveBranch(DungeonMap map, List<GridPosition> path)
        {
            if (!IsOneTileWide(path)) return false;
            for (int i = 0; i < path.Count; i++)
            {
                if (!CanCarve(map, path[i], path[0], path[0])) return false;
            }
            return true;
        }

        private static bool IsInterior(DungeonMap map, GridPosition position) =>
            position.X > 0 && position.Y > 0 && position.X < map.Width - 1 && position.Y < map.Height - 1;

        private static void Shuffle<T>(List<T> items, Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1); var item = items[i]; items[i] = items[j]; items[j] = item;
            }
        }
    }
}
