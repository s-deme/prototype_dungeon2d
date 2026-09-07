namespace LanternDepths
{
    public sealed class ActionResolver
    {
        private readonly CombatResolver combat = new CombatResolver();
        private readonly ItemActionResolver items = new ItemActionResolver();
        private readonly System.Action<RunState> descend;
        public ActionResolver(System.Action<RunState> descend = null) { this.descend = descend; }
        public ActionResult Resolve(RunState state, CharacterState actor, PlayerCommand command)
        {
            var result = new ActionResult();
            if (state.IsFinished || !actor.IsAlive) return result;
            if (actor == state.Player && command.Kind == CommandKind.Context)
                command = state.Floor.GetItemAt(actor.Position) != null ? new PlayerCommand(CommandKind.PickUp) :
                    actor.Position == state.Floor.Stairs ? new PlayerCommand(CommandKind.Descend) : new PlayerCommand(CommandKind.Wait);
            if (actor != state.Player && command.Kind == CommandKind.Shoot && actor.Role == EnemyRole.Archer)
            { combat.Attack(state, actor, state.Player, result, 4); return result; }
            if (actor != state.Player && actor.Role == EnemyRole.Guardian)
            {
                if (command.Kind == CommandKind.Charge)
                {
                    actor.Charged = true; result.ConsumesTurn = true;
                    result.Add(new GameEvent(EventKind.Telegraph, $"{actor.Name} prepares a crushing strike. Step away!", actor.Id)); return result;
                }
                if (command.Kind == CommandKind.Smash && actor.Charged)
                {
                    actor.Charged = false; combat.Attack(state, actor, state.Player, result);
                    if (!result.ConsumesTurn) result.Say("The guardian's strike hits empty ground.");
                    result.ConsumesTurn = true; return result;
                }
            }
            if (actor == state.Player && command.Kind >= CommandKind.PickUp && command.Kind <= CommandKind.Unequip)
                return items.Resolve(state, command);
            if (command.Kind == CommandKind.Descend && actor == state.Player)
            {
                if (actor.Position != state.Floor.Stairs || descend == null) { result.Say("Stand on the stairs to descend."); return result; }
                foreach (var enemy in state.Floor.Enemies)
                    if (enemy.IsAlive && enemy.Role == EnemyRole.Guardian) { result.Say("Defeat the Ember Guardian before claiming the ember."); return result; }
                try { descend(state); }
                catch (System.InvalidOperationException) { result.Say("Could not create the next floor. Your current floor is intact."); return result; }
                result.ConsumesTurn = true;
                result.ChangedFloor = !state.IsVictory;
                result.Add(state.IsVictory
                    ? new GameEvent(EventKind.Victory, "You recover the vault's ember. Your lantern lights the way home.")
                    : new GameEvent(EventKind.FloorChanged, $"You enter floor {state.FloorNumber}."));
                return result;
            }
            if (command.Kind == CommandKind.Wait) { result.ConsumesTurn = true; return result; }
            if (command.Kind != CommandKind.Move) { result.Say("That action is unavailable."); return result; }
            if (command.Direction.X < -1 || command.Direction.X > 1 || command.Direction.Y < -1 || command.Direction.Y > 1 || command.Direction == default)
            { result.Say("Choose one adjacent tile."); return result; }
            var destination = actor.Position + command.Direction;
            var target = actor == state.Player ? state.Floor.GetEnemyAt(destination) :
                (state.Player.Position == destination ? state.Player : null);
            if (target != null && MovementRules.CanReachAdjacent(state.Floor.Map, actor.Position, destination))
            { combat.Attack(state, actor, target, result); return result; }
            if (!MovementRules.CanMove(state.Floor, actor, state.Player, destination))
            { result.Say("The way is blocked."); return result; }
            var from = actor.Position; actor.Position = destination;
            result.Add(new GameEvent(EventKind.Moved, "", actor.Id, from, destination));
            if (actor == state.Player && destination == state.Floor.Stairs) result.Say("Stairs found. Press the action button to descend [Enter / LB].");
            if (actor == state.Player && state.Floor.GetItemAt(destination) is ItemInstance ground) result.Say($"Here: {ground.Definition.Name}. Press the action button to pick it up.");
            result.ConsumesTurn = true;
            return result;
        }
    }
}
