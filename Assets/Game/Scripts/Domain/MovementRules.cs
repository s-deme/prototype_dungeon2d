using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public static class MovementRules
    {
        private static readonly IReadOnlyList<GridPosition> AllDirections = Array.AsReadOnly(new[]
        {
            new GridPosition(-1, -1), new GridPosition(0, -1), new GridPosition(1, -1),
            new GridPosition(-1, 0),                         new GridPosition(1, 0),
            new GridPosition(-1, 1),  new GridPosition(0, 1),  new GridPosition(1, 1)
        });

        public static IEnumerable<GridPosition> Directions => AllDirections;
        public static bool CanReachAdjacent(DungeonMap map, GridPosition from, GridPosition to)
        {
            if (from.Distance(to) != 1 || !map.IsWalkable(from) || !map.IsWalkable(to)) return false;
            var d = to - from;
            return d.X == 0 || d.Y == 0 ||
                (map.IsWalkable(new GridPosition(from.X + d.X, from.Y)) && map.IsWalkable(new GridPosition(from.X, from.Y + d.Y)));
        }
        public static bool CanMove(FloorState floor, CharacterState actor, CharacterState player, GridPosition to)
        {
            if (!actor.IsAlive || !CanReachAdjacent(floor.Map, actor.Position, to)) return false;
            if (player != actor && player.IsAlive && player.Position == to) return false;
            return floor.GetEnemyAt(to) == null;
        }
    }
}
