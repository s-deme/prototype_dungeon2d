using System;

namespace LanternDepths
{
    public enum Terrain { Wall, Room, Corridor, Stairs }

    public readonly struct TileData
    {
        public Terrain Terrain { get; }
        public int RoomId { get; }
        public bool IsWalkable => Terrain != Terrain.Wall;
        public TileData(Terrain terrain, int roomId = -1) { Terrain = terrain; RoomId = roomId; }
    }

    public sealed class DungeonMap
    {
        private readonly TileData[] tiles;
        public int Width { get; }
        public int Height { get; }
        public DungeonMap(int width, int height)
        {
            if (width < 3 || height < 3 || width > 256 || height > 256) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width; Height = height;
            tiles = new TileData[width * height];
            Array.Fill(tiles, new TileData(Terrain.Wall));
        }
        public bool Contains(GridPosition p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
        public TileData GetTile(GridPosition p) => Contains(p) ? tiles[p.Y * Width + p.X] : new TileData(Terrain.Wall);
        public bool IsWalkable(GridPosition p) => GetTile(p).IsWalkable;
        public void SetTile(GridPosition p, TileData tile)
        {
            if (!Contains(p)) throw new ArgumentOutOfRangeException(nameof(p));
            tiles[p.Y * Width + p.X] = tile;
        }
    }
}
