using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LanternDepths.Tests
{
    public sealed class SaveTests
    {
        private static RunRules Rules() => new RunRules(40, 28, 8, 6, 30, 6, 1,
            new ProgressionRules(new[] { 12, 30, 60 }, 5, 2), ItemTests.Definitions());
        [Test] public void OnlyOneSessionCanOwnTheSaveFolderAndClosingReleasesIt()
        {
            string directory = Path.Combine(Path.GetTempPath(), "LanternLockTest-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (SaveFile.OpenSession(directory)) Assert.Throws<IOException>(() => { using var duplicate = SaveFile.OpenSession(directory); });
                using var reopened = SaveFile.OpenSession(directory);
                Assert.That(reopened.CanWrite, Is.True);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test] public void SnapshotRestoresProgressItemsMapAndFutureRandomOutcomes()
        {
            var rules = Rules(); var a = new RunController(rules); a.StartNewRun(913);
            a.State.Progression.AddExperience(a.State.Player, 32, new ActionResult());
            a.State.Player.TakeDamage(7);
            a.State.Inventory.TryAdd(new ItemInstance(50, rules.Items[1]));
            a.State.Equipment.Equip(a.State.Inventory.Find(50));
            // Enemies can move onto stairs/entrances after spawning; snapshots must allow that.
            a.State.Floor.Enemies[0].Position = a.State.Floor.Stairs;
            a.State.Floor.Enemies[1].TakeDamage(100);
            byte[] saved = a.Save(); var b = new RunController(rules); b.Load(saved);
            Assert.That(b.Save(), Is.EqualTo(saved));
            for (int i = 0; i < 50; i++)
            {
                var command = new PlayerCommand(CommandKind.Move, MovementRules.Directions.ElementAt(i % 8));
                var left = a.Execute(command); var right = b.Execute(command);
                Assert.That(right.Events.Select(e => e.Message), Is.EqualTo(left.Events.Select(e => e.Message)));
                Assert.That(b.Save(), Is.EqualTo(a.Save()));
                if (a.State.IsFinished) break;
            }
            // Check future generation as well as enemy random choices.
            a.StartNewRun(88); b.Load(a.Save());
            a.State.Player.Position = a.State.Floor.Stairs; b.State.Player.Position = b.State.Floor.Stairs;
            a.Execute(new PlayerCommand(CommandKind.Descend)); b.Execute(new PlayerCommand(CommandKind.Descend));
            Assert.That(b.Save(), Is.EqualTo(a.Save()));
            a.State.Player.TakeDamage(a.State.Player.MaxHp); b.Load(a.Save());
            Assert.That(b.State.IsGameOver, Is.True);
            a.StartNewRun(3);
            for (int i = 0; i < rules.FinalFloor; i++)
            { foreach (var enemy in a.State.Floor.Enemies.Where(e => e.Role == EnemyRole.Guardian)) enemy.TakeDamage(enemy.MaxHp); a.State.Player.Position = a.State.Floor.Stairs; a.Execute(new PlayerCommand(CommandKind.Descend)); }
            b.Load(a.Save()); Assert.That(b.State.IsVictory, Is.True);
            Assert.That(b.Execute(new PlayerCommand(CommandKind.Wait)).ConsumesTurn, Is.False);
        }
        [Test] public void BadOrIncompatibleSaveNeverReplacesTheCurrentRun()
        {
            var game = new RunController(Rules()); game.StartNewRun(4); var original = game.State;
            byte[] valid = game.Save(), damaged = (byte[])valid.Clone(); damaged[damaged.Length / 2] ^= 1;
            Assert.Throws<InvalidDataException>(() => game.Load(damaged));
            Assert.Throws<InvalidDataException>(() => game.Load(new byte[10]));
            var other = new RunController(GenerationTests.Rules()); other.StartNewRun(4);
            Assert.Throws<InvalidDataException>(() => game.Load(other.Save()));
            Assert.That(game.State, Is.SameAs(original));
            Assert.That(game.Save(), Is.EqualTo(valid));
        }
        [Test] public void AtomicSaveKeepsBackupAndPreservesLastSaveOnWriteFailure()
        {
            string directory = Path.Combine(Path.GetTempPath(), "LanternSaveTest-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "run.sav");
            try
            {
                var game = new RunController(Rules()); game.StartNewRun(7); byte[] first = game.Save(); SaveFile.Write(path, first);
                game.Execute(new PlayerCommand(CommandKind.Wait)); byte[] second = game.Save(); SaveFile.Write(path, second);
                Assert.That(SaveFile.Read(path), Is.EqualTo(second)); Assert.That(SaveFile.Read(path + ".bak"), Is.EqualTo(first));
                Directory.CreateDirectory(path + ".tmp");
                Assert.That(() => SaveFile.Write(path, first), Throws.Exception);
                Assert.That(SaveFile.Read(path), Is.EqualTo(second)); Assert.That(SaveFile.Read(path + ".bak"), Is.EqualTo(first));
                game.Load(SaveFile.Read(path + ".bak")); Assert.That(game.State.TurnNumber, Is.Zero);
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
        [Test] public void RandomStateHasStableSequenceAndResumesAcrossAllUsedOverloads()
        {
            var random = new RunRandom(1);
            random.Next(); Assert.That(random.State, Is.EqualTo(270369u));
            random.Next(); Assert.That(random.State, Is.EqualTo(67634689u));
            var resumed = new RunRandom(random.State);
            for (int i = 0; i < 100; i++) Assert.That(resumed.Next(-10, 10), Is.EqualTo(random.Next(-10, 10)));
            Assert.That(resumed.NextDouble(), Is.EqualTo(random.NextDouble()));
            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(-1));
        }
    }
}
