#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LanternDepths.Presentation
{
    // Only compiled for development; validates the built player without changing normal gameplay.
    public sealed class DevelopmentSmoke : MonoBehaviour
    {
        private GameBootstrap game;
        public void Begin(GameBootstrap bootstrap) { DontDestroyOnLoad(gameObject); game = bootstrap; StartCoroutine(Run()); }
        private IEnumerator Run()
        {
            Application.runInBackground = true;
            string output = Path.GetFullPath("TestResults/PlayerSmoke");
            foreach (string arg in Environment.GetCommandLineArgs()) if (arg.StartsWith("--smoke-output=")) output = Path.GetFullPath(arg.Substring(15));
            Directory.CreateDirectory(output);
            Screen.SetResolution(1280, 900, FullScreenMode.Windowed);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--resume-check") >= 0)
            {
                Check(!game.MenuOpen && game.State.TurnNumber == 1 && !game.State.IsFinished, "Separate process did not resume the run.");
                yield return new WaitForSecondsRealtime(1);
                yield return Capture(output, "process-resumed");
                File.WriteAllText(Path.Combine(output, "resume-result.txt"), "PASS: new player process resumed the saved run.");
                Application.Quit(0); yield break;
            }
            yield return new WaitForSecondsRealtime(1);
            var root = game.GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement;
            File.WriteAllText(Path.Combine(output, "layout.txt"), $"root={root.layout}; children={root.childCount}; panel={root.panel != null}");
            Check(root.layout.width > 0 && root.layout.height > 0 && root.childCount > 0, "UI has no visible layout.");
            yield return Capture(output, "exploration-1280x900");
            Invoke("OpenHelp"); yield return Capture(output, "tutorial-japanese");
            Utility().Activate(); Check(game.State.TurnNumber == 0, "Tutorial consumed a turn.");
            Invoke("OpenSettings"); Utility().Select(2); Utility().Activate(); Utility().Select(2); Utility().Activate();
            yield return new WaitForSecondsRealtime(0.5f); yield return Capture(output, "settings-120");
            Utility().Close(); yield return Capture(output, "exploration-120");
            Invoke("OpenSettings"); Utility().Select(1); Utility().Activate(); Utility().Close();
            yield return Capture(output, "exploration-english");
            Invoke("OpenSettings"); Utility().Select(1); Utility().Activate(); Utility().Select(2); Utility().Activate();
            Utility().Select(5); Utility().Activate(); yield return Capture(output, "key-bindings"); Utility().Close();
            var prefs = PlayerPreferences.Read(Path.Combine(output, "Save", "preferences.json"));
            Check(prefs.japanese && prefs.scale == 100 && prefs.tutorialSeen, "Preferences did not persist.");
            // A second bootstrap uses the same save folder and must show a readable error.
            var duplicateObject = new GameObject("Duplicate session check"); duplicateObject.SetActive(false);
            var duplicate = duplicateObject.AddComponent<GameBootstrap>();
            foreach (string field in new[] { "rules", "theme" })
            {
                var info = typeof(GameBootstrap).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                info.SetValue(duplicate, info.GetValue(game));
            }
            duplicateObject.SetActive(true);
            duplicate.GetComponent<UnityEngine.UIElements.UIDocument>().sortingOrder = 100;
            Check(duplicate.State == null && !duplicate.enabled, "A duplicate session opened the save.");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture(output, "duplicate-session");
            Destroy(duplicateObject); yield return null; Debug.developerConsoleVisible = false;
            game.ToggleInventory();
            Check(game.InventoryOpen && game.State.TurnNumber == 0, "Opening inventory consumed a turn.");
            yield return Capture(output, "inventory-empty");
            game.ToggleInventory();
            var tonic = new ItemDefinition("smoke-tonic", "Ember Tonic", "Restores 18 HP.", ItemKind.Healing, 18);
            var weapon = new ItemDefinition("smoke-edge", "Copper Edge", "Attack +3 while equipped.", ItemKind.Weapon, 3);
            for (int i = 1; i <= 20; i++) game.State.Inventory.TryAdd(new ItemInstance(i, i == 1 ? weapon : tonic));
            game.ToggleInventory(); yield return Capture(output, "inventory-full"); game.ToggleInventory();
            game.Submit(new PlayerCommand(CommandKind.Equip, itemId: 1));
            int turn = game.State.TurnNumber;
            game.Submit(new PlayerCommand(CommandKind.Wait));
            Check(game.State.TurnNumber == turn, "Busy input advanced an extra turn.");
            while (game.IsBusy) yield return null;
            Check(game.State.Equipment.WeaponId == 1, "Equipment was not applied.");
            game.State.Player.TakeDamage(5);
            game.Submit(new PlayerCommand(CommandKind.Use, itemId: 2)); while (game.IsBusy) yield return null;
            Check(game.State.Inventory.Find(2) == null, "Healing item was not consumed.");
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture(output, "exploration-1280x720");
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(0.5f);
            yield return Capture(output, "exploration-1920x1080");
            game.State.Player.TakeDamage(game.State.Player.MaxHp);
            game.Submit(new PlayerCommand(CommandKind.Wait));
            Check(game.State.IsGameOver, "Death was not recorded.");
            yield return Capture(output, "game-over");
            game.Restart(); Check(game.State.TurnNumber == 0 && game.State.Inventory.Items.Count == 0 && !game.State.IsGameOver, "Restart did not reset the run.");
            yield return Capture(output, "restart");
            // Set up stairs directly; navigation is covered by the Domain integration tests.
            for (int floor = 0; floor < 100 && !game.State.IsFinished; floor++)
            {
                typeof(CharacterState).GetProperty(nameof(CharacterState.Position)).SetValue(game.State.Player, game.State.Floor.Stairs);
                var guardian = game.State.Floor.Enemies.FirstOrDefault(e => e.IsAlive && e.Role == EnemyRole.Guardian);
                if (guardian != null)
                {
                    int before = game.State.TurnNumber; game.Submit(new PlayerCommand(CommandKind.Descend)); while (game.IsBusy) yield return null;
                    Check(!game.State.IsVictory && game.State.TurnNumber == before, "Living guardian did not block the exit.");
                    yield return Capture(output, "guardian-vault"); guardian.TakeDamage(guardian.MaxHp);
                }
                game.Submit(new PlayerCommand(CommandKind.Descend));
                while (game.IsBusy) yield return null;
            }
            Check(game.State.IsVictory, "Final stairs did not complete the run.");
            turn = game.State.TurnNumber;
            game.Submit(new PlayerCommand(CommandKind.Wait)); game.ToggleInventory();
            Check(game.State.TurnNumber == turn && !game.InventoryOpen, "Victory did not block actions.");
            yield return Capture(output, "victory");
            game.Restart(); Check(!game.State.IsFinished && game.State.TurnNumber == 0, "Victory restart failed.");
            game.Submit(new PlayerCommand(CommandKind.Wait)); while (game.IsBusy) yield return null;
            string save = Path.Combine(output, "Save", "run.sav"); byte[] saved = SaveFile.Read(save);
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            game = FindFirstObjectByType<GameBootstrap>();
            Check(game.State.TurnNumber == 1 && !game.MenuOpen && saved.SequenceEqual(SaveFile.Read(save)), "Saved run did not resume.");
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed); yield return new WaitForSecondsRealtime(0.5f);
            game.ToggleMenu(); game.Submit(new PlayerCommand(CommandKind.Wait));
            Check(game.State.TurnNumber == 1, "Menu input consumed a turn.");
            yield return Capture(output, "menu");
            Menu().Select(3); Menu().Activate();
            yield return Capture(output, "new-run-confirmation");
            game.ToggleMenu(); Check(game.State.TurnNumber == 1 && game.MenuOpen, "Cancel confirmation changed the run.");
            game.ToggleMenu(); game.ToggleMenu(); Menu().Select(5); Menu().Activate();
            byte setting = SaveFile.Read(Path.Combine(output, "Save", "settings.dat"))[0];
            game.ToggleMenu();
            Directory.CreateDirectory(save + ".tmp");
            game.Submit(new PlayerCommand(CommandKind.Wait)); while (game.IsBusy) yield return null;
            Check(game.MenuOpen && game.State.TurnNumber == 2 && saved.SequenceEqual(SaveFile.Read(save)), "Save failure lost the last save or active run.");
            yield return Capture(output, "save-failure");
            Directory.Delete(save + ".tmp"); Check(game.SaveCurrent(), "Save retry failed.");
            File.WriteAllText(save, "broken");
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            game = FindFirstObjectByType<GameBootstrap>();
            Check(game.MenuOpen && File.ReadAllText(save) == "broken", "Broken save was silently replaced.");
            yield return Capture(output, "load-failure");
            Check(game.GetComponent<UnityEngine.UIElements.UIDocument>() != null, "Reload lost the UI.");
            var hud = (HudPresenter)typeof(GameBootstrap).GetField("hud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(game);
            Check(hud.Animate.value == (setting == 1), "Animation setting did not persist.");
            // Load errors initially select Quit (index 2); move to Restore (index 4).
            Menu().Select(2); Menu().Activate(); Menu().Activate();
            Check(!game.MenuOpen && game.State.TurnNumber == 1, "Backup recovery failed.");
            yield return Capture(output, "restored");
            Invoke("OpenUtilities"); Utility().Select(2); Utility().Activate(); yield return Capture(output, "run-records"); Utility().Close();
            Check(PlayerHistory.Read(Path.Combine(output, "Save", "history.json")).runs.Count >= 2, "Run history did not persist.");
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: boot, duplicate-session rejection, inventory, equipment, healing, busy lock, resize, death/restart, victory/restart, menu lock, cancel new run, save/resume, settings, write failure/retry, corrupt save protection, backup recovery.");
            game.ToggleMenu(); Menu().Select(2); Menu().Activate();
        }
        private MenuPresenter Menu() => (MenuPresenter)typeof(GameBootstrap).GetField("menu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(game);
        private UtilityPresenter Utility() => (UtilityPresenter)typeof(GameBootstrap).GetField("utility", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(game);
        private void Invoke(string method) => typeof(GameBootstrap).GetMethod(method, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(game, null);
        private static IEnumerator Capture(string output, string name)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(output, name + ".png"); ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 10;
            while (!File.Exists(path) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(File.Exists(path), "Screenshot failed: " + name);
            yield return null;
        }
        private static void Check(bool condition, string message)
        {
            if (condition) return;
            Debug.LogError("SMOKE FAILED: " + message); Application.Quit(1); throw new InvalidOperationException(message);
        }
    }
}
#endif
