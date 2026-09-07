using System.Linq;
using System.Collections.Generic;

namespace LanternDepths
{
    public sealed class ItemActionResolver
    {
        public ActionResult Resolve(RunState state, PlayerCommand command)
        {
            var result = new ActionResult();
            var position = state.Player.Position;
            if (command.Kind == CommandKind.PickUp)
            {
                var ground = state.Floor.GetItemAt(position);
                if (ground == null) { result.Say("There is no item here."); return result; }
                if (!state.Inventory.TryAdd(ground)) { result.Say("Your pack is full (20 items)."); return result; }
                state.Floor.RemoveItem(position);
                result.Add(new GameEvent(EventKind.PickedUp, $"Picked up {ground.Definition.Name}."));
                result.ConsumesTurn = true; return result;
            }
            var item = state.Inventory.Find(command.ItemId);
            if (item == null) { result.Say("That item is no longer in your pack."); return result; }
            switch (command.Kind)
            {
                case CommandKind.Use:
                    if (item.Definition.Kind == ItemKind.Blast)
                    {
                        var targets = state.Floor.Enemies.Where(e => e.IsAlive && e.Position.Distance(position) <= 2 && BasicEnemyBrain.HasLineOfSight(state.Floor.Map, position, e.Position)).ToArray();
                        if (targets.Length == 0) { result.Say("No enemy is within blast range (2 tiles)."); return result; }
                        foreach (var target in targets) CombatResolver.DealDamage(state, state.Player, target, item.Definition.Power, result);
                        state.Inventory.Remove(item); result.Add(new GameEvent(EventKind.Used, $"Used {item.Definition.Name}.")); break;
                    }
                    if (item.Definition.Kind == ItemKind.Blink)
                    {
                        var enemies = state.Floor.Enemies.Where(e => e.IsAlive).ToArray();
                        if (enemies.Length == 0) { result.Say("There is no danger to escape."); return result; }
                        int Safety(GridPosition p) => enemies.Min(e => e.Position.Distance(p));
                        var destination = position; int best = Safety(position);
                        var visited = new HashSet<GridPosition> { position }; var frontier = new List<GridPosition> { position };
                        for (int step = 0; step < item.Definition.Power; step++)
                        {
                            var next = new List<GridPosition>();
                            foreach (var from in frontier) foreach (var direction in MovementRules.Directions)
                            {
                                var to = from + direction;
                                if (!MovementRules.CanReachAdjacent(state.Floor.Map, from, to) || state.Floor.GetEnemyAt(to) != null || !visited.Add(to)) continue;
                                next.Add(to); int safety = Safety(to);
                                if (safety > best) { best = safety; destination = to; }
                            }
                            frontier = next;
                        }
                        if (destination == position) { result.Say("No safer escape tile is reachable."); return result; }
                        state.Player.Position = destination; state.Inventory.Remove(item);
                        result.Add(new GameEvent(EventKind.Moved, "The rune carries you away from danger.", state.Player.Id, position, destination));
                        result.Add(new GameEvent(EventKind.Used, $"Used {item.Definition.Name}.")); break;
                    }
                    if (item.Definition.Kind != ItemKind.Healing) { result.Say("Equip this item to use it."); return result; }
                    if (state.Player.Hp == state.Player.MaxHp) { result.Say("Your health is already full."); return result; }
                    int healed = state.Player.Heal(item.Definition.Power); state.Inventory.Remove(item);
                    result.Add(new GameEvent(EventKind.Used, $"Used {item.Definition.Name}. Recovered {healed} HP.", state.Player.Id, amount: healed));
                    break;
                case CommandKind.Drop:
                    if (!TryFindDropPosition(state.Floor, position, out var dropPosition))
                    { result.Say("There is no room to drop an item here."); return result; }
                    state.Floor.PlaceItem(dropPosition, item);
                    state.Equipment.Unequip(item.Id); state.Inventory.Remove(item);
                    result.Say($"Dropped {item.Definition.Name}."); break;
                case CommandKind.Equip:
                    if (!state.Equipment.Equip(item)) { result.Say("That item cannot be equipped, or is already equipped."); return result; }
                    result.Add(new GameEvent(EventKind.Equipped, $"Equipped {item.Definition.Name}.")); break;
                case CommandKind.Unequip:
                    if (!state.Equipment.Unequip(item.Id)) { result.Say("That item is not equipped."); return result; }
                    result.Add(new GameEvent(EventKind.Equipped, $"Unequipped {item.Definition.Name}.")); break;
                default: result.Say("That item action is unavailable."); return result;
            }
            result.ConsumesTurn = true; return result;
        }
        private static bool TryFindDropPosition(FloorState floor, GridPosition origin, out GridPosition dropPosition)
        {
            var visited = new HashSet<GridPosition> { origin };
            var queue = new Queue<GridPosition>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                var position = queue.Dequeue();
                if (position != floor.Stairs && floor.GetItemAt(position) == null && floor.GetEnemyAt(position) == null)
                { dropPosition = position; return true; }
                foreach (var direction in MovementRules.Directions)
                {
                    var next = position + direction;
                    if (visited.Add(next) && MovementRules.CanReachAdjacent(floor.Map, position, next)) queue.Enqueue(next);
                }
            }
            dropPosition = default; return false;
        }
    }
}
