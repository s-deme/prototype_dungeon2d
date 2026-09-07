using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public sealed class FloorState
    {
        private readonly List<CharacterState> enemies = new List<CharacterState>();
        private readonly Dictionary<GridPosition, ItemInstance> items = new Dictionary<GridPosition, ItemInstance>();
        public IEnumerable<KeyValuePair<GridPosition, ItemInstance>> Items => items;
        public DungeonMap Map { get; }
        public GridPosition Entrance { get; }
        public GridPosition Stairs { get; }
        public IReadOnlyList<CharacterState> Enemies => enemies.AsReadOnly();
        public FloorState(DungeonMap map, GridPosition entrance, GridPosition stairs)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            if (!map.IsWalkable(entrance) || !map.IsWalkable(stairs)) throw new ArgumentException("Entrance and stairs must be walkable.");
            Entrance = entrance; Stairs = stairs;
        }
        public CharacterState GetEnemyAt(GridPosition position)
        {
            foreach (var enemy in enemies) if (enemy.IsAlive && enemy.Position == position) return enemy;
            return null;
        }
        public ItemInstance GetItemAt(GridPosition position) => items.TryGetValue(position, out var item) ? item : null;
        public void PlaceItem(GridPosition position, ItemInstance item)
        {
            if (item == null || !Map.IsWalkable(position) || position == Stairs || items.ContainsKey(position)) throw new ArgumentException("Invalid item placement.");
            foreach (var existing in items.Values) if (existing.Id == item.Id) throw new ArgumentException("Duplicate item ID.");
            items.Add(position, item);
        }
        internal void RemoveItem(GridPosition position) => items.Remove(position);
        public void AddEnemy(CharacterState enemy)
        {
            if (enemy == null || enemy.Position == Entrance || enemy.Position == Stairs)
                throw new ArgumentException("Invalid enemy placement.");
            RestoreEnemy(enemy);
        }
        internal void RestoreEnemy(CharacterState enemy)
        {
            if (enemy == null || !Map.IsWalkable(enemy.Position) || GetEnemyAt(enemy.Position) != null)
                throw new ArgumentException("Invalid enemy placement.");
            foreach (var existing in enemies) if (existing.Id == enemy.Id) throw new ArgumentException("Duplicate enemy ID.");
            enemies.Add(enemy);
        }
    }
}
