using System.Collections;
using LanternDepths.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace LanternDepths.Tests
{
    public sealed class SceneTests
    {
        private string saves;
        private System.IDisposable heldSession;
        [SetUp] public void IsolateSaves()
        {
            saves = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LanternSceneTests-" + System.Guid.NewGuid().ToString("N"));
            GameBootstrap.SaveDirectoryOverride = saves;
            new PlayerPreferences { tutorialSeen = true }.Write(System.IO.Path.Combine(saves, "preferences.json"));
        }
        [UnityTearDown] public IEnumerator RemoveTestSaves()
        {
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap != null) Object.Destroy(bootstrap.gameObject);
            yield return null;
            heldSession?.Dispose(); heldSession = null;
            GameBootstrap.SaveDirectoryOverride = null;
            if (System.IO.Directory.Exists(saves)) System.IO.Directory.Delete(saves, true);
        }
        [UnityTest] public IEnumerator MenuBlocksTurnsAndSceneReloadResumesSavedRun()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            bootstrap.ToggleMenu(); Assert.That(bootstrap.MenuOpen, Is.True);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait)); Assert.That(bootstrap.State.TurnNumber, Is.Zero);
            bootstrap.ToggleMenu(); bootstrap.Submit(new PlayerCommand(CommandKind.Wait));
            while (bootstrap.IsBusy) yield return null;
            int hp = bootstrap.State.Player.Hp; var position = bootstrap.State.Player.Position;
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            Assert.That(bootstrap.MenuOpen, Is.False); Assert.That(bootstrap.State.TurnNumber, Is.EqualTo(1));
            Assert.That(bootstrap.State.Player.Hp, Is.EqualTo(hp)); Assert.That(bootstrap.State.Player.Position, Is.EqualTo(position));
            System.IO.File.WriteAllText(System.IO.Path.Combine(saves, "run.sav"), "broken");
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            Assert.That(bootstrap.MenuOpen, Is.True); bootstrap.ToggleMenu(); Assert.That(bootstrap.MenuOpen, Is.True);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait)); Assert.That(bootstrap.State.TurnNumber, Is.Zero);
            Assert.That(System.IO.File.ReadAllText(System.IO.Path.Combine(saves, "run.sav")), Is.EqualTo("broken"));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator FirstTutorialBlocksGameplayAndSettingsCanSwitchLanguage()
        {
            new PlayerPreferences().Write(System.IO.Path.Combine(saves, "preferences.json"));
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            var utility = (UtilityPresenter)typeof(GameBootstrap).GetField("utility", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(bootstrap);
            Assert.That(utility.IsOpen, Is.True); bootstrap.Submit(new PlayerCommand(CommandKind.Wait)); Assert.That(bootstrap.State.TurnNumber, Is.Zero);
            utility.Activate(); Assert.That(utility.IsOpen, Is.False);
            Assert.That(PlayerPreferences.Read(System.IO.Path.Combine(saves, "preferences.json")).tutorialSeen, Is.True);
            typeof(GameBootstrap).GetMethod("OpenSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(bootstrap, null);
            utility.Select(1); utility.Activate(); Assert.That(PlayerPreferences.Read(System.IO.Path.Combine(saves, "preferences.json")).japanese, Is.False);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait)); Assert.That(bootstrap.State.TurnNumber, Is.Zero);
            utility.Close(); bootstrap.Submit(new PlayerCommand(CommandKind.Wait)); while (bootstrap.IsBusy) yield return null;
            Assert.That(bootstrap.State.TurnNumber, Is.EqualTo(1)); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator LockedSaveFolderShowsRecoveryMessageWithoutStartingAnotherRun()
        {
            heldSession = SaveFile.OpenSession(saves);
            {
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("IOException: Sharing violation"));
                yield return SceneManager.LoadSceneAsync("Main"); yield return null;
                var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
                Assert.That(bootstrap.State, Is.Null); Assert.That(bootstrap.enabled, Is.False);
                var root = bootstrap.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(root.Q<Label>().text, Does.Contain("Close any other copy"));
                Assert.That(root.Q<Button>().text, Is.EqualTo("Quit"));
                Assert.That(System.IO.File.Exists(System.IO.Path.Combine(saves, "run.sav")), Is.False);
            }
        }
        [UnityTest] public IEnumerator MainSceneSupportsInventoryTurnsDeathAndRestart()
        {
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            Assert.That(bootstrap, Is.Not.Null); Assert.That(bootstrap.State, Is.Not.Null);
            var root = bootstrap.GetComponent<UIDocument>().rootVisualElement;
            Assert.That(root.childCount, Is.GreaterThan(0));
            bootstrap.ToggleInventory(); Assert.That(bootstrap.InventoryOpen, Is.True);
            Assert.That(bootstrap.State.TurnNumber, Is.Zero);
            bootstrap.ToggleInventory(); Assert.That(bootstrap.InventoryOpen, Is.False);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait));
            Assert.That(bootstrap.IsBusy, Is.True);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait));
            Assert.That(bootstrap.State.TurnNumber, Is.EqualTo(1));
            float deadline = Time.realtimeSinceStartup + 5;
            while (bootstrap.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(bootstrap.IsBusy, Is.False);
            bootstrap.State.Player.TakeDamage(bootstrap.State.Player.MaxHp);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait)); Assert.That(bootstrap.State.TurnNumber, Is.EqualTo(1));
            bootstrap.Restart(); Assert.That(bootstrap.State.IsGameOver, Is.False); Assert.That(bootstrap.State.Inventory.Items.Count, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator CaptureRepresentativeScreens()
        {
            if (Application.isBatchMode) Assert.Ignore("Run this screenshot test interactively with the Game view visible.");
            yield return SceneManager.LoadSceneAsync("Main"); yield return null;
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../TestResults/Screenshots"));
            System.IO.Directory.CreateDirectory(folder);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "exploration.png"));
            bootstrap.ToggleInventory(); yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "inventory-empty.png"));
            bootstrap.State.Inventory.TryAdd(new ItemInstance(1, new ItemDefinition("test-tonic", "Ember Tonic", "Restores 18 HP.", ItemKind.Healing, 18)));
            bootstrap.ToggleInventory(); bootstrap.ToggleInventory();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "inventory-populated.png"));
            bootstrap.ToggleInventory();
            bootstrap.State.Player.TakeDamage(bootstrap.State.Player.MaxHp);
            bootstrap.Submit(new PlayerCommand(CommandKind.Wait));
            Assert.That(bootstrap.State.IsGameOver, Is.True);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, "game-over.png")); yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
