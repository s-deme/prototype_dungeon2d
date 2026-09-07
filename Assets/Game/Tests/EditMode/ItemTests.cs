using NUnit.Framework;

namespace LanternDepths.Tests
{
    public sealed class ItemTests
    {
        internal static ItemDefinition[] Definitions() => new[]
        {
            new ItemDefinition("ember-tonic", "Ember Tonic", "Restores 18 HP.", ItemKind.Healing, 18),
            new ItemDefinition("copper-edge", "Copper Edge", "Attack +3.", ItemKind.Weapon, 3),
            new ItemDefinition("woven-guard", "Woven Guard", "Defense +2.", ItemKind.Armor, 2)
        };
        private static RunState State()
        {
            var p = new GridPosition(2, 2);
            return new RunState(new CharacterState(0, "P", p, 30, 6, 1), new FloorState(GridTests.OpenMap(), p, new GridPosition(5, 5)));
        }
        [Test] public void InventoryCapacityAndFailedPickupPreserveGroundItem()
        {
            var state = State(); var definition = Definitions()[0];
            for (int i = 1; i <= 20; i++) Assert.That(state.Inventory.TryAdd(new ItemInstance(i, definition)), Is.True);
            var ground = new ItemInstance(21, definition); state.Floor.PlaceItem(state.Player.Position, ground);
            var resolver = new ActionResolver();
            Assert.That(resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.PickUp)).ConsumesTurn, Is.False);
            Assert.That(state.Floor.GetItemAt(state.Player.Position), Is.SameAs(ground));
            Assert.That(state.Inventory.TryAdd(new ItemInstance(1, definition)), Is.False);
        }
        [Test] public void HealingPickupDropAndStaleCommandAreAtomic()
        {
            var state = State(); var resolver = new ActionResolver(); var item = new ItemInstance(1, Definitions()[0]);
            state.Floor.PlaceItem(state.Player.Position, item);
            Assert.That(resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.PickUp)).ConsumesTurn, Is.True);
            Assert.That(state.Floor.GetItemAt(state.Player.Position), Is.Null);
            var use = new PlayerCommand(CommandKind.Use, itemId: 1);
            Assert.That(resolver.Resolve(state, state.Player, use).ConsumesTurn, Is.False);
            state.Player.TakeDamage(25);
            Assert.That(resolver.Resolve(state, state.Player, use).ConsumesTurn, Is.True); Assert.That(state.Player.Hp, Is.EqualTo(23));
            Assert.That(state.Inventory.Items.Count, Is.Zero);
            Assert.That(resolver.Resolve(state, state.Player, use).ConsumesTurn, Is.False);
        }
        [Test] public void EquipmentSwapsAndFailedDropsKeepEquipmentIntact()
        {
            var state = State(); var definitions = Definitions(); var resolver = new ActionResolver();
            state.Inventory.TryAdd(new ItemInstance(1, definitions[1])); state.Inventory.TryAdd(new ItemInstance(2, definitions[2]));
            state.Inventory.TryAdd(new ItemInstance(3, new ItemDefinition("strong", "Strong edge", "", ItemKind.Weapon, 5)));
            resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.Equip, itemId: 1));
            resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.Equip, itemId: 2));
            Assert.That(StatCalculator.Attack(state, state.Player), Is.EqualTo(9)); Assert.That(StatCalculator.Defense(state, state.Player), Is.EqualTo(3));
            resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.Equip, itemId: 3));
            Assert.That(state.Equipment.WeaponId, Is.EqualTo(3)); Assert.That(state.Inventory.Items.Count, Is.EqualTo(3));
            state.Floor.PlaceItem(state.Player.Position, new ItemInstance(4, definitions[0]));
            Assert.That(resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.Drop, itemId: 3)).ConsumesTurn, Is.False);
            Assert.That(state.Equipment.WeaponId, Is.EqualTo(3));
            state.Player.Position = new GridPosition(3, 2);
            Assert.That(resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.Drop, itemId: 3)).ConsumesTurn, Is.True);
            Assert.That(state.Equipment.WeaponId, Is.EqualTo(-1)); Assert.That(state.Inventory.Find(3), Is.Null);
            Assert.That(state.Floor.GetItemAt(state.Player.Position).Id, Is.EqualTo(3));
            Assert.That(resolver.Resolve(state, state.Player, new PlayerCommand(CommandKind.Unequip, itemId: 2)).ConsumesTurn, Is.True);
            Assert.That(StatCalculator.Defense(state, state.Player), Is.EqualTo(1));
        }
        [Test] public void FloorChangePreservesInventoryEquipmentAndProgression()
        {
            var rules = new RunRules(40, 28, 8, 6, 30, 6, 1, new ProgressionRules(new[] { 12, 30 }, 5, 2), Definitions());
            var game = new RunController(rules); game.StartNewRun(12);
            var state = game.State; state.Inventory.TryAdd(new ItemInstance(1, Definitions()[1]));
            new ActionResolver().Resolve(state, state.Player, new PlayerCommand(CommandKind.Equip, itemId: 1));
            state.Progression.AddExperience(state.Player, 12, new ActionResult());
            state.Player.Position = state.Floor.Stairs; game.Execute(new PlayerCommand(CommandKind.Descend));
            Assert.That(state.Inventory.Find(1), Is.Not.Null); Assert.That(state.Equipment.WeaponId, Is.EqualTo(1));
            Assert.That(state.Player.Level, Is.EqualTo(2)); Assert.That(state.Progression.Experience, Is.EqualTo(12));
            var positions = new System.Collections.Generic.HashSet<GridPosition> { state.Floor.Entrance, state.Floor.Stairs };
            foreach (var enemy in state.Floor.Enemies) Assert.That(positions.Add(enemy.Position), Is.True);
            int count = 0; foreach (var pair in state.Floor.Items) { Assert.That(positions.Add(pair.Key), Is.True); count++; }
            Assert.That(count, Is.EqualTo(6));
            game.StartNewRun(12); Assert.That(game.State.Inventory.Items.Count, Is.Zero); Assert.That(game.State.Equipment.WeaponId, Is.EqualTo(-1));
        }
    }
}
