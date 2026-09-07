using System;
using System.Linq;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed class HudPresenter
    {
        public VisualElement Root { get; }
        public VisualElement Map { get; }
        public VisualElement Pack { get; }
        public Toggle Animate { get; }
        private readonly Label stats, healthText, equipment, floor, log, context, deathSummary, outcomeTitle, outcomeDetail, objective;
        private readonly VisualElement healthFill, death;
        private readonly Button pickup, descend;
        private readonly Label inspector;
        private int inspectedId = -1;
        private RunState current;
        public HudPresenter(VisualElement root, Action<PlayerCommand> execute, Action togglePack, Action restart, Action openMenu)
        {
            root.style.flexGrow = 1; root.style.alignItems = Align.Center;
            var shell = new VisualElement(); root.Add(shell); root = shell;
            Root = root; root.AddToClassList("game");
            var header = Box(root, "header");
            var identity = Box(header, "identity"); Text(identity, "LANTERN DEPTHS", "title"); Text(identity, "A turn-based descent into the ember vaults", "muted");
            var navigation = Box(header, "identity"); floor = Text(navigation, "", "floor-number");
            InventoryPresenter.AddButton(navigation, "Menu [Esc / Start]", openMenu);
            var body = Box(root, "body"); var world = Box(body, "world");
            var bar = Box(world, "map-heading"); Text(bar, "THE EMBER VAULTS", "section-title"); context = Text(bar, "", "muted");
            var frame = Box(world, "map-frame"); Map = Box(frame, "map");
            Text(world, "@ YOU   m MELEE / r ARCHER / B GUARDIAN   ! TONIC   * BLAST   ? RETREAT   / WEAPON   ] ARMOR   > STAIRS", "legend");
            var sidebar = Box(body, "sidebar"); Text(sidebar, "WAYFARER", "section-title");
            healthText = Text(sidebar, "", "health-text"); var health = Box(sidebar, "health-track"); healthFill = Box(health, "health-fill");
            stats = Text(sidebar, "", "stats"); equipment = Text(sidebar, "", "equipment");
            objective = Text(sidebar, "", "muted");
            var actions = Box(sidebar, "actions");
            var firstRow = Box(actions, "button-row"); var secondRow = Box(actions, "button-row");
            InventoryPresenter.AddButton(firstRow, "Pack [I / Y]", togglePack);
            InventoryPresenter.AddButton(firstRow, "Wait [Space / A]", () => execute(new PlayerCommand(CommandKind.Wait)));
            pickup = InventoryPresenter.AddButton(secondRow, "Pick up [G / X]", () => execute(new PlayerCommand(CommandKind.PickUp)));
            descend = InventoryPresenter.AddButton(secondRow, "Down [Enter / LB]", () => execute(new PlayerCommand(CommandKind.Descend)));
            InventoryPresenter.AddButton(sidebar, "Inspect [Tab / RS]", CycleEnemy);
            inspector = Text(sidebar, "", "muted");
            Animate = new Toggle("Animate turns [F / Back]") { value = true, focusable = false };
            var logPanel = Box(root, "log-panel"); Text(logPanel, "JOURNAL", "section-title"); log = Text(logPanel, "", "log");
            Pack = Box(root, "pack-panel");
            death = Box(root, "death-overlay"); var card = Box(death, "death-card");
            outcomeTitle = Text(card, "", "title"); deathSummary = Text(card, "", "death-summary");
            outcomeDetail = Text(card, "", "muted");
            InventoryPresenter.AddButton(card, "Begin a new descent [R]", restart);
            InventoryPresenter.AddButton(card, "Menu [Esc / Start]", openMenu); death.style.display = DisplayStyle.None;
        }
        public void Refresh(RunController game, MessageLog messages)
        {
            var state = game.State; var p = state.Player;
            current = state; RefreshEnemy();
            floor.text = $"FLOOR {state.FloorNumber:00} / {game.FinalFloor:00}  ·  {new[] { "Entrance", "Wisp passages", "Old armory", "Ember approach", "Guardian vault" }[Math.Min(state.FloorNumber - 1, 4)]}";
            bool finalFloor = state.FloorNumber == game.FinalFloor;
            objective.text = finalFloor ? "Recover the ember at the stairs to win." : $"Reach floor {game.FinalFloor} and recover the ember.";
            descend.text = finalFloor ? "Claim [Enter / LB]" : "Down [Enter / LB]";
            healthText.text = $"HP  {p.Hp} / {p.MaxHp}"; healthFill.style.width = Length.Percent(100f * p.Hp / p.MaxHp);
            healthFill.EnableInClassList("low-health", p.Hp * 3 <= p.MaxHp);
            int next = state.Progression.NextThreshold(p);
            stats.text = $"LEVEL  {p.Level}\nEXP  {state.Progression.Experience} / {(next < 0 ? "MAX" : next.ToString())}\nATK  {StatCalculator.Attack(state, p)}     DEF  {StatCalculator.Defense(state, p)}\nTURN  {state.TurnNumber}";
            equipment.text = $"WEAPON  {state.Inventory.Find(state.Equipment.WeaponId)?.Definition.Name ?? "None"}\nARMOR  {state.Inventory.Find(state.Equipment.ArmorId)?.Definition.Name ?? "None"}";
            var item = state.Floor.GetItemAt(p.Position);
            context.text = item != null ? item.Definition.Name : p.Position == state.Floor.Stairs ? (finalFloor ? "Claim the ember" : "Stairs down") : "Choose your next step";
            pickup.SetEnabled(item != null && state.Inventory.Items.Count < state.Inventory.Capacity);
            descend.SetEnabled(p.Position == state.Floor.Stairs);
            log.text = messages.ToString();
            death.style.display = state.IsFinished ? DisplayStyle.Flex : DisplayStyle.None;
            outcomeTitle.text = state.IsVictory ? "THE EMBER RETURNS" : "THE LANTERN FADES";
            outcomeDetail.text = state.IsVictory ? "You reclaimed the ember and found your way home.\nDescent complete." : "Your pack and progress are lost.\nA new descent awaits.";
            deathSummary.text = $"Floor {state.FloorNumber}  /  Level {p.Level}  /  {state.TurnNumber} turns\nSeed {game.Seed}";
        }
        public void Inspect(int id) { inspectedId = id; RefreshEnemy(); LocalText.Refresh(Root); }
        public void CycleEnemy()
        {
            if (current == null) return;
            var enemies = current.Floor.Enemies.Where(e => e.IsAlive).OrderBy(e => e.Id).ToArray();
            if (enemies.Length == 0) return;
            inspectedId = enemies[(Array.FindIndex(enemies, e => e.Id == inspectedId) + 1) % enemies.Length].Id;
            RefreshEnemy(); LocalText.Refresh(Root);
        }
        private void RefreshEnemy()
        {
            if (current == null) return;
            var enemy = current.Floor.Enemies.FirstOrDefault(e => e.Id == inspectedId && e.IsAlive) ?? current.Floor.Enemies.Where(e => e.IsAlive).OrderBy(e => e.Position.Distance(current.Player.Position)).FirstOrDefault();
            inspectedId = enemy?.Id ?? -1;
            inspector.text = enemy == null ? "No enemies remain." : $"{enemy.Name}  ({enemy.Position.X},{enemy.Position.Y})\nHP {enemy.Hp}/{enemy.MaxHp}  ATK {enemy.BaseAttack}  DEF {enemy.BaseDefense}\n" +
                (enemy.Charged ? "STRIKE READY — move away!" : enemy.Role == EnemyRole.Archer ? "Archer: range 4, retreats when adjacent. Walls block shots." : enemy.Role == EnemyRole.Guardian ? "Guardian: warns, then strikes next turn. Step away to dodge." : "Melee: closes in and attacks adjacent tiles.");
        }
        private static VisualElement Box(VisualElement parent, string role) { var element = new VisualElement(); element.AddToClassList(role); parent.Add(element); return element; }
        private static Label Text(VisualElement parent, string text, string role) { var label = new Label(text); label.AddToClassList(role); parent.Add(label); return label; }
    }
}
