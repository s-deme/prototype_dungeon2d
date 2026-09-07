using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LanternDepths.Tests
{
    public sealed class RunIntegrationTests
    {
        private sealed class WaitingBrain : IEnemyBrain
        {
            public PlayerCommand ChooseAction(RunState state, CharacterState enemy, Random random) => new PlayerCommand(CommandKind.Wait);
        }
        [Test] public void TwentySeedsSupportCollectEquipFightAndThreeFloorDescents()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var rules = new RunRules(40, 28, 8, 6, 30, 6, 1, new ProgressionRules(new[] { 12, 30, 60 }, 5, 2), ItemTests.Definitions());
                var game = new RunController(rules, brain: new WaitingBrain()); game.StartNewRun(seed);
                for (int floor = 1; floor <= 3; floor++)
                {
                    var ground = new List<KeyValuePair<GridPosition, ItemInstance>>(game.State.Floor.Items);
                    foreach (var pair in ground)
                    {
                        WalkTo(game, pair.Key);
                        Assert.That(game.Execute(new PlayerCommand(CommandKind.PickUp)).ConsumesTurn, Is.True);
                        Assert.That(game.State.Inventory.Find(pair.Value.Id), Is.SameAs(pair.Value));
                        if (pair.Value.Definition.Kind == ItemKind.Healing)
                        {
                            game.State.Player.TakeDamage(1);
                            Assert.That(game.Execute(new PlayerCommand(CommandKind.Use, itemId: pair.Value.Id)).ConsumesTurn, Is.True);
                        }
                        else Assert.That(game.Execute(new PlayerCommand(CommandKind.Equip, itemId: pair.Value.Id)).ConsumesTurn, Is.True);
                    }
                    WalkTo(game, game.State.Floor.Stairs);
                    int hp = game.State.Player.Hp, exp = game.State.Progression.Experience, count = game.State.Inventory.Items.Count;
                    int weapon = game.State.Equipment.WeaponId, armor = game.State.Equipment.ArmorId;
                    Assert.That(game.Execute(new PlayerCommand(CommandKind.Descend)).ChangedFloor, Is.True);
                    Assert.That(game.State.FloorNumber, Is.EqualTo(floor + 1));
                    Assert.That(game.State.Player.Hp, Is.EqualTo(hp)); Assert.That(game.State.Progression.Experience, Is.EqualTo(exp));
                    Assert.That(game.State.Inventory.Items.Count, Is.EqualTo(count));
                    Assert.That(game.State.Equipment.WeaponId, Is.EqualTo(weapon)); Assert.That(game.State.Equipment.ArmorId, Is.EqualTo(armor));
                }
                Assert.That(game.State.IsGameOver, Is.False);
            }
        }
        private static void WalkTo(RunController game, GridPosition target)
        {
            for (int attempts = 0; attempts < 2000 && game.State.Player.Position != target; attempts++)
            {
                var from = game.State.Player.Position;
                var queue = new Queue<GridPosition>(); var previous = new Dictionary<GridPosition, GridPosition> { [from] = from }; queue.Enqueue(from);
                while (queue.Count > 0 && !previous.ContainsKey(target))
                {
                    var p = queue.Dequeue();
                    foreach (var d in MovementRules.Directions)
                        if (!previous.ContainsKey(p + d) && MovementRules.CanReachAdjacent(game.State.Floor.Map, p, p + d))
                        { previous.Add(p + d, p); queue.Enqueue(p + d); }
                }
                Assert.That(previous.ContainsKey(target), Is.True);
                var step = target; while (previous[step] != from) step = previous[step];
                Assert.That(game.Execute(new PlayerCommand(CommandKind.Move, step - from)).ConsumesTurn, Is.True);
            }
            Assert.That(game.State.Player.Position, Is.EqualTo(target));
        }
        [Test] public void NormalAiAllowsFiveFloorVictoryWithCollectionHealingAndResume()
        {
            int victories = 0;
            for (int seed = 0; seed < 5; seed++)
            {
                var rules = new RunRules(40, 28, 8, 6, 30, 6, 1,
                    new ProgressionRules(new[] { 12, 30, 60, 100, 160, 240, 340, 460, 600 }, 5, 2), TacticalTests.Items());
                var game = new RunController(rules); game.StartNewRun(seed);
                for (int attempts = 0; attempts < 4000 && !game.State.IsFinished; attempts++)
                {
                    var state = game.State;
                    var heal = state.Inventory.Items.FirstOrDefault(i => i.Definition.Kind == ItemKind.Healing);
                    if (heal != null && state.Player.MaxHp - state.Player.Hp >= heal.Definition.Power)
                        game.Execute(new PlayerCommand(CommandKind.Use, itemId: heal.Id));
                    else if (state.Floor.GetItemAt(state.Player.Position) != null && state.Inventory.Items.Count < state.Inventory.Capacity)
                    {
                        var item = state.Floor.GetItemAt(state.Player.Position);
                        game.Execute(new PlayerCommand(CommandKind.PickUp));
                        if (!state.IsFinished && item.Definition.IsEquipment)
                            game.Execute(new PlayerCommand(CommandKind.Equip, itemId: item.Id));
                    }
                    else
                    {
                        var target = state.Inventory.Items.Count < state.Inventory.Capacity
                            ? state.Floor.Items.OrderBy(p => p.Value.Id).Select(p => p.Key).DefaultIfEmpty(state.Floor.Stairs).First()
                            : state.Floor.Stairs;
                        var guardian = state.Floor.Enemies.FirstOrDefault(e => e.IsAlive && e.Role == EnemyRole.Guardian);
                        if (target == state.Floor.Stairs && guardian != null) target = guardian.Position;
                        if (state.Player.Position == target) game.Execute(new PlayerCommand(CommandKind.Descend));
                        else
                        {
                            var from = state.Player.Position; var queue = new Queue<GridPosition>();
                            var previous = new Dictionary<GridPosition, GridPosition> { [from] = from }; queue.Enqueue(from);
                            while (queue.Count > 0 && !previous.ContainsKey(target))
                            {
                                var p = queue.Dequeue();
                                foreach (var d in MovementRules.Directions)
                                    if (!previous.ContainsKey(p + d) && MovementRules.CanReachAdjacent(state.Floor.Map, p, p + d))
                                    { previous.Add(p + d, p); queue.Enqueue(p + d); }
                            }
                            Assert.That(previous.ContainsKey(target), Is.True);
                            var step = target; while (previous[step] != from) step = previous[step];
                            game.Execute(new PlayerCommand(CommandKind.Move, step - from));
                        }
                    }
                    if (attempts % 25 == 0) game.Load(game.Save());
                }
                Assert.That(game.State.IsFinished, Is.True, "Run stalled without victory or death.");
                if (game.State.IsVictory) victories++;
                TestContext.WriteLine($"Seed {seed}: {(game.State.IsVictory ? "victory" : "death")}, floor {game.State.FloorNumber}, turns {game.State.TurnNumber}");
            }
            Assert.That(victories, Is.GreaterThan(0), "No tested normal-AI run reached the ending.");
        }
        [Test] public void FinalStairsCompleteTheRunOnceAndRestartClearsVictory()
        {
            var rules = new RunRules(40, 28, 8, 0, 30, 6, 1,
                new ProgressionRules(new[] { 12 }, 5, 2), finalFloor: 2);
            var game = new RunController(rules); game.StartNewRun(42);
            Assert.That(game.Execute(new PlayerCommand(CommandKind.Descend)).ConsumesTurn, Is.False);
            WalkTo(game, game.State.Floor.Stairs);
            Assert.That(game.Execute(new PlayerCommand(CommandKind.Descend)).ChangedFloor, Is.True);
            Assert.That(game.State.IsFinished, Is.False);
            WalkTo(game, game.State.Floor.Stairs);
            var floor = game.State.Floor; int turn = game.State.TurnNumber, hp = game.State.Player.Hp;
            var result = game.Execute(new PlayerCommand(CommandKind.Descend));
            Assert.That(result.ConsumesTurn, Is.True); Assert.That(result.ChangedFloor, Is.False);
            Assert.That(result.Events[0].Kind, Is.EqualTo(EventKind.Victory));
            Assert.That(game.State.IsVictory && game.State.IsFinished && !game.State.IsGameOver, Is.True);
            Assert.That(game.State.Floor, Is.SameAs(floor)); Assert.That(game.State.FloorNumber, Is.EqualTo(2));
            Assert.That(game.State.TurnNumber, Is.EqualTo(turn + 1)); Assert.That(game.State.Player.Hp, Is.EqualTo(hp));
            foreach (CommandKind kind in Enum.GetValues(typeof(CommandKind)))
                Assert.That(game.Execute(new PlayerCommand(kind)).ConsumesTurn, Is.False);
            Assert.That(game.State.TurnNumber, Is.EqualTo(turn + 1));
            game.StartNewRun(42);
            Assert.That(game.State.IsFinished, Is.False); Assert.That(game.State.TurnNumber, Is.Zero);
            Assert.That(game.State.FloorNumber, Is.EqualTo(1));
        }
        [Test] public void SameSeedAndCommandStreamProduceTheSameRun()
        {
            var a = new RunController(GenerationTests.Rules()); var b = new RunController(GenerationTests.Rules()); a.StartNewRun(998); b.StartNewRun(998);
            var input = new Random(11);
            for (int i = 0; i < 300; i++)
            {
                var command = i % 5 == 0 ? new PlayerCommand(CommandKind.Wait) : new PlayerCommand(CommandKind.Move, new GridPosition(input.Next(-1, 2), input.Next(-1, 2)));
                var left = a.Execute(command); var right = b.Execute(command);
                Assert.That(a.State.Player.Position, Is.EqualTo(b.State.Player.Position)); Assert.That(a.State.Player.Hp, Is.EqualTo(b.State.Player.Hp));
                Assert.That(a.State.TurnNumber, Is.EqualTo(b.State.TurnNumber)); Assert.That(left.Events.Count, Is.EqualTo(right.Events.Count));
                for (int e = 0; e < left.Events.Count; e++) Assert.That(left.Events[e].Message, Is.EqualTo(right.Events[e].Message));
                for (int e = 0; e < a.State.Floor.Enemies.Count; e++)
                {
                    Assert.That(a.State.Floor.Enemies[e].Position, Is.EqualTo(b.State.Floor.Enemies[e].Position));
                    Assert.That(a.State.Floor.Enemies[e].Hp, Is.EqualTo(b.State.Floor.Enemies[e].Hp));
                }
            }
        }
        [Test] public void InvalidMovementAndRulesAreRejectedWithoutAdvancing()
        {
            var game = new RunController(GenerationTests.Rules()); game.StartNewRun(1);
            var position = game.State.Player.Position;
            foreach (var direction in new[] { default(GridPosition), new GridPosition(2, 0), new GridPosition(int.MaxValue, int.MinValue) })
                Assert.That(game.Execute(new PlayerCommand(CommandKind.Move, direction)).ConsumesTurn, Is.False);
            Assert.That(game.State.TurnNumber, Is.Zero); Assert.That(game.State.Player.Position, Is.EqualTo(position));
            Assert.Throws<ArgumentException>(() => new ProgressionRules(new[] { 10, 9 }, 1, 1));
            Assert.Throws<ArgumentException>(() => new RoomCorridorGenerator().Generate(new Random(1), 5, 5, 2));
            Assert.Throws<ArgumentException>(() => new ItemDefinition("", "x", "", ItemKind.Healing, 1));
        }
    }
}
