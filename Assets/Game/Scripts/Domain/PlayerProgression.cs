using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public sealed class ProgressionRules
    {
        public IReadOnlyList<int> Thresholds { get; }
        public int HpGain { get; }
        public int AttackGain { get; }
        public ProgressionRules(int[] cumulativeThresholds, int hpGain, int attackGain)
        {
            if (cumulativeThresholds == null || hpGain < 0 || attackGain < 0) throw new ArgumentException("Invalid progression rules.");
            int last = 0;
            foreach (int threshold in cumulativeThresholds)
            {
                if (threshold <= last) throw new ArgumentException("Experience thresholds must increase.");
                last = threshold;
            }
            Thresholds = Array.AsReadOnly((int[])cumulativeThresholds.Clone()); HpGain = hpGain; AttackGain = attackGain;
        }
    }
    public sealed class PlayerProgression
    {
        private readonly ProgressionRules rules;
        public int Experience { get; private set; }
        public PlayerProgression(ProgressionRules rules) { this.rules = rules ?? throw new ArgumentNullException(nameof(rules)); }
        public int NextThreshold(CharacterState player) => player.Level <= rules.Thresholds.Count ? rules.Thresholds[player.Level - 1] : -1;
        public void AddExperience(CharacterState player, int amount, ActionResult result)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!player.IsAlive) return;
            Experience = (int)Math.Min(int.MaxValue, (long)Experience + amount);
            while (NextThreshold(player) >= 0 && Experience >= NextThreshold(player))
            {
                player.LevelUp(rules.HpGain, rules.AttackGain);
                result.Add(new GameEvent(EventKind.LevelUp, $"Level {player.Level}! Your strength and health grow.", player.Id));
            }
        }
    }
}
