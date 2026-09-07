using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace LanternDepths.Tests
{
    public sealed class GenerationTests
    {
        internal static RunRules Rules(int enemies = 6) => new RunRules(40, 28, 8, enemies, 30, 6, 1, new ProgressionRules(new[] { 12, 30, 60, 100, 160 }, 5, 2));
        [Test] public void OneHundredSeedsAreConnectedAndPlacementIsUnique()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var factory = new FloorFactory(new RoomCorridorGenerator(), Rules());
                var floor = factory.Create(new Random(seed), 1);
                var same = factory.Create(new Random(seed), 1);
                var visited = new HashSet<GridPosition> { floor.Entrance }; var queue = new Queue<GridPosition>(); queue.Enqueue(floor.Entrance);
                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    foreach (var d in MovementRules.Directions)
                        if (MovementRules.CanReachAdjacent(floor.Map, p, p + d) && visited.Add(p + d)) queue.Enqueue(p + d);
                }
                int walkable = 0;
                for (int y = 0; y < floor.Map.Height; y++) for (int x = 0; x < floor.Map.Width; x++)
                {
                    var p = new GridPosition(x, y);
                    if (floor.Map.IsWalkable(p)) walkable++;
                    Assert.That(floor.Map.GetTile(p).Terrain, Is.EqualTo(same.Map.GetTile(p).Terrain));
                    if (x == 0 || y == 0 || x == floor.Map.Width - 1 || y == floor.Map.Height - 1) Assert.That(floor.Map.IsWalkable(p), Is.False);
                }
                Assert.That(visited.Count, Is.EqualTo(walkable), $"Seed {seed}");
                var positions = new HashSet<GridPosition> { floor.Entrance };
                Assert.That(positions.Add(floor.Stairs), Is.True);
                foreach (var enemy in floor.Enemies) Assert.That(positions.Add(enemy.Position), Is.True);
                Assert.That(same.Entrance, Is.EqualTo(floor.Entrance)); Assert.That(same.Stairs, Is.EqualTo(floor.Stairs));
            }
        }
        [Test] public void DescendingPreservesPlayerAndSkipsEnemyActions()
        {
            var game = new RunController(Rules()); game.StartNewRun(12);
            var player = game.State.Player; player.TakeDamage(3);
            player.Position = game.State.Floor.Stairs;
            var old = game.State.Floor;
            var result = game.Execute(new PlayerCommand(CommandKind.Descend));
            Assert.That(result.ChangedFloor, Is.True); Assert.That(game.State.FloorNumber, Is.EqualTo(2));
            Assert.That(game.State.Floor, Is.Not.SameAs(old)); Assert.That(game.State.Player, Is.SameAs(player));
            Assert.That(player.Hp, Is.EqualTo(27)); Assert.That(player.Position, Is.EqualTo(game.State.Floor.Entrance));
            Assert.That(game.State.TurnNumber, Is.EqualTo(1));
            foreach (var e in result.Events) Assert.That(e.Kind, Is.Not.EqualTo(EventKind.Damaged));
            player.TakeDamage(100); game.StartNewRun(12);
            Assert.That(game.State.IsGameOver, Is.False); Assert.That(game.State.TurnNumber, Is.Zero); Assert.That(game.State.FloorNumber, Is.EqualTo(1));
        }
        private sealed class FailingGenerator : IDungeonGenerator
        {
            private int calls;
            public DungeonMap Generate(Random random, int width, int height, int rooms)
            {
                if (++calls == 2) throw new InvalidOperationException("Test failure.");
                return new RoomCorridorGenerator().Generate(random, width, height, rooms);
            }
        }
        [Test] public void FailedFloorGenerationDoesNotDestroyCurrentFloor()
        {
            var game = new RunController(Rules(), new FailingGenerator()); game.StartNewRun(2);
            var floor = game.State.Floor; game.State.Player.Position = floor.Stairs;
            Assert.That(game.Execute(new PlayerCommand(CommandKind.Descend)).ConsumesTurn, Is.False);
            Assert.That(game.State.Floor, Is.SameAs(floor)); Assert.That(game.State.FloorNumber, Is.EqualTo(1)); Assert.That(game.State.TurnNumber, Is.Zero);
        }
    }
}
