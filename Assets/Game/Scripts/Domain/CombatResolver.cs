using System;

namespace LanternDepths
{
    public sealed class CombatResolver
    {
        public void Attack(RunState state, CharacterState attacker, CharacterState target, ActionResult result, int range = 1)
        {
            if (!attacker.IsAlive || !target.IsAlive || attacker.Position.Distance(target.Position) > range ||
                !BasicEnemyBrain.HasLineOfSight(state.Floor.Map, attacker.Position, target.Position)) return;
            int damage = Math.Max(1, StatCalculator.Attack(state, attacker) - StatCalculator.Defense(state, target));
            DealDamage(state, attacker, target, damage, result);
        }
        internal static void DealDamage(RunState state, CharacterState attacker, CharacterState target, int damage, ActionResult result)
        {
            if (!target.IsAlive) return;
            int actual = target.TakeDamage(damage);
            result.ConsumesTurn = true;
            result.Add(new GameEvent(EventKind.Damaged, $"{attacker.Name} hits {target.Name} for {actual}.", target.Id, attacker.Position, target.Position, actual));
            if (target.IsAlive) return;
            result.Add(new GameEvent(EventKind.Died, $"{target.Name} falls.", target.Id, target.Position, target.Position));
            if (attacker == state.Player && state.Progression != null)
                state.Progression.AddExperience(state.Player, target.ExperienceReward, result);
        }
    }
}
