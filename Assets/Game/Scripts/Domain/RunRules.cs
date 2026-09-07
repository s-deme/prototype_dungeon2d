using System;

namespace LanternDepths
{
    public sealed class RunRules
    {
        public int Width { get; }
        public int Height { get; }
        public int Rooms { get; }
        public int EnemyCount { get; }
        public int PlayerHp { get; }
        public int PlayerAttack { get; }
        public int PlayerDefense { get; }
        public int FinalFloor { get; }
        public ProgressionRules Progression { get; }
        public System.Collections.Generic.IReadOnlyList<ItemDefinition> Items { get; }
        public RunRules(int width, int height, int rooms, int enemyCount, int playerHp, int playerAttack, int playerDefense, ProgressionRules progression, ItemDefinition[] items = null, int finalFloor = 5)
        {
            if (width < 16 || width > 256 || height < 12 || height > 256 || rooms < 2 || rooms > 40 || enemyCount < 0 || enemyCount > 30 || playerHp < 1 || playerAttack < 0 || playerDefense < 0)
                throw new ArgumentException("Invalid run rules.");
            Width = width; Height = height; Rooms = rooms; EnemyCount = enemyCount;
            if (finalFloor < 1 || finalFloor > 100) throw new ArgumentOutOfRangeException(nameof(finalFloor));
            FinalFloor = finalFloor;
            PlayerHp = playerHp; PlayerAttack = playerAttack; PlayerDefense = playerDefense;
            Progression = progression ?? throw new ArgumentNullException(nameof(progression));
            var definitions = items ?? Array.Empty<ItemDefinition>();
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var item in definitions) if (item == null || !ids.Add(item.Id)) throw new ArgumentException("Item IDs must be unique.");
            Items = Array.AsReadOnly((ItemDefinition[])definitions.Clone());
        }
    }
}
