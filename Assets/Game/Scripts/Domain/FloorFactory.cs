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
            var rooms = new List<GridPosition>();
            var deadEnds = new List<GridPosition>();
            for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
            {
                var position = new GridPosition(x, y);
                if (map.GetTile(position).Terrain == Terrain.Room) rooms.Add(position);
                if (map.GetTile(position).Terrain == Terrain.Corridor && IsDeadEnd(map, position)) deadEnds.Add(position);
            }
            int itemCount = rules.Items.Count == 0 ? 0 : 6;
            if (rooms.Count < rules.EnemyCount + 2 + itemCount) throw new InvalidOperationException("Not enough room floor space.");
            Shuffle(rooms, random); Shuffle(deadEnds, random);
            var stairs = rooms[1]; map.SetTile(stairs, new TileData(Terrain.Stairs, map.GetTile(stairs).RoomId));
            var floor = new FloorState(map, rooms[0], stairs);
            int difficulty = Math.Min(floorNumber - 1, 20);
            for (int i = 0; i < rules.EnemyCount; i++)
            {
                bool guardian = floorNumber == rules.FinalFloor && i == 0;
                bool archer = floorNumber == 1 ? i == rules.EnemyCount - 1 : floorNumber == 2 ? i % 3 != 0 : i % 2 != 0;
                var role = guardian ? EnemyRole.Guardian : archer ? EnemyRole.Archer : EnemyRole.Melee;
                floor.AddEnemy(new CharacterState(i + 1, guardian ? "Ember Guardian" : role == EnemyRole.Melee ? "Cinder Mite" : "Gloom Wisp",
                    rooms[i + 2], guardian ? 48 : (role == EnemyRole.Archer ? 6 : 8) + difficulty * 2,
                    guardian ? 12 : (role == EnemyRole.Archer ? 2 : 3) + difficulty / 2, guardian ? 3 : difficulty / 4, guardian ? 40 : 6 + difficulty, role));
            }
            int rewardCount = Math.Min(itemCount, deadEnds.Count);
            var rewards = new List<ItemDefinition>(rules.Items); rewards.Sort((a, b) => RewardValue(b).CompareTo(RewardValue(a)));
            int regularCount = itemCount - rewardCount;
            for (int i = 0; i < regularCount; i++)
            {
                int definition = i < 3 ? i % rules.Items.Count : (i + floorNumber - 1) % rules.Items.Count;
                floor.PlaceItem(rooms[2 + rules.EnemyCount + i], new ItemInstance(checked(floorNumber * 100 + i + 1), rules.Items[definition]));
            }
            for (int i = 0; i < rewardCount; i++)
                floor.PlaceItem(deadEnds[i], new ItemInstance(checked(floorNumber * 100 + regularCount + i + 1), rewards[i % rewards.Count]));
            return floor;
        }
        private static bool IsDeadEnd(DungeonMap map, GridPosition position)
        {
            int exits = 0;
            foreach (var direction in new[] { new GridPosition(0, 1), new GridPosition(1, 0), new GridPosition(0, -1), new GridPosition(-1, 0) })
                if (map.IsWalkable(position + direction)) exits++;
            return exits == 1;
        }
        private static void Shuffle<T>(List<T> positions, Random random)
        {
            for (int i = positions.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); var position = positions[i]; positions[i] = positions[j]; positions[j] = position; }
        }
        private static int RewardValue(ItemDefinition item) => item.Power + (item.Kind == ItemKind.Healing ? 0 : 100);
    }
}
