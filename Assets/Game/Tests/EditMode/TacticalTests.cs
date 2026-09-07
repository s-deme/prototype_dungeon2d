using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace LanternDepths.Tests
{
    public sealed class TacticalTests
    {
        internal static ItemDefinition[] Items() => ItemTests.Definitions().Concat(new[] {
            new ItemDefinition("retreat-rune", "Retreat Rune", "", ItemKind.Blink, 4),
            new ItemDefinition("blast-flask", "Blast Flask", "", ItemKind.Blast, 8),
            new ItemDefinition("heavy-blade", "Heavy Blade", "", ItemKind.Weapon, 6, 2),
            new ItemDefinition("plate-armor", "Plate Armor", "", ItemKind.Armor, 5, 2) }).ToArray();
        private static RunState State()
        {
            var player = new CharacterState(0, "P", new GridPosition(2, 2), 30, 6, 1);
            return new RunState(player, new FloorState(GridTests.OpenMap(10), player.Position, new GridPosition(8, 8)), new PlayerProgression(new ProgressionRules(new[] { 12, 30 }, 5, 2)));
        }
        [Test] public void ArcherShootsRetreatsAndCannotShootThroughWalls()
        {
            var s = State(); var enemy = new CharacterState(1, "Archer", new GridPosition(5, 2), 8, 4, 0, role: EnemyRole.Archer); s.Floor.AddEnemy(enemy);
            var brain = new BasicEnemyBrain(); var resolver = new ActionResolver();
            var command = brain.ChooseAction(s, enemy, new Random(1)); Assert.That(command.Kind, Is.EqualTo(CommandKind.Shoot));
            resolver.Resolve(s, enemy, command); Assert.That(s.Player.Hp, Is.EqualTo(27));
            s.Floor.Map.SetTile(new GridPosition(4, 2), new TileData(Terrain.Wall));
            Assert.That(resolver.Resolve(s, enemy, command).ConsumesTurn, Is.False); Assert.That(s.Player.Hp, Is.EqualTo(27));
            enemy.Position = new GridPosition(3, 2); Assert.That(brain.ChooseAction(s, enemy, new Random(1)).Kind, Is.EqualTo(CommandKind.Shoot));
            s.Floor.Map.SetTile(new GridPosition(4, 2), new TileData(Terrain.Room, 0)); command = brain.ChooseAction(s, enemy, new Random(1));
            Assert.That(command.Kind, Is.EqualTo(CommandKind.Move)); Assert.That((enemy.Position + command.Direction).Distance(s.Player.Position), Is.GreaterThan(1));
        }
        [Test] public void GuardianWarnsCanBeDodgedAndKeepsChargeAcrossSave()
        {
            var s = State(); var enemy = new CharacterState(1, "Guardian", new GridPosition(3, 2), 48, 12, 3, 40, EnemyRole.Guardian); s.Floor.AddEnemy(enemy);
            var brain = new BasicEnemyBrain(); var resolver = new ActionResolver();
            var warning = resolver.Resolve(s, enemy, brain.ChooseAction(s, enemy, new Random(1)));
            Assert.That(warning.Events.Single().Kind, Is.EqualTo(EventKind.Telegraph)); Assert.That(s.Player.Hp, Is.EqualTo(30));
            s.Player.Position = new GridPosition(1, 2);
            resolver.Resolve(s, enemy, brain.ChooseAction(s, enemy, new Random(1))); Assert.That(s.Player.Hp, Is.EqualTo(30)); Assert.That(enemy.Charged, Is.False);
            s.Player.Position = new GridPosition(2, 2);
            resolver.Resolve(s, enemy, brain.ChooseAction(s, enemy, new Random(1))); resolver.Resolve(s, enemy, brain.ChooseAction(s, enemy, new Random(1)));
            Assert.That(s.Player.Hp, Is.EqualTo(19));
            var rules = new RunRules(40, 28, 8, 6, 30, 6, 1, new ProgressionRules(new[] { 12 }, 5, 2), Items());
            var game = new RunController(rules); game.StartNewRun(9);
            for (int i = 1; i < 5; i++) { game.State.Player.Position = game.State.Floor.Stairs; game.Execute(new PlayerCommand(CommandKind.Descend)); }
            var boss = game.State.Floor.Enemies.Single(e => e.Role == EnemyRole.Guardian); boss.Charged = true;
            game.Load(game.Save()); Assert.That(game.State.Floor.Enemies.Single(e => e.Role == EnemyRole.Guardian).Charged, Is.True);
            game.State.Player.Position = game.State.Floor.Stairs;
            Assert.That(game.Execute(new PlayerCommand(CommandKind.Descend)).ConsumesTurn, Is.False);
            game.State.Floor.Enemies.Single(e => e.Role == EnemyRole.Guardian).TakeDamage(48);
            Assert.That(game.Execute(new PlayerCommand(CommandKind.Descend)).ConsumesTurn, Is.True); Assert.That(game.State.IsVictory, Is.True);
        }
        [Test] public void TacticalItemsConsumeOnlyOnSuccessAndEquipmentUsesBothStats()
        {
            var s = State(); var resolver = new ItemActionResolver();
            s.Inventory.TryAdd(new ItemInstance(1, Items()[3])); s.Inventory.TryAdd(new ItemInstance(2, Items()[4]));
            Assert.That(resolver.Resolve(s, new PlayerCommand(CommandKind.Use, itemId: 1)).ConsumesTurn, Is.False);
            Assert.That(resolver.Resolve(s, new PlayerCommand(CommandKind.Use, itemId: 2)).ConsumesTurn, Is.False);
            var a = new CharacterState(1, "A", new GridPosition(3, 2), 8, 1, 0, 6); var b = new CharacterState(2, "B", new GridPosition(4, 2), 8, 1, 0, 6); s.Floor.AddEnemy(a); s.Floor.AddEnemy(b);
            var before = s.Player.Position;
            Assert.That(resolver.Resolve(s, new PlayerCommand(CommandKind.Use, itemId: 1)).ConsumesTurn, Is.True);
            Assert.That(s.Player.Position.Distance(a.Position), Is.GreaterThan(before.Distance(a.Position))); Assert.That(s.Inventory.Find(1), Is.Null);
            s.Player.Position = before;
            resolver.Resolve(s, new PlayerCommand(CommandKind.Use, itemId: 2));
            Assert.That(a.IsAlive || b.IsAlive, Is.False); Assert.That(s.Progression.Experience, Is.EqualTo(12)); Assert.That(s.Player.Level, Is.EqualTo(2));
            Assert.That(resolver.Resolve(s, new PlayerCommand(CommandKind.Use, itemId: 2)).ConsumesTurn, Is.False); Assert.That(s.Progression.Experience, Is.EqualTo(12));
            s.Inventory.TryAdd(new ItemInstance(3, Items()[5])); s.Inventory.TryAdd(new ItemInstance(4, Items()[6]));
            resolver.Resolve(s, new PlayerCommand(CommandKind.Equip, itemId: 3)); Assert.That(StatCalculator.Defense(s, s.Player), Is.Zero);
            resolver.Resolve(s, new PlayerCommand(CommandKind.Equip, itemId: 4)); Assert.That(StatCalculator.Attack(s, s.Player), Is.EqualTo(12)); Assert.That(StatCalculator.Defense(s, s.Player), Is.EqualTo(4));
        }
        [Test] public void RealVersionOneSaveMigratesWithProgressAndFutureContentIntact()
        {
            var rules = new RunRules(40, 28, 8, 6, 30, 6, 1, new ProgressionRules(new[] { 12, 30, 60, 100, 160, 240, 340, 460, 600 }, 5, 2), Items());
            string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "run-v1.bytes");
            if (!File.Exists(path)) path = "Assets/Game/Tests/Fixtures/run-v1.bytes";
            byte[] legacy = File.ReadAllBytes(path); var game = new RunController(rules); game.Load(legacy);
            Assert.That(game.State.FloorNumber, Is.EqualTo(1)); Assert.That(game.State.TurnNumber, Is.EqualTo(1));
            Assert.That(game.State.Floor.Enemies.All(e => e.Role == EnemyRole.Melee && !e.Charged), Is.True);
            byte[] current = game.Save(); Assert.That(current, Is.Not.EqualTo(legacy)); game.Load(current); Assert.That(game.Save(), Is.EqualTo(current));
            game.State.Player.Position = game.State.Floor.Stairs; game.Execute(new PlayerCommand(CommandKind.Descend));
            Assert.That(game.State.Floor.Enemies.Any(e => e.Role == EnemyRole.Archer), Is.True);
            Assert.That(game.State.Floor.Items.Any(i => i.Value.Definition.Kind == ItemKind.Blast), Is.True);
        }
    }
}
