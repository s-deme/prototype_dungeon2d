using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public sealed class BasicEnemyBrain : IEnemyBrain
    {
        private readonly int sightRange;
        public BasicEnemyBrain(int sightRange = 8)
        {
            if (sightRange < 1) throw new ArgumentOutOfRangeException(nameof(sightRange));
            this.sightRange = sightRange;
        }
        public PlayerCommand ChooseAction(RunState state, CharacterState enemy, Random random)
        {
            var target = state.Player.Position;
            var distanceToTarget = enemy.Position.Distance(target);
            if (enemy.Role == EnemyRole.Guardian && enemy.Charged) return new PlayerCommand(CommandKind.Smash);
            if (enemy.Role == EnemyRole.Archer)
            {
                if (distanceToTarget <= 1)
                    foreach (var direction in MovementRules.Directions)
                        if ((enemy.Position + direction).Distance(target) > 1 && MovementRules.CanMove(state.Floor, enemy, state.Player, enemy.Position + direction))
                            return new PlayerCommand(CommandKind.Move, direction);
                if (distanceToTarget <= 4 && HasLineOfSight(state.Floor.Map, enemy.Position, target))
                    return new PlayerCommand(CommandKind.Shoot);
            }
            if (MovementRules.CanReachAdjacent(state.Floor.Map, enemy.Position, target))
                return new PlayerCommand(enemy.Role == EnemyRole.Guardian ? CommandKind.Charge : CommandKind.Move, target - enemy.Position);
            if (distanceToTarget <= sightRange && HasLineOfSight(state.Floor.Map, enemy.Position, target))
            {
                var step = FindStep(state.Floor, enemy, state.Player);
                return step == enemy.Position ? new PlayerCommand(CommandKind.Wait) : new PlayerCommand(CommandKind.Move, step - enemy.Position);
            }
            var options = new List<GridPosition> { default };
            foreach (var direction in MovementRules.Directions)
                if (MovementRules.CanMove(state.Floor, enemy, state.Player, enemy.Position + direction)) options.Add(direction);
            var chosen = options[random.Next(options.Count)];
            return chosen == default ? new PlayerCommand(CommandKind.Wait) : new PlayerCommand(CommandKind.Move, chosen);
        }
        public static bool HasLineOfSight(DungeonMap map, GridPosition from, GridPosition to)
        {
            int x = from.X, y = from.Y, dx = Math.Abs(to.X - x), dy = Math.Abs(to.Y - y);
            int sx = Math.Sign(to.X - x), sy = Math.Sign(to.Y - y), error = dx - dy;
            while (x != to.X || y != to.Y)
            {
                var previous = new GridPosition(x, y);
                int twice = 2 * error;
                if (twice > -dy) { error -= dy; x += sx; }
                if (twice < dx) { error += dx; y += sy; }
                if (!MovementRules.CanReachAdjacent(map, previous, new GridPosition(x, y))) return false;
            }
            return true;
        }
        public static GridPosition FindStep(FloorState floor, CharacterState enemy, CharacterState player)
        {
            // ponytail: one BFS per pursuing enemy; share a distance field if larger enemy counts become costly.
            var queue = new Queue<GridPosition>();
            var previous = new Dictionary<GridPosition, GridPosition>();
            queue.Enqueue(enemy.Position); previous.Add(enemy.Position, enemy.Position);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in MovementRules.Directions)
                {
                    var next = p + d;
                    if (previous.ContainsKey(next) || !MovementRules.CanReachAdjacent(floor.Map, p, next)) continue;
                    if (floor.GetEnemyAt(next) != null) continue;
                    previous.Add(next, p);
                    if (next == player.Position)
                    {
                        while (previous[next] != enemy.Position) next = previous[next];
                        return next;
                    }
                    queue.Enqueue(next);
                }
            }
            return enemy.Position;
        }
    }
}
