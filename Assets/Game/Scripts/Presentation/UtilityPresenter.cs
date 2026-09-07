using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed class UtilityPresenter
    {
        private readonly VisualElement overlay;
        private readonly ScrollView content;
        private readonly Label title;
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<Action> actions = new List<Action>();
        private int selected;
        public bool IsOpen => overlay.style.display != DisplayStyle.None;
        public UtilityPresenter(VisualElement root)
        {
            overlay = new VisualElement(); overlay.AddToClassList("death-overlay"); root.Add(overlay);
            var card = new VisualElement(); card.AddToClassList("utility-card"); overlay.Add(card);
            title = new Label(); title.AddToClassList("title"); card.Add(title);
            content = new ScrollView(); content.style.flexGrow = 1; card.Add(content); Close();
        }
        public void Open(string heading)
        {
            title.text = LocalText.T(heading); content.Clear(); buttons.Clear(); actions.Clear(); selected = 0;
            overlay.style.display = DisplayStyle.Flex; overlay.BringToFront();
        }
        public void Text(string text) { var label = new Label(LocalText.T(text)); label.AddToClassList("description"); content.Add(label); }
        public void Add(string text, Action action)
        {
            var button = InventoryPresenter.AddButton(content, LocalText.T(text), action); buttons.Add(button); actions.Add(action); Select(0);
        }
        public void Close() => overlay.style.display = DisplayStyle.None;
        public void Select(int delta)
        {
            if (buttons.Count == 0) return;
            if (buttons.Count == 1 && delta != 0) { content.scrollOffset += new UnityEngine.Vector2(0, delta * 180); return; }
            selected = (selected + delta + buttons.Count) % buttons.Count;
            for (int i = 0; i < buttons.Count; i++) buttons[i].EnableInClassList("selected", selected == i);
            content.ScrollTo(buttons[selected]);
        }
        public void Activate() { if (buttons.Count > 0) actions[selected](); }
    }
}
