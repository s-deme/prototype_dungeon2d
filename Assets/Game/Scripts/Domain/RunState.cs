using System;

namespace LanternDepths
{
    public sealed class RunState
    {
        public string RunId { get; internal set; } = Guid.NewGuid().ToString("N");
        public CharacterState Player { get; }
        public FloorState Floor { get; internal set; }
        public int FloorNumber { get; internal set; } = 1;
        public int TurnNumber { get; internal set; }
        public bool IsGameOver => !Player.IsAlive;
        public bool IsVictory { get; internal set; }
        public bool IsFinished => IsGameOver || IsVictory;
        public PlayerProgression Progression { get; }
        public Inventory Inventory { get; } = new Inventory();
        public Equipment Equipment { get; } = new Equipment();
        public RunState(CharacterState player, FloorState floor, PlayerProgression progression = null)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Floor = floor ?? throw new ArgumentNullException(nameof(floor));
            Progression = progression;
            if (!floor.Map.IsWalkable(player.Position) || floor.GetEnemyAt(player.Position) != null)
                throw new ArgumentException("Invalid player placement.");
        }
    }
}
