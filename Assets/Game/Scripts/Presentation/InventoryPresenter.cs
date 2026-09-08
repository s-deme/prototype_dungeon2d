using System;
using System.Linq;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed class InventoryPresenter
    {
        private readonly VisualElement panel;
        private readonly ScrollView list;
        private readonly Label description;
        private readonly Label title;
        private readonly Button use, equip, drop;
        private readonly Action<PlayerCommand> execute;
        private RunState state;
        private int selected;
        private ItemInstance[] ordered = Array.Empty<ItemInstance>();
        public bool IsOpen { get; private set; }
        public InventoryPresenter(VisualElement panel, Action<PlayerCommand> execute, Action close)
        {
            this.panel = panel; this.execute = execute;
            panel = new VisualElement(); panel.AddToClassList("pack-card"); this.panel.Add(panel);
            title = new Label("PACK"); title.AddToClassList("section-title"); panel.Add(title);
            list = new ScrollView(); list.AddToClassList("inventory-list"); panel.Add(list);
            description = new Label(); description.AddToClassList("description"); panel.Add(description);
            var actions = new VisualElement(); actions.AddToClassList("button-row"); panel.Add(actions);
            use = AddButton(actions, "Use [U / A]", () => Act(CommandKind.Use));
            equip = AddButton(actions, "Equip [E / X]", EquipSelected);
            drop = AddButton(actions, "Drop [D / RB]", () => Act(CommandKind.Drop));
            AddButton(panel, "Close pack [I / B]", close);
            SetOpen(false);
        }
        internal static Button AddButton(VisualElement parent, string text, Action clicked)
        {
            var button = new Button(clicked) { text = text, focusable = false }; parent.Add(button); return button;
        }
        public void SetOpen(bool value) { IsOpen = value; panel.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; }
        public void Refresh(RunState state)
        {
            int previousId = Selected?.Id ?? -1;
            this.state = state; list.Clear(); ordered = state.Inventory.Items.OrderBy(i => i.Definition.Kind).ThenBy(i => i.Definition.Name).ThenBy(i => i.Id).ToArray();
            var items = ordered;
            int previousIndex = Array.FindIndex(items, i => i.Id == previousId); if (previousIndex >= 0) selected = previousIndex;
            selected = Math.Max(0, Math.Min(selected, items.Length - 1));
            title.text = $"PACK  {items.Length} / {state.Inventory.Capacity}";
            if (items.Length == 0) list.Add(new Label("Your pack is empty.\nStand on an item and press G to collect it."));
            Button selectedButton = null;
            for (int i = 0; i < items.Length; i++)
            {
                int index = i; var item = items[i];
                if (i == 0 || items[i - 1].Definition.Kind != item.Definition.Kind) list.Add(new Label(LocalText.T(item.Definition.Kind.ToString())));
                var button = AddButton(list, $"{(i == selected ? "> " : "  ")}{item.Definition.Name}{(state.Equipment.IsEquipped(item.Id) ? " [equipped]" : "")}", () => { selected = index; Refresh(state); });
                button.AddToClassList("inventory-item");
                if (i == selected) { button.AddToClassList("selected"); selectedButton = button; }
            }
            if (selectedButton != null) list.schedule.Execute(() => list.ScrollTo(selectedButton));
            var chosen = Selected;
            description.text = chosen == null ? "Inventory browsing costs no turns." : chosen.Definition.Description;
            if (chosen != null && chosen.Definition.IsEquipment)
            {
                var candidate = chosen.Definition; bool weapon = candidate.Kind == ItemKind.Weapon;
                var newWeapon = weapon ? candidate : state.Inventory.Find(state.Equipment.WeaponId)?.Definition;
                var newArmor = weapon ? state.Inventory.Find(state.Equipment.ArmorId)?.Definition : candidate;
                int atk = Math.Max(0, state.Player.BaseAttack + (newWeapon?.Power ?? 0) - (newArmor?.Penalty ?? 0)) - StatCalculator.Attack(state, state.Player);
                int def = Math.Max(0, state.Player.BaseDefense + (newArmor?.Power ?? 0) - (newWeapon?.Penalty ?? 0)) - StatCalculator.Defense(state, state.Player);
                description.text += $"\nATK {atk:+0;-0;0}  /  DEF {def:+0;-0;0} (vs equipped)";
            }
            use.SetEnabled(chosen != null && !chosen.Definition.IsEquipment && (chosen.Definition.Kind != ItemKind.Healing || state.Player.Hp < state.Player.MaxHp));
            equip.SetEnabled(chosen != null && chosen.Definition.IsEquipment);
            equip.text = chosen != null && state.Equipment.IsEquipped(chosen.Id) ? "Remove [E / X]" : "Equip [E / X]";
            drop.SetEnabled(chosen != null && state.Player.Position != state.Floor.Stairs && state.Floor.GetItemAt(state.Player.Position) == null);
        }
        private ItemInstance Selected => selected >= 0 && selected < ordered.Length ? ordered[selected] : null;
        public void Select(int delta) { selected += delta; Refresh(state); }
        public void Act(CommandKind kind)
        {
            var item = Selected; if (item != null) execute(new PlayerCommand(kind, itemId: item.Id));
        }
        public void EquipSelected()
        {
            var item = Selected; if (item != null) Act(state.Equipment.IsEquipped(item.Id) ? CommandKind.Unequip : CommandKind.Equip);
        }
    }
}
