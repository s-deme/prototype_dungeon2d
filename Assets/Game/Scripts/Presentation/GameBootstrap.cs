using System;
using System.Collections;
using System.IO;
using LanternDepths.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed partial class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameRulesAsset rules;
        [SerializeField] private StyleSheet theme;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int fixedSeed = 12345;
        private readonly PlayerInputAdapter input = new PlayerInputAdapter();
        private readonly MessageLog messages = new MessageLog();
        private RunController game;
        private HudPresenter hud;
        private InventoryPresenter inventory;
        private GridPresenter grid;
        private PanelSettings panel;
        private MenuPresenter menu;
        private string savePath, settingsPath;
        private FileStream sessionLock;
        private bool loadBlocked, exiting;
        private bool busy;
        private UtilityPresenter utility;
        private PlayerPreferences preferences = new PlayerPreferences();
        private PlayerHistory history = new PlayerHistory();
        private string preferencesPath, historyPath;
        private bool historyBlocked;
        private int binding = -1;
        private TurnAudio sound;
        private Font japaneseFont;
        public RunState State => game?.State;
        public bool IsBusy => busy;
        public bool InventoryOpen => inventory != null && inventory.IsOpen;
        public bool MenuOpen => menu != null && menu.IsOpen;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static string SaveDirectoryOverride;
        private static bool smokeStarted;
#endif
        private void Awake()
        {
            Application.targetFrameRate = 60;
            var cameraObject = new GameObject("UI Camera"); cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.063f, 0.094f, 0.129f); camera.cullingMask = 0;
            cameraObject.AddComponent<AudioListener>(); sound = gameObject.AddComponent<TurnAudio>();
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DefaultTheme");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1280, 900);
            panel.screenMatchMode = PanelScreenMatchMode.Expand;
            var document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement;
            root.style.unityFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            japaneseFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "MS Gothic", "Arial Unicode MS" }, 18);
            root.style.unityFont = japaneseFont;
            if (theme != null) root.styleSheets.Add(theme);
            try
            {
                if (rules == null || theme == null) throw new InvalidOperationException("Assign the game rules and theme on the Main scene bootstrap.");
                game = new RunController(rules.ToRules());
                hud = new HudPresenter(root, Submit, ToggleInventory, Restart, ToggleMenu);
                inventory = new InventoryPresenter(hud.Pack, Submit, ToggleInventory); grid = new GridPresenter(hud.Map, hud.Inspect, hud.InspectItem);
                menu = new MenuPresenter(hud.Root, ToggleMenu, () => SaveCurrent(), Quit, RequestNewRun, RequestBackup, ToggleAnimation, OpenUtilities);
                utility = new UtilityPresenter(hud.Root);
                string directory = Application.persistentDataPath;
#if UNITY_EDITOR
                directory = Path.Combine(directory, "Editor");
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (SaveDirectoryOverride != null) directory = SaveDirectoryOverride;
                foreach (string arg in Environment.GetCommandLineArgs())
                    if (arg.StartsWith("--smoke-output=")) directory = Path.Combine(Path.GetFullPath(arg.Substring(15)), "Save");
#endif
                savePath = Path.Combine(directory, "run.sav");
                try { sessionLock = SaveFile.OpenSession(directory); }
                catch (Exception error) when (SaveError(error))
                { throw new InvalidOperationException("Cannot open the save folder. Close any other copy of Lantern Depths and check folder access, then restart.", error); }
                settingsPath = Path.Combine(directory, "settings.dat");
                preferencesPath = Path.Combine(directory, "preferences.json"); historyPath = Path.Combine(directory, "history.json");
                try { preferences = PlayerPreferences.Read(preferencesPath); }
                catch (Exception error) when (SaveError(error)) { messages.Append("Preferences could not be loaded. Defaults are in use."); }
                input.Map = key => preferences.Map(key); ApplyPreferences();
                try { history = PlayerHistory.Read(historyPath); }
                catch (Exception error) when (SaveError(error)) { historyBlocked = true; }
                game.StartNewRun(NewSeed());
                if (File.Exists(savePath) || File.Exists(savePath + ".bak"))
                {
                    try { game.Load(SaveFile.Read(savePath)); if (history.journalRun == State.RunId && history.journalTurn <= State.TurnNumber) foreach (string line in (history.journal ?? "").Split('\n')) messages.Append(line); messages.Append("Your saved descent continues."); menu.Status($"Resumed — floor {State.FloorNumber}, turn {State.TurnNumber}."); }
                    catch (Exception error) when (SaveError(error))
                    { loadBlocked = true; menu.SetOpen(true); menu.Status("The save could not be loaded. Restore the previous save or begin a new descent.\n" + error.Message); }
                }
                else { Welcome(); SaveCurrent(); }
                try
                {
                    if (File.Exists(settingsPath))
                    {
                        byte[] setting = SaveFile.Read(settingsPath);
                        if (setting.Length != 1 || setting[0] > 1) throw new InvalidDataException("Invalid animation setting.");
                        hud.Animate.value = setting[0] == 1;
                    }
                }
                catch (Exception error) when (SaveError(error))
                { if (!loadBlocked) { menu.SetOpen(true); menu.Status("The animation setting could not be loaded. Using animation ON."); } }
                inventory.SetOpen(false); grid.Build(game.State); Refresh();
                if (!preferences.tutorialSeen && !loadBlocked && Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") < 0 && Array.IndexOf(Environment.GetCommandLineArgs(), "--resume-check") < 0) OpenHelp();
                Application.wantsToQuit += BeforeQuit;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!smokeStarted && (Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0 || Array.IndexOf(Environment.GetCommandLineArgs(), "--resume-check") >= 0))
                { smokeStarted = true; new GameObject("Player smoke test").AddComponent<DevelopmentSmoke>().Begin(this); }
#endif
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                root.Clear(); root.AddToClassList("game");
                var notice = new Label("Unable to start Lantern Depths.\n" + error.Message);
                notice.style.whiteSpace = WhiteSpace.Normal; root.Add(notice); enabled = false;
                InventoryPresenter.AddButton(root, "Quit", Exit);
            }
        }
        public void Restart()
        {
            if (busy) return;
            var next = new RunController(rules.ToRules()); next.StartNewRun(NewSeed());
            try { SaveFile.Write(savePath, next.Save()); }
            catch (Exception error) when (SaveError(error)) { ShowSaveError(error); return; }
            if (!loadBlocked && State != null) { history.Record(game); PersistHistory(); }
            game = next; loadBlocked = false; menu.SetOpen(false); Welcome();
            inventory.SetOpen(false); grid.Build(game.State); Refresh();
        }
        private int NewSeed() => useFixedSeed ? fixedSeed : unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks);
        private void Welcome()
        {
            messages.Clear(); messages.Append($"Recover the ember at the stairs on floor {game.FinalFloor}. Find the stairs and descend.");
            messages.Append("Bump into enemies to attack. Every successful action advances the dungeon.");
        }
        public void ToggleInventory()
        {
            if (busy || MenuOpen || loadBlocked || game.State.IsFinished) return;
            inventory.SetOpen(!inventory.IsOpen); inventory.Refresh(game.State);
            ApplyPreferences();
        }
        private void Update()
        {
            bool hasCommand = input.TryRead(busy || utility.IsOpen || MenuOpen || loadBlocked || inventory.IsOpen || game.State.IsFinished, out var command);
            if (busy) return;
            if (utility.IsOpen)
            {
                if (binding >= 0) { CaptureBinding(); return; }
                if (input.ClosePressed || input.MenuPressed) utility.Close();
                else if (input.SelectionDelta != 0) utility.Select(input.SelectionDelta);
                else if (input.ConfirmPressed) utility.Activate();
                return;
            }
            if (MenuOpen)
            {
                if (input.MenuPressed || input.ClosePressed) ToggleMenu();
                else if (input.AnimationPressed) ToggleAnimation();
                else if (input.SelectionDelta != 0) menu.Select(input.SelectionDelta);
                else if (input.ConfirmPressed) menu.Activate();
                return;
            }
            if (input.MenuPressed && (!inventory.IsOpen || !input.ClosePressed)) { ToggleMenu(); return; }
            if (game.State.IsFinished) { if (input.RestartPressed) Restart(); return; }
            if (input.AnimationPressed) { ToggleAnimation(); return; }
            if (input.InspectPressed && !inventory.IsOpen) { hud.CycleEnemy(); return; }
            if (input.InventoryPressed || (inventory.IsOpen && input.ClosePressed)) { ToggleInventory(); return; }
            if (inventory.IsOpen)
            {
                if (input.SelectionDelta != 0) inventory.Select(input.SelectionDelta);
                if (input.UsePressed) inventory.Act(CommandKind.Use);
                else if (input.EquipPressed) inventory.EquipSelected();
                else if (input.DropPressed) inventory.Act(CommandKind.Drop);
                if (input.SelectionDelta != 0) ApplyPreferences();
                LocalText.Refresh(hud.Root);
                return;
            }
            if (hasCommand) Submit(command);
        }
        public void Submit(PlayerCommand command)
        {
            if (busy || utility.IsOpen || MenuOpen || loadBlocked) return;
            if (game.State.IsFinished) { Refresh(); return; }
            if (command.Kind == CommandKind.Move) grid.FacePlayer(command.Direction);
            var result = game.Execute(command);
            if (result.ConsumesTurn) SaveCurrent();
            StartCoroutine(Present(result));
        }
        private IEnumerator Present(ActionResult result)
        {
            busy = true; hud.Pack.SetEnabled(false); menu.SetEnabled(false);
            try
            {
                if (result.ConsumesTurn) yield return null;
                if (result.ChangedFloor) grid.Build(game.State);
                foreach (var e in result.Events)
                {
                    messages.Append(e.Message);
                    sound.Play(e, preferences.volume);
                    yield return grid.Play(e, hud.Animate.value);
                }
            }
            finally
            {
                grid.Sync(game.State); busy = false; hud.Pack.SetEnabled(true); menu.SetEnabled(true);
                if (game.State.IsFinished) inventory.SetOpen(false);
                Refresh();
                PersistHistory();
            }
        }
        private void Refresh()
        {
            hud.Refresh(game, messages); inventory.Refresh(game.State);
            menu.Refresh(!loadBlocked, File.Exists(savePath + ".bak"), hud.Animate.value);
            ApplyPreferences();
        }
        public void ToggleMenu()
        {
            if (MenuOpen && menu.IsConfirming) { menu.CancelConfirmation(); return; }
            if (busy || (MenuOpen && loadBlocked)) return;
            inventory.SetOpen(false); menu.SetOpen(!MenuOpen); Refresh();
        }
        private void ToggleAnimation()
        {
            hud.Animate.value = !hud.Animate.value;
            try { SaveFile.Write(settingsPath, new[] { (byte)(hud.Animate.value ? 1 : 0) }); }
            catch (Exception error) when (SaveError(error)) { menu.SetOpen(true); menu.Status("The animation setting could not be saved. It applies for this session only."); }
            Refresh();
        }
        public bool SaveCurrent()
        {
            if (loadBlocked) return false;
            try { SaveFile.Write(savePath, game.Save()); if (State.IsFinished) history.Record(game); menu.Status($"Saved — floor {State.FloorNumber}, turn {State.TurnNumber}."); LocalText.Refresh(hud.Root); return true; }
            catch (Exception error) when (SaveError(error)) { ShowSaveError(error); return false; }
        }
        private static bool SaveError(Exception error) => error is IOException || error is InvalidDataException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException;
        private void ShowSaveError(Exception error)
        {
            menu.SetOpen(true); menu.Status("Could not save. Your current run is still in memory.\nCheck free space and write access, then retry Save now before quitting.");
            menu.Refresh(!loadBlocked, File.Exists(savePath + ".bak"), hud.Animate.value);
        }
        private void RequestNewRun()
        {
            menu.Confirm("Replace the current descent with a new run? This overwrites the current save.", "Confirm new descent", Restart);
        }
        private void RequestBackup()
        {
            menu.Confirm("Restore the previous saved turn? Current progress will be replaced. The current file will be kept as a backup.", "Confirm restore previous save", () =>
            {
                try
                {
                    byte[] data = SaveFile.Read(savePath + ".bak");
                    var next = new RunController(rules.ToRules()); next.Load(data);
                    SaveFile.Write(savePath, data); game = next; loadBlocked = false;
                    menu.SetOpen(false); messages.Clear(); messages.Append("Previous save restored.");
                    grid.Build(game.State); Refresh();
                }
                catch (Exception error) when (SaveError(error)) { menu.Status("Could not restore the previous save.\n" + error.Message); }
            });
        }
        private bool BeforeQuit() { PersistHistory(); return exiting || loadBlocked || SaveCurrent(); }
        private void Quit()
        {
            if (!loadBlocked && !SaveCurrent())
            {
                menu.Confirm("Saving failed. Quit and lose changes since the last successful save?", "Quit without saving", Exit);
                return;
            }
            Exit();
        }
        private void Exit()
        {
            exiting = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDisable()
        {
            StopAllCoroutines(); busy = false;
            if (game?.State != null && grid != null) { grid.Sync(game.State); Refresh(); }
        }
        private void OnDestroy() { Application.wantsToQuit -= BeforeQuit; sessionLock?.Dispose(); if (panel != null) Destroy(panel); if (japaneseFont != null) Destroy(japaneseFont); }
    }
}
