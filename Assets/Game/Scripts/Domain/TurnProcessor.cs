using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public interface IEnemyBrain
    {
        PlayerCommand ChooseAction(RunState state, CharacterState enemy, Random random);
    }
    public sealed class TurnEndProcessor
    {
        public void Process(RunState state, ActionResult result)
        {
            state.TurnNumber++;
        }
    }
    public sealed class TurnProcessor
    {
        private readonly ActionResolver resolver;
        private readonly IEnemyBrain brain;
        private readonly Random random;
        private readonly TurnEndProcessor turnEnd = new TurnEndProcessor();
        private bool processing;
        public TurnProcessor(ActionResolver resolver, IEnemyBrain brain, Random random)
        { this.resolver = resolver; this.brain = brain; this.random = random; }
        public ActionResult Execute(RunState state, PlayerCommand command)
        {
            if (processing || state.IsFinished) return new ActionResult();
            processing = true;
            try
            {
                var result = resolver.Resolve(state, state.Player, command);
                if (!result.ConsumesTurn) return result;
                if (!result.ChangedFloor && !state.IsFinished)
                {
                    var actors = new List<CharacterState>(state.Floor.Enemies);
                    actors.Sort((a, b) => a.Id.CompareTo(b.Id));
                    foreach (var enemy in actors)
                    {
                        if (state.IsFinished) break;
                        if (enemy.IsAlive) result.Append(resolver.Resolve(state, enemy, brain.ChooseAction(state, enemy, random)));
                    }
                }
                turnEnd.Process(state, result);
                return result;
            }
            finally { processing = false; }
        }
    }
}
