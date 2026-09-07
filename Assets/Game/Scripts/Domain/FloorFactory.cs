using System;
using System.Collections.Generic;

namespace LanternDepths
{
    public sealed class FloorFactory
    {
        private readonly IDungeonGenerator generator;
        private readonly RunRules rules;
        public FloorFactory(IDungeonGenerator generator, RunRules rules)
        { this.generator = generator ?? throw new ArgumentNullException(nameof(generator)); this.rules = rules ?? throw new ArgumentNullException(nameof(rules)); }
        public FloorState Create(Random random, int floorNumber)
        {
            if (floorNumber < 1) throw new ArgumentOutOfRangeException(nameof(floorNumber));
            var map = generator.Generate(random, rules.Width, rules.Height, rules.Rooms);
            var available = new List<GridPosition>();
            for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
                if (map.IsWalkable(new GridPosition(x, y))) available.Add(new GridPosition(x, y));
            int itemCount = rules.Items.Count == 0 ? 0 : 6;
            if (available.Count < rules.EnemyCount + 2 + itemCount) throw new InvalidOperationException("Not enough floor space.");
            for (int i = available.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); var temp = available[i]; available[i] = available[j]; available[j] = temp; }
            var stairs = available[1]; map.SetTile(stairs, new TileData(Terrain.Stairs, map.GetTile(stairs).RoomId));
            var floor = new FloorState(map, available[0], stairs);
            int difficulty = Math.Min(floorNumber - 1, 20);
            for (int i = 0; i < rules.EnemyCount; i++)
            {
                bool guardian = floorNumber == rules.FinalFloor && i == 0;
                bool archer = floorNumber == 1 ? i == rules.EnemyCount - 1 : floorNumber == 2 ? i % 3 != 0 : i % 2 != 0;
                var role = guardian ? EnemyRole.Guardian : archer ? EnemyRole.Archer : EnemyRole.Melee;
                floor.AddEnemy(new CharacterState(i + 1, guardian ? "Ember Guardian" : role == EnemyRole.Melee ? "Cinder Mite" : "Gloom Wisp",
                    available[i + 2], guardian ? 48 : (role == EnemyRole.Archer ? 6 : 8) + difficulty * 2,
                    guardian ? 12 : (role == EnemyRole.Archer ? 2 : 3) + difficulty / 2, guardian ? 3 : difficulty / 4, guardian ? 40 : 6 + difficulty, role));
            }
            for (int i = 0; i < itemCount; i++)
            {
                // Keep a tonic and basic equipment available; later slots vary by floor.
                int definition = i < 3 ? i % rules.Items.Count : (i + floorNumber - 1) % rules.Items.Count;
                floor.PlaceItem(available[2 + rules.EnemyCount + i], new ItemInstance(checked(floorNumber * 100 + i + 1), rules.Items[definition]));
            }
            return floor;
        }
    }
}
