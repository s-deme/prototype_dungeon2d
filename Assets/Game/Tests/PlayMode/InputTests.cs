using System.Collections.Generic;
using LanternDepths.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace LanternDepths.Tests
{
    public sealed class InputTests
    {
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
            keys.Clear(); keys.Add(KeyCode.JoystickButton4); adapter.TryRead(false, out var stairs); Assert.That(stairs.Kind, Is.EqualTo(CommandKind.Descend));
            keys.Clear(); keys.Add(KeyCode.DownArrow); adapter.TryRead(true, out _); Assert.That(adapter.SelectionDelta, Is.EqualTo(1));
            keys.Clear(); keys.Add(KeyCode.JoystickButton7); Assert.That(adapter.RestartPressed, Is.True);
            Assert.That(adapter.MenuPressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.Escape); Assert.That(adapter.MenuPressed && adapter.ClosePressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.Return); Assert.That(adapter.ConfirmPressed, Is.True);
            keys.Clear(); keys.Add(KeyCode.JoystickButton0); Assert.That(adapter.ConfirmPressed, Is.True);
        }
    }
}
