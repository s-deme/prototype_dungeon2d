using NUnit.Framework;

namespace LanternDepths.Tests
{
    public sealed class GridTests
    {
        internal static DungeonMap OpenMap(int size = 7)
        {
            var map = new DungeonMap(size, size);
            for (int y = 1; y < size - 1; y++) for (int x = 1; x < size - 1; x++)
                map.SetTile(new GridPosition(x, y), new TileData(Terrain.Room, 0));
            return map;
        }
        [Test] public void GridEqualityAndBounds()
        {
            var map = OpenMap();
            Assert.That(new GridPosition(1, 2) + new GridPosition(2, 1), Is.EqualTo(new GridPosition(3, 3)));
            Assert.That(map.IsWalkable(new GridPosition(-1, 1)), Is.False);
            Assert.That(map.IsWalkable(new GridPosition(0, 1)), Is.False);
            Assert.That(map.IsWalkable(new GridPosition(1, 1)), Is.True);
            Assert.That(map.GetTile(new GridPosition(0, 1)).RoomId, Is.EqualTo(-1));
            Assert.That(map.IsWalkable(new GridPosition(7, 1)), Is.False);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => map.SetTile(new GridPosition(7, 0), new TileData(Terrain.Room)));
        }
        [Test] public void EightDirectionsRespectCornersAndOccupancy()
        {
            var map = OpenMap(); var start = new GridPosition(3, 3);
            var floor = new FloorState(map, start, new GridPosition(5, 5));
            var player = new CharacterState(0, "Player", start, 20, 5, 1);
            foreach (var d in MovementRules.Directions) Assert.That(MovementRules.CanMove(floor, player, player, start + d), Is.True);
            map.SetTile(new GridPosition(4, 3), new TileData(Terrain.Wall));
            Assert.That(MovementRules.CanMove(floor, player, player, new GridPosition(4, 4)), Is.False);
            floor.AddEnemy(new CharacterState(1, "Enemy", new GridPosition(2, 3), 10, 3, 0));
            Assert.That(MovementRules.CanMove(floor, player, player, new GridPosition(2, 3)), Is.False);
            Assert.That(MovementRules.CanMove(floor, player, player, start), Is.False);
        }
        [Test] public void EnemyViewStaysLiveAndReadOnly()
        {
            var floor = new FloorState(OpenMap(), new GridPosition(1, 1), new GridPosition(5, 5));
            var view = floor.Enemies;
            var enemy = new CharacterState(1, "Enemy", new GridPosition(2, 3), 10, 3, 0);
            floor.AddEnemy(enemy);
            Assert.That(view, Is.EqualTo(new[] { enemy }));
            Assert.Throws<System.NotSupportedException>(() => ((System.Collections.Generic.IList<CharacterState>)view).Clear());
        }
    }
}
