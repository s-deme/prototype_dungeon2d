using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LanternDepths.Presentation
{
    [Serializable] public sealed class PlayerPreferences
    {
        public int version = 1;
        public bool japanese = true, tutorialSeen, fullscreen;
        public int scale = 100, volume = 50;
        public int[] keys = Defaults.Select(k => (int)k).ToArray();
        public static readonly KeyCode[] Defaults = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Q, KeyCode.E, KeyCode.Z, KeyCode.C, KeyCode.Space, KeyCode.G, KeyCode.I, KeyCode.Return, KeyCode.U, KeyCode.F, KeyCode.R, KeyCode.Tab };
        public KeyCode Map(KeyCode key) { int i = Array.IndexOf(Defaults, key); return i < 0 ? key : (KeyCode)keys[i]; }
        public static PlayerPreferences Read(string path)
        {
            if (!File.Exists(path)) return new PlayerPreferences();
            var value = JsonUtility.FromJson<PlayerPreferences>(Encoding.UTF8.GetString(SaveFile.Read(path)));
            if (value == null || value.version != 1 || value.scale < 100 || value.scale > 120 || value.scale % 10 != 0 || value.volume < 0 || value.volume > 100 || value.volume % 25 != 0 || value.keys == null || value.keys.Length != Defaults.Length || value.keys.Distinct().Count() != value.keys.Length || value.keys.Any(k => !Allowed((KeyCode)k)))
                throw new InvalidDataException("Invalid preferences.");
            return value;
        }
        public static bool Allowed(KeyCode key) => (key >= KeyCode.A && key <= KeyCode.Z) || key == KeyCode.Space || key == KeyCode.Return || key == KeyCode.Tab || (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9);
        public void Write(string path) => SaveFile.Write(path, Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));
    }
    [Serializable] public sealed class RunRecord
    {
        public string id, date, outcome;
        public int floor, level, turns, seed;
    }
    [Serializable] public sealed class PlayerHistory
    {
        public int version = 1;
        public List<RunRecord> runs = new List<RunRecord>();
        public string journalRun = "", journal = "";
        public int journalTurn;
        public static PlayerHistory Read(string path)
        {
            if (!File.Exists(path)) return new PlayerHistory();
            var value = JsonUtility.FromJson<PlayerHistory>(Encoding.UTF8.GetString(SaveFile.Read(path)));
            if (value == null || value.version != 1 || value.runs == null || value.runs.Count > 1000 || value.runs.Any(r => r == null || r.id == null || r.turns < 0)) throw new InvalidDataException("Invalid history.");
            return value;
        }
        public void Record(RunController game)
        {
            var s = game.State;
            if (runs.Any(r => r.id == s.RunId)) return;
            runs.Add(new RunRecord { id = s.RunId, date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), outcome = s.IsVictory ? "Victory" : s.IsGameOver ? "Defeat" : "Abandoned", floor = s.FloorNumber, level = s.Player.Level, turns = s.TurnNumber, seed = game.Seed });
            // ponytail: retain the latest 1000 runs; use an archive if lifetime statistics are needed beyond that.
            if (runs.Count > 1000) runs.RemoveAt(0);
        }
        public void Write(string path) => SaveFile.Write(path, Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));
        public string Summary() => $"Runs {runs.Count}   Wins {runs.Count(r => r.outcome == "Victory")}   Best floor {(runs.Count == 0 ? 0 : runs.Max(r => r.floor))}\n\n" +
            string.Join("\n", runs.AsEnumerable().Reverse().Select(r => $"{r.date}  {r.outcome}  Floor {r.floor}  Lv {r.level}  Turns {r.turns}  Seed {r.seed}"));
    }
}
