using NUnit.Framework;
using System;

namespace LanternDepths.Tests
{
    public sealed class TurnTests
    {
        private sealed class CountingBrain : IEnemyBrain
        {
            public int Calls;
            public PlayerCommand ChooseAction(RunState state, CharacterState enemy, Random random)
            { Calls++; return new PlayerCommand(CommandKind.Wait); }
        }
        [Test] public void OnlyValidActionsAdvanceAllEnemiesOnce()
        {
            var map = GridTests.OpenMap(); var start = new GridPosition(1, 1);
            var floor = new FloorState(map, start, new GridPosition(5, 5));
            floor.AddEnemy(new CharacterState(1, "A", new GridPosition(3, 3), 10, 3, 0));
            floor.AddEnemy(new CharacterState(2, "B", new GridPosition(4, 3), 10, 3, 0));
            var state = new RunState(new CharacterState(0, "Player", start, 20, 5, 1), floor);
            var brain = new CountingBrain(); var turns = new TurnProcessor(new ActionResolver(), brain, new Random(1));
            Assert.That(turns.Execute(state, new PlayerCommand(CommandKind.Move, new GridPosition(-1, 0))).ConsumesTurn, Is.False);
            Assert.That(brain.Calls, Is.Zero); Assert.That(state.TurnNumber, Is.Zero);
            Assert.That(turns.Execute(state, new PlayerCommand(CommandKind.Move, new GridPosition(1, 0))).ConsumesTurn, Is.True);
            Assert.That(brain.Calls, Is.EqualTo(2)); Assert.That(state.TurnNumber, Is.EqualTo(1));
            turns.Execute(state, new PlayerCommand(CommandKind.Wait));
            Assert.That(brain.Calls, Is.EqualTo(4)); Assert.That(state.TurnNumber, Is.EqualTo(2));
            state.Player.TakeDamage(100);
            turns.Execute(state, new PlayerCommand(CommandKind.Wait));
            Assert.That(state.TurnNumber, Is.EqualTo(2));
        }
    }
}
