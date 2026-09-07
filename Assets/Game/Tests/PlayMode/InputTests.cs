using System.Collections.Generic;
using LanternDepths.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace LanternDepths.Tests
{
    public sealed class InputTests
    {
        [Test] public void PictogramsKeepFacingAndItemsAcrossSync()
        {
            var game = new RunController(new RunRules(40, 28, 8, 3, 30, 6, 1, new ProgressionRules(new[] { 12 }, 5, 2), new[] { new ItemDefinition("tonic", "Tonic", "Restores HP", ItemKind.Healing, 18) })); game.StartNewRun(12345);
            var root = new UnityEngine.UIElements.VisualElement();
            var grid = new GridPresenter(root);
            grid.Build(game.State);
            var player = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root, className: "player");
            grid.FacePlayer(new GridPosition(-1, 0));
            grid.Sync(game.State);
            Assert.That(player.GetType().GetProperty("Facing").GetValue(player), Is.EqualTo(Vector2.left));
            grid.FacePlayer(default);
            Assert.That(player.GetType().GetProperty("Facing").GetValue(player), Is.EqualTo(Vector2.left));
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(root).ToList(), Is.Empty);
            var enemy = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root, className: "enemy");
            Assert.That(enemy.focusable, Is.True);
            var item = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root, className: "item");
            Assert.That(item, Is.Not.Null);
            grid.Sync(game.State);
            Assert.That(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(root, className: "item"), Is.SameAs(item));
        }
        [Test] public void RemappedKeysAndPreferencesSurviveReloadAndHistoryDeduplicates()
        {
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LanternPreferences-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                string path = System.IO.Path.Combine(folder, "preferences.json");
                var preferences = new PlayerPreferences { japanese = false, scale = 120, volume = 0, tutorialSeen = true };
                preferences.keys[0] = (int)KeyCode.T; preferences.Write(path); preferences = PlayerPreferences.Read(path);
                Assert.That(preferences.scale, Is.EqualTo(120)); Assert.That(preferences.volume, Is.Zero); Assert.That(preferences.japanese, Is.False);
                var keys = new HashSet<KeyCode> { KeyCode.T }; var adapter = new PlayerInputAdapter(keys.Contains, keys.Contains, _ => 0) { Map = preferences.Map };
                adapter.TryRead(false, out var move); Assert.That(move.Direction, Is.EqualTo(new GridPosition(0, 1)));
                keys.Clear(); keys.Add(KeyCode.W); Assert.That(adapter.TryRead(false, out _), Is.False);
                preferences.keys[1] = preferences.keys[0]; preferences.Write(path); Assert.Throws<System.IO.InvalidDataException>(() => PlayerPreferences.Read(path));
                var rules = new RunRules(40, 28, 8, 0, 30, 6, 1, new ProgressionRules(new[] { 12 }, 5, 2)); var game = new RunController(rules); game.StartNewRun(2);
                var history = new PlayerHistory(); history.Record(game); history.Record(game); Assert.That(history.runs.Count, Is.EqualTo(1));
                path = System.IO.Path.Combine(folder, "history.json"); history.Write(path); Assert.That(PlayerHistory.Read(path).runs[0].id, Is.EqualTo(game.State.RunId));
                var log = new MessageLog(); for (int i = 0; i < 210; i++) log.Append(i.ToString());
                Assert.That(log.FullText.Split('\n').Length, Is.EqualTo(200)); Assert.That(log.ToString().Split('\n').Length, Is.EqualTo(5));
                LocalText.Japanese = true; Assert.That(LocalText.T("Wayfarer hits Cinder Mite for 3."), Does.Contain("3 ダメージ"));
            }
            finally { if (System.IO.Directory.Exists(folder)) System.IO.Directory.Delete(folder, true); }
        }
        [Test] public void NumpadAndKeyboardProduceEightDirections()
        {
            var keys = new HashSet<KeyCode>(); var adapter = new PlayerInputAdapter(keys.Contains, keys.Contains, _ => 0);
            foreach (var d in MovementRules.Directions)
            {
                keys.Clear(); keys.Add((KeyCode)((int)KeyCode.Keypad0 + (d.Y + 1) * 3 + d.X + 2));
                Assert.That(adapter.TryRead(false, out var command), Is.True);
                Assert.That(command.Kind, Is.EqualTo(CommandKind.Move)); Assert.That(command.Direction, Is.EqualTo(d));
            }
            keys.Clear(); keys.Add(KeyCode.W); keys.Add(KeyCode.D);
            Assert.That(adapter.TryRead(false, out var diagonal), Is.True); Assert.That(diagonal.Direction, Is.EqualTo(new GridPosition(1, 1)));
            keys.Clear(); keys.Add(KeyCode.Q);
            adapter.TryRead(false, out diagonal); Assert.That(diagonal.Direction, Is.EqualTo(new GridPosition(-1, 1)));
            keys.Clear(); keys.Add(KeyCode.Space); adapter.TryRead(false, out var wait); Assert.That(wait.Kind, Is.EqualTo(CommandKind.Wait));
            Assert.That(adapter.TryRead(true, out _), Is.False);
        }
        [Test] public void HeldMovementRepeatsAfterTheInitialDelay()
        {
            var down = new HashSet<KeyCode>(); var held = new HashSet<KeyCode>(); float time = 0;
            var adapter = new PlayerInputAdapter(down.Contains, held.Contains, _ => 0, () => time);
            down.Add(KeyCode.W); held.Add(KeyCode.W);
            Assert.That(adapter.TryRead(false, out var first), Is.True); Assert.That(first.Direction, Is.EqualTo(new GridPosition(0, 1)));
            down.Clear(); time = 0.24f; Assert.That(adapter.TryRead(false, out _), Is.False);
            time = 0.25f; Assert.That(adapter.TryRead(false, out var repeat), Is.True); Assert.That(repeat.Direction, Is.EqualTo(new GridPosition(0, 1)));
            time = 0.34f; Assert.That(adapter.TryRead(false, out _), Is.False);
            time = 0.35f; Assert.That(adapter.TryRead(false, out _), Is.True);
            held.Clear(); time = 0.36f; Assert.That(adapter.TryRead(false, out _), Is.False);
            down.Add(KeyCode.D); held.Add(KeyCode.D); Assert.That(adapter.TryRead(false, out var changed), Is.True); Assert.That(changed.Direction, Is.EqualTo(new GridPosition(1, 0)));
        }
        [Test] public void GamepadEdgesAndMenuInputDoNotRepeatMovement()
        {
            var keys = new HashSet<KeyCode>(); var axes = new Dictionary<string, float>();
            var adapter = new PlayerInputAdapter(keys.Contains, keys.Contains, name => axes.TryGetValue(name, out var value) ? value : 0);
            axes["GamepadHorizontal"] = 1; axes["GamepadVertical"] = -1;
            Assert.That(adapter.TryRead(false, out var move), Is.True); Assert.That(move.Direction, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(adapter.TryRead(false, out _), Is.False);
            axes["GamepadVertical"] = 0;
            Assert.That(adapter.TryRead(false, out _), Is.False, "Releasing one diagonal axis must not add a turn.");
            axes.Clear(); adapter.TryRead(false, out _); axes["DpadHorizontal"] = -1;
            Assert.That(adapter.TryRead(true, out _), Is.False); Assert.That(adapter.TryRead(false, out _), Is.False);
            axes.Clear(); adapter.TryRead(false, out _); keys.Add(KeyCode.JoystickButton2);
            adapter.TryRead(false, out var pick); Assert.That(pick.Kind, Is.EqualTo(CommandKind.PickUp)); Assert.That(adapter.EquipPressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.JoystickButton3); Assert.That(adapter.InventoryPressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.JoystickButton4); adapter.TryRead(false, out var action); Assert.That(action.Kind, Is.EqualTo(CommandKind.Context));
            keys.Clear(); keys.Add(KeyCode.DownArrow); adapter.TryRead(true, out _); Assert.That(adapter.SelectionDelta, Is.EqualTo(1));
            keys.Clear(); keys.Add(KeyCode.JoystickButton7); Assert.That(adapter.RestartPressed, Is.True);
            Assert.That(adapter.MenuPressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.Escape); Assert.That(adapter.MenuPressed && adapter.ClosePressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.Return); Assert.That(adapter.ConfirmPressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.JoystickButton0); Assert.That(adapter.ConfirmPressed, Is.True);
        }
    }
}
