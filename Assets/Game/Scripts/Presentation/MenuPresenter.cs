using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed class MenuPresenter
    {
        private readonly VisualElement panel;
        private readonly Label message;
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<Action> actions = new List<Action>();
        private readonly Button resume, save, quitButton, backup, animation, confirm;
        private Action confirmed;
        private string previousMessage;
        private int selected;
        public bool IsOpen { get; private set; }
        public bool IsConfirming => confirmed != null;
        public MenuPresenter(VisualElement root, Action close, Action saveRun, Action quit, Action newRun, Action restoreBackup, Action toggleAnimation, Action utilities = null)
        {
            panel = new VisualElement(); panel.AddToClassList("death-overlay"); root.Add(panel);
            var card = new VisualElement(); card.AddToClassList("menu-card"); panel.Add(card);
            var title = new Label("LANTERN DEPTHS"); title.AddToClassList("title"); card.Add(title);
            message = new Label(); message.AddToClassList("menu-message"); card.Add(message);
            resume = Add(card, "Return to game [Esc / Start / B]", close);
            save = Add(card, "Save now", saveRun);
            quitButton = Add(card, "Save and quit", quit);
            Add(card, "Begin a new descent…", newRun);
            backup = Add(card, "Restore previous save…", restoreBackup);
            animation = Add(card, "", toggleAnimation);
            confirm = Add(card, "", () => { var action = confirmed; CancelConfirmation(); action?.Invoke(); });
            if (utilities != null) Add(card, "Settings / Help / Records", utilities);
            var help = new Label("↑ ↓ / D-pad    Enter / A    Esc / B");
            help.AddToClassList("muted"); card.Add(help); SetOpen(false);
        }
        private Button Add(VisualElement card, string label, Action action)
        {
            var button = InventoryPresenter.AddButton(card, label, action); buttons.Add(button); actions.Add(action); return button;
        }
        public void SetOpen(bool value)
        {
            IsOpen = value; panel.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            CancelConfirmation(); selected = 0; Select(0);
        }
        public void Status(string text) { message.text = text; }
        public void SetEnabled(bool enabled) => panel.SetEnabled(enabled);
        public void Refresh(bool canResume, bool hasBackup, bool animate)
        {
            resume.SetEnabled(canResume); save.SetEnabled(canResume); backup.SetEnabled(hasBackup);
            quitButton.text = canResume ? "Save and quit" : "Quit (keep existing save)";
            animation.text = $"Turn animation: {(animate ? "ON" : "OFF")} [F / Back]";
            Select(0);
        }
        public void Confirm(string text, string label, Action action)
        {
            CancelConfirmation(); previousMessage = message.text;
            message.text = text; confirmed = action; confirm.text = label; confirm.style.display = DisplayStyle.Flex;
            selected = buttons.IndexOf(confirm); Select(0);
        }
        public void CancelConfirmation()
        {
            if (confirmed != null) message.text = previousMessage;
            confirmed = null; confirm.style.display = DisplayStyle.None; Select(0);
        }
        public void Select(int delta)
        {
            selected = (selected + delta + buttons.Count) % buttons.Count;
            for (int i = 0; i < buttons.Count && !Available(buttons[selected]); i++)
                selected = (selected + (delta < 0 ? buttons.Count - 1 : 1)) % buttons.Count;
            for (int i = 0; i < buttons.Count; i++) buttons[i].EnableInClassList("selected", i == selected);
        }
        private static bool Available(Button button) => button.enabledSelf && button.style.display != DisplayStyle.None;
        public void Activate()
        {
            if (Available(buttons[selected])) actions[selected]();
        }
    }
}
