using System;

namespace LanternDepths
{
    public static class MovementRules
    {
        public static System.Collections.Generic.IEnumerable<GridPosition> Directions
        {
            get
            {
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                    if (x != 0 || y != 0) yield return new GridPosition(x, y);
            }
        }
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
