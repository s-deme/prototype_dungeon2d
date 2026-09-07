using NUnit.Framework;
using System;

namespace LanternDepths.Tests
{
    public sealed class CombatAndAiTests
    {
        [Test] public void KillAwardsExperienceOnceAndSupportsMultipleLevels()
        {
            var start = new GridPosition(2, 2); var floor = new FloorState(GridTests.OpenMap(), start, new GridPosition(5, 5));
            var enemy = new CharacterState(1, "Cinder Mite", new GridPosition(3, 2), 1, 2, 100, 30); floor.AddEnemy(enemy);
            var player = new CharacterState(0, "Wayfarer", start, 20, 5, 1); player.TakeDamage(10);
            var progression = new PlayerProgression(new ProgressionRules(new[] { 10, 25 }, 4, 1));
            var state = new RunState(player, floor, progression); var combat = new CombatResolver();
            var result = new ActionResult(); combat.Attack(state, player, enemy, result);
            Assert.That(enemy.IsAlive, Is.False); Assert.That(progression.Experience, Is.EqualTo(30));
            Assert.That(player.Level, Is.EqualTo(3)); Assert.That(player.Hp, Is.EqualTo(28));
            combat.Attack(state, player, enemy, result); Assert.That(progression.Experience, Is.EqualTo(30));
            Assert.That(floor.GetEnemyAt(enemy.Position), Is.Null);
            Assert.That(progression.NextThreshold(player), Is.EqualTo(-1));
        }
        [Test] public void AdjacentEnemyAttacksAndDeathStopsRemainingEnemies()
        {
            var start = new GridPosition(2, 2); var floor = new FloorState(GridTests.OpenMap(), start, new GridPosition(5, 5));
            floor.AddEnemy(new CharacterState(1, "A", new GridPosition(3, 2), 10, 9, 0));
            floor.AddEnemy(new CharacterState(2, "B", new GridPosition(2, 3), 10, 9, 0));
            var state = new RunState(new CharacterState(0, "Player", start, 1, 5, 0), floor);
            var result = new TurnProcessor(new ActionResolver(), new BasicEnemyBrain(), new Random(1)).Execute(state, new PlayerCommand(CommandKind.Wait));
            int hits = 0; foreach (var e in result.Events) if (e.Kind == EventKind.Damaged) hits++;
            Assert.That(hits, Is.EqualTo(1)); Assert.That(state.IsGameOver, Is.True);
        }
        [Test] public void PathfindingDetoursAndSightCannotCrossWalls()
        {
            var map = GridTests.OpenMap(9); var start = new GridPosition(6, 4); var floor = new FloorState(map, start, new GridPosition(7, 7));
            var enemy = new CharacterState(1, "A", new GridPosition(2, 4), 10, 3, 0); floor.AddEnemy(enemy);
            var player = new CharacterState(0, "P", start, 20, 4, 0); var state = new RunState(player, floor);
            var brain = new BasicEnemyBrain();
            var move = brain.ChooseAction(state, enemy, new Random(1));
            Assert.That(move.Kind, Is.EqualTo(CommandKind.Move));
            Assert.That((enemy.Position + move.Direction).Distance(start), Is.LessThan(enemy.Position.Distance(start)));
            map.SetTile(new GridPosition(3, 4), new TileData(Terrain.Wall));
            Assert.That(BasicEnemyBrain.HasLineOfSight(map, enemy.Position, start), Is.False);
            var step = BasicEnemyBrain.FindStep(floor, enemy, player);
            Assert.That(step, Is.Not.EqualTo(enemy.Position)); Assert.That(MovementRules.CanMove(floor, enemy, player, step), Is.True);
            foreach (var d in MovementRules.Directions) map.SetTile(enemy.Position + d, new TileData(Terrain.Wall));
            Assert.That(BasicEnemyBrain.FindStep(floor, enemy, player), Is.EqualTo(enemy.Position));
            Assert.That(brain.ChooseAction(state, enemy, new Random(1)).Kind, Is.EqualTo(CommandKind.Wait));
        }
    }
}
