using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace LanternDepths.Presentation
{
    public static class LocalText
    {
        public static bool Japanese = true;
        public static System.Func<UnityEngine.KeyCode, UnityEngine.KeyCode> KeyMap = key => key;
        private static readonly Dictionary<string, string> phrases = new Dictionary<string, string>
        {
            { "A turn-based descent into the ember vaults", "一手ずつ進む、残り火を探す冒険" },
            { "THE EMBER VAULTS", "残り火の地下迷宮" }, { "WAYFARER", "旅人" },
            { "@ YOU   m MELEE / r ARCHER / B GUARDIAN   ! TONIC   * BLAST   ? RETREAT   / WEAPON   ] ARMOR   > STAIRS", "@ 自分   m 近接 / r 射手 / B 守護者   ! 回復   * 爆薬   ? 退避   / 武器   ] 防具   > 階段" },
            { "Return to game", "ゲームに戻る" }, { "Save now", "今すぐ保存" },
            { "Save and quit", "保存して終了" }, { "Quit (keep existing save)", "終了（セーブは保持）" },
            { "Begin a new descent…", "新しい冒険を始める…" }, { "Begin a new descent", "新しい冒険を始める" },
            { "Restore previous save…", "前のセーブを復元…" }, { "Turn animation:", "ターン演出：" },
            { "Confirm new descent", "新しい冒険を開始する" }, { "Confirm restore previous save", "前のセーブを復元する" },
            { "Replace the current descent with a new run? This overwrites the current save.", "新しい冒険を始めますか？ 現在のセーブを置き換えます。" },
            { "Restore the previous saved turn? Current progress will be replaced. The current file will be kept as a backup.", "前回保存したターンに戻しますか？ 現在のファイルはバックアップとして残ります。" },
            { "Saving failed. Quit and lose changes since the last successful save?", "保存に失敗しました。最後の保存以降の進行を破棄して終了しますか？" },
            { "Quit without saving", "保存せず終了" },
            { "Could not save. Your current run is still in memory.\nCheck free space and write access, then retry Save now before quitting.", "保存できませんでした。現在の進行はメモリに保持されています。\n空き容量と書き込み権限を確認し、終了する前に再度保存してください。" },
            { "The save could not be loaded. Restore the previous save or begin a new descent.", "セーブを読み込めません。前のセーブを復元するか、新しい冒険を始めてください。" },
            { "Could not restore the previous save.", "前のセーブを復元できませんでした。" },
            { "Your saved descent continues.", "保存した冒険を再開しました。" }, { "Previous save restored.", "前のセーブを復元しました。" },
            { "Bump into enemies to attack. Every successful action advances the dungeon.", "敵のいるマスへ移動すると攻撃。行動が成功すると敵も動きます。" },
            { "Recover the ember at the stairs to win.", "守護者を倒し、階段で残り火を回収しよう。" },
            { "Claim the ember", "残り火を回収" }, { "Stairs down", "下り階段" }, { "Choose your next step", "次の一歩を選ぼう" },
            { "THE EMBER RETURNS", "残り火を取り戻した" }, { "THE LANTERN FADES", "灯火は消えた" },
            { "You reclaimed the ember and found your way home.\nDescent complete.", "残り火を取り戻し、帰路についた。\n冒険クリア。" },
            { "Your pack and progress are lost.\nA new descent awaits.", "この冒険は終わった。\n新たな挑戦が待っている。" },
            { "Your pack is empty.\nStand on an item and press G to collect it.", "持ち物はありません。\n道具の上で拾う操作をしてください。" },
            { "Inventory browsing costs no turns.", "持ち物を眺めてもターンは進みません。" },
            { "Close pack", "持ち物を閉じる" }, { " [equipped]", "［装備中］" }, { "(vs equipped)", "（現在の装備との差）" },
            { "There is no item here.", "ここには道具がありません。" }, { "Your pack is full (20 items).", "持ち物がいっぱいです（20個）。" },
            { "That item is no longer in your pack.", "その道具はもう持っていません。" },
            { "Equip this item to use it.", "この道具は装備して使います。" }, { "Your health is already full.", "HPはすでに満タンです。" },
            { "There is no room to drop an item here.", "ここには道具を置けません。" },
            { "That item cannot be equipped, or is already equipped.", "装備できないか、すでに装備しています。" }, { "That item is not equipped.", "その道具は装備していません。" },
            { "That item action is unavailable.", "その道具操作はできません。" }, { "That action is unavailable.", "その行動はできません。" },
            { "Choose one adjacent tile.", "隣接するマスを選んでください。" }, { "The way is blocked.", "道が塞がっています。" },
            { "Stand on the stairs to descend.", "階段の上で操作してください。" },
            { "Defeat the Ember Guardian before claiming the ember.", "残り火を回収する前に、残り火の守護者を倒してください。" },
            { "The guardian's strike hits empty ground.", "守護者の強打は空を切った。" },
            { "Could not create the next floor. Your current floor is intact.", "次の階を生成できませんでした。現在の階は保持されています。" },
            { "You recover the vault's ember. Your lantern lights the way home.", "残り火を回収した。ランタンが帰り道を照らす。" },
            { "Stairs found. Use them when you are ready [Enter / LB].", "階段を発見。準備ができたら階段操作で先へ進もう。" },
            { "No enemy is within blast range (2 tiles).", "爆発の範囲（2マス）に敵がいません。" },
            { "There is no danger to escape.", "退避が必要な敵はいません。" }, { "No safer escape tile is reachable.", "今より安全な退避先がありません。" },
            { "The rune carries you away from danger.", "ルーンの力で危険から離れた。" },
            { "Restores 18 HP. A warm draught distilled from cavern moss.", "HPを18回復。洞窟の苔から作った温かい薬。" },
            { "Attack +3 while equipped. A plain blade with a steady edge.", "装備中は攻撃力+3。扱いやすい銅の刃。" },
            { "Defense +2 while equipped. Layered fibers soften incoming blows.", "装備中は防御力+2。重ねた繊維が衝撃を和らげる。" },
            { "Escape up to 4 reachable tiles to a safer position. Enemies act afterward.", "通行可能な道を最大4歩進み、敵から遠い位置へ退避。その後、敵が行動。" },
            { "Deal 8 damage to every visible enemy within 2 tiles.", "遮られていない周囲2マスの敵すべてに8ダメージ。" },
            { "Attack +6. Defense -2. A risky two-handed blade.", "攻撃力+6、防御力-2。防御を犠牲にする両手剣。" },
            { "Defense +5. Attack -2. Heavy protective plates.", "防御力+5、攻撃力-2。重い板金で身を守る。" },
            { "Ember Guardian", "残り火の守護者" }, { "Cinder Mite", "灰ダニ" }, { "Gloom Wisp", "闇の射手" }, { "Wayfarer", "旅人" },
            { "Ember Tonic", "残り火の薬" }, { "Copper Edge", "銅の刃" }, { "Woven Guard", "織り防具" },
            { "Retreat Rune", "退避ルーン" }, { "Blast Flask", "爆薬瓶" }, { "Heavy Blade", "重い大剣" }, { "Plate Armor", "板金鎧" },
            { "Settings / Help / Records", "設定・遊び方・記録" }, { "Settings", "設定" }, { "Help", "遊び方" }, { "Records", "冒険の記録" },
            { "Back", "戻る" }, { "Close", "閉じる" }, { "Journal", "行動履歴" }, { "JOURNAL", "直近の行動" },
            { "Healing", "回復" }, { "Weapon", "武器" }, { "Armor", "防具" }, { "Blink", "退避" }, { "Blast", "範囲攻撃" },
            { "WEAPON", "武器" }, { "ARMOR", "防具" }, { "None", "なし" }, { "LEVEL", "レベル" }, { "TURN", "ターン" }, { "FLOOR", "階層" }, { "PACK", "持ち物" },
            { "Victory", "クリア" }, { "Defeat", "敗北" }, { "Abandoned", "中断して再出発" },
            { "Menu", "メニュー" }, { "Pack", "持ち物" }, { "Wait", "待機" }, { "Pick up", "拾う" }, { "Down", "降りる" }, { "Claim", "回収" }, { "Use", "使う" }, { "Equip", "装備" }, { "Remove", "外す" }, { "Drop", "置く" },
            { "Inspect", "敵を調べる" }, { "No enemies remain.", "この階の敵はいません。" },
            { "Melee: closes in and attacks adjacent tiles.", "近接：接近して隣のマスを攻撃。" },
            { "Archer: range 4, retreats when adjacent. Walls block shots.", "射手：射程4。隣接すると後退。壁で射撃を防げる。" },
            { "Guardian: warns, then strikes next turn. Step away to dodge.", "守護者：予告の次ターンに強打。離れて回避しよう。" },
            { "STRIKE READY — move away!", "強打準備中 — 離れよう！" },
            { "Best floor", "最高到達階" }, { "Runs", "冒険数" }, { "Wins", "クリア数" }, { "Floor", "階" }, { "Turns", "ターン" }, { "Seed", "シード" },
            { "Entrance", "入口" }, { "Wisp passages", "射手の回廊" }, { "Old armory", "古い武器庫" }, { "Ember approach", "残り火への道" }, { "Guardian vault", "守護者の間" }
        };
        private static readonly KeyValuePair<string, string>[] ordered = phrases.OrderByDescending(p => p.Key.Length).ToArray();
        public static string T(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = Regex.Replace(value, @"\[([A-Za-z]+)(?= /|\])", match =>
            {
                string name = match.Groups[1].Value;
                if (!System.Enum.TryParse(name == "Enter" ? "Return" : name, out UnityEngine.KeyCode key)) return match.Value;
                var mapped = KeyMap(key); return "[" + (mapped == UnityEngine.KeyCode.Return ? "Enter" : mapped.ToString());
            });
            if (!Japanese) return value;
            value = Regex.Replace(value, @"(.+) hits (.+) for (\d+)\.", "$1 → $2 に $3 ダメージ。");
            value = Regex.Replace(value, @"(.+) falls\.", "$1 を倒した。");
            value = Regex.Replace(value, @"(.+) prepares a crushing strike\. Step away!", "$1 が強打を準備。離れよう！");
            value = Regex.Replace(value, @"Level (\d+)! Your strength and health grow\.", "レベル $1！ 攻撃力とHPが上がった。");
            value = Regex.Replace(value, @"You enter floor (\d+)\.", "$1 階に到着。");
            value = Regex.Replace(value, @"Recover the ember at the stairs on floor (\d+)\. Find the stairs and descend\.", "$1 階の階段で残り火を回収しよう。まずは下り階段を探そう。");
            value = Regex.Replace(value, @"Reach floor (\d+) and recover the ember\.", "$1 階を目指し、残り火を回収しよう。");
            value = Regex.Replace(value, @"Here: (.+)\. Pick it up to keep it\.", "足元：$1。拾って持ち物に加えよう。");
            value = Regex.Replace(value, @"Picked up (.+)\.", "$1 を拾った。");
            value = Regex.Replace(value, @"Used (.+)\. Recovered (\d+) HP\.", "$1 を使用。HPが $2 回復。");
            value = Regex.Replace(value, @"Used (.+)\.", "$1 を使用。");
            value = Regex.Replace(value, @"Unequipped (.+)\.", "$1 を外した。");
            value = Regex.Replace(value, @"Equipped (.+)\.", "$1 を装備した。");
            value = Regex.Replace(value, @"Dropped (.+)\.", "$1 を置いた。");
            value = Regex.Replace(value, @"(Saved|Resumed) — floor (\d+), turn (\d+)\.", "保存データ：$2 階、$3 ターン。");
            foreach (var pair in ordered) value = value.Replace(pair.Key, pair.Value);
            return value;
        }
        private sealed class Original { public Original() { } public string source, translated; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TextElement, Original> originals = new System.Runtime.CompilerServices.ConditionalWeakTable<TextElement, Original>();
        public static void Refresh(VisualElement root)
        {
            root.Query<TextElement>().ForEach(e =>
            {
                var entry = originals.GetOrCreateValue(e);
                if (e.text != entry.translated) entry.source = e.text;
                entry.translated = T(entry.source); e.text = entry.translated;
            });
        }
    }
}
