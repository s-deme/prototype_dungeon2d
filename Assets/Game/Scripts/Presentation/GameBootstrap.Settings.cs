using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public sealed partial class GameBootstrap
    {
        private readonly System.Collections.Generic.Dictionary<TextElement, float> fontSizes = new System.Collections.Generic.Dictionary<TextElement, float>();
        private string L(string ja, string en) => preferences.japanese ? ja : en;
        private void ApplyPreferences()
        {
            LocalText.Japanese = preferences.japanese;
            LocalText.KeyMap = key => preferences.Map(key);
            var mode = preferences.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.fullScreenMode != mode) Screen.fullScreenMode = mode;
            hud.Root.schedule.Execute(() =>
            {
                // Keep the grid fixed; enlarge reading text without changing tile coordinates.
                foreach (var dead in fontSizes.Keys.Where(e => e.panel == null).ToArray()) fontSizes.Remove(dead);
                hud.Root.Query<TextElement>().ForEach(e =>
                {
                    if (e.ClassListContains("glyph") || e.worldBound.width <= 0) return;
                    if (!fontSizes.TryGetValue(e, out float size)) { size = e.resolvedStyle.fontSize; if (float.IsNaN(size) || size <= 0) return; fontSizes[e] = size; }
                    e.style.fontSize = size * preferences.scale / 100f;
                });
            }).StartingIn(100);
            LocalText.Refresh(hud.Root);
        }
        private void StorePreferences()
        {
            ApplyPreferences();
            try { preferences.Write(preferencesPath); }
            catch (Exception error) when (SaveError(error)) { utility.Text(L("設定を保存できません。今回の起動中のみ適用します。", "Could not save preferences. Changes apply to this session only.")); }
        }
        private void PersistHistory()
        {
            if (historyBlocked || loadBlocked || State == null || historyPath == null || sessionLock == null) return;
            try
            {
                if (State.IsFinished) history.Record(game);
                history.journalRun = State.RunId; history.journalTurn = State.TurnNumber; history.journal = messages.FullText; history.Write(historyPath);
            }
            catch (Exception error) when (SaveError(error))
            { menu.Status(L("記録の保存に失敗しました。冒険のセーブとは別です。空き容量と権限を確認してください。", "Could not save records. Check disk space and permissions. Run saves are separate.")); LocalText.Refresh(hud.Root); }
        }
        private void OpenUtilities()
        {
            utility.Open("Settings / Help / Records");
            utility.Add("Settings", OpenSettings); utility.Add("Help", OpenHelp);
            utility.Add("Records", () => { utility.Open("Records"); utility.Add("Back", OpenUtilities); utility.Text(historyBlocked ? L("記録ファイルを読み込めません。破損したファイルは上書きせず保持しています。", "Records could not be loaded. The original file is preserved.") : history.Summary()); ApplyPreferences(); });
            utility.Add("Journal", () => { utility.Open("Journal"); utility.Add("Back", OpenUtilities); utility.Text(messages.FullText); ApplyPreferences(); });
            utility.Add("Close", utility.Close); ApplyPreferences();
        }
        private void OpenSettings()
        {
            utility.Open("Settings");
            utility.Add("Back", OpenUtilities);
            utility.Add("Language / 言語: " + (preferences.japanese ? "日本語" : "English"), () => { preferences.japanese = !preferences.japanese; LocalText.Japanese = preferences.japanese; OpenSettings(); Refresh(); StorePreferences(); });
            utility.Add(L("文字サイズ", "Text size") + $": {preferences.scale}%", () => { preferences.scale = preferences.scale == 120 ? 100 : preferences.scale + 10; OpenSettings(); StorePreferences(); });
            utility.Add(L("効果音", "Sound volume") + $": {preferences.volume}%", () => { preferences.volume = (preferences.volume + 25) % 125; OpenSettings(); StorePreferences(); sound.Play(new GameEvent(EventKind.PickedUp, ""), preferences.volume); });
            utility.Add(L("画面", "Display") + ": " + (preferences.fullscreen ? L("全画面", "Fullscreen") : L("ウィンドウ", "Windowed")), () => { preferences.fullscreen = !preferences.fullscreen; OpenSettings(); StorePreferences(); });
            utility.Add(L("キー割り当て", "Key bindings"), OpenBindings); ApplyPreferences();
        }
        private string[] BindingNames => new[] { L("上", "Up"), L("左", "Left"), L("下", "Down"), L("右／持ち物を置く", "Right / Drop"), L("左上", "Northwest"), L("右上／装備", "Northeast / Equip"), L("左下", "Southwest"), L("右下", "Southeast"), L("待機", "Wait"), L("拾う", "Pick up"), L("持ち物", "Pack"), L("便利操作／決定", "Action / Confirm"), L("使う", "Use"), L("演出切替", "Animation"), L("再出発", "Restart"), L("敵情報", "Inspect") };
        private void OpenBindings()
        {
            binding = -1; utility.Open(L("キー割り当て", "Key bindings")); utility.Add("Back", OpenSettings);
            utility.Text(L("変更したい操作を選び、英字・数字・Space・Enter・Tabを押してください。Escで中止。矢印、テンキー、ゲームパッドは固定です。", "Choose an action, then press a letter, number, Space, Enter or Tab. Esc cancels. Arrows, numpad and gamepad remain available."));
            var names = BindingNames;
            for (int i = 0; i < preferences.keys.Length; i++)
            {
                int index = i; utility.Add(names[i] + ": " + (KeyCode)preferences.keys[i], () => { binding = index; utility.Open(BindingNames[index]); utility.Text(L("新しいキーを押してください。Escで中止。", "Press the new key. Esc cancels.")); });
            }
            utility.Add(L("初期設定に戻す", "Reset bindings"), () => { preferences.keys = PlayerPreferences.Defaults.Select(k => (int)k).ToArray(); OpenBindings(); StorePreferences(); }); ApplyPreferences();
        }
        private void CaptureBinding()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { OpenBindings(); return; }
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!PlayerPreferences.Allowed(key) || !Input.GetKeyDown(key)) continue;
                int other = Array.IndexOf(preferences.keys, (int)key);
                if (other >= 0 && other != binding) preferences.keys[other] = preferences.keys[binding];
                preferences.keys[binding] = (int)key; OpenBindings(); StorePreferences(); return;
            }
        }
        private void OpenHelp()
        {
            utility.Open("Help");
            utility.Add(L("冒険へ戻る", "Return to adventure"), () => { preferences.tutorialSeen = true; StorePreferences(); utility.Close(); });
            utility.Text(L("目標：5階で守護者を倒し、階段から残り火を回収。\n\n① 矢印・WASD・テンキー・左スティックで移動。Q E Z Cは斜め移動。押しっぱなしで連続移動。敵のマスに進むと攻撃。\n② 瓶の薬・剣の武器・盾の防具・魔法陣の退避・火花付き瓶の爆薬の上ではEnter／LBの便利操作で拾える。階段の上では同じ操作で進む。何もない場所では待機。\n③ 行動が成功すると敵も1回動く。持ち物を見る・敵を調べる・メニューを開く操作では進まない。\n④ 弓の射手は4マス先まで射撃。壁を使って接近。ダイヤ形の守護者に赤い輪が出る強打予告を見たら離れよう。\n⑤ 行動ごとに自動保存され、再起動で続きから遊べる。\n\n敵や道具をクリック・フォーカス、または調べるキー／右スティック押し込みでHP・特性・効果を確認。三角は向き。壁に当たっても入力した方向を向く。\n持ち物：上下で選択、U/Aで使用、E/Xで装備、D/RBで置く。\nメニュー：上下で選択、Enter/Aで決定、Esc/Bで戻る。\nコントローラー：A 待機、X 拾う、Y 持ち物、LB 便利操作、Start メニュー。\n\n現在のキー割り当て：", "Goal: defeat the guardian on floor 5 and claim the ember at the stairs.\n\n1. Move with arrows, WASD, numpad or stick; Q E Z C move diagonally. Hold a direction to keep moving. Bump into an enemy to attack.\n2. Enter/LB picks up an item underfoot, uses stairs when standing on them, and waits otherwise. Use or equip items from your pack.\n3. Successful actions advance enemies one turn. Browsing, inspecting and menus are free.\n4. Bow-shaped archers shoot up to 4 tiles; approach behind walls. Step away when the diamond-shaped guardian shows a red warning ring.\n5. Each action autosaves; relaunch to continue.\n\nClick or focus enemies and items, or use Inspect / right stick click for health, traits and effects. The triangle marks facing, including when movement is blocked.\nPack: arrows select, U/A use, E/X equip, D/RB drop.\nMenu: arrows select, Enter/A confirm, Esc/B back.\nController: A wait, X pick up, Y pack, LB action, Start menu.\n\nCurrent bindings:") + "\n" + string.Join("   ·   ", BindingNames.Select((n, i) => n + " " + (KeyCode)preferences.keys[i])));
            ApplyPreferences();
        }
    }
}
