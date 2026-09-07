using System.Collections.Generic;

namespace LanternDepths
{
    public enum CommandKind { Move, Wait, PickUp, Use, Drop, Equip, Unequip, Descend, Shoot, Charge, Smash, Context }
    public readonly struct PlayerCommand
    {
        public CommandKind Kind { get; }
        public GridPosition Direction { get; }
        public int ItemId { get; }
        public PlayerCommand(CommandKind kind, GridPosition direction = default, int itemId = -1)
        { Kind = kind; Direction = direction; ItemId = itemId; }
    }
    public enum EventKind { Message, Moved, Damaged, Died, PickedUp, Used, Equipped, LevelUp, FloorChanged, Victory, Telegraph }
    public readonly struct GameEvent
    {
        public EventKind Kind { get; }
        public int ActorId { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public string Message { get; }
        public int Amount { get; }
        public GameEvent(EventKind kind, string message, int actorId = -1, GridPosition from = default, GridPosition to = default, int amount = 0)
        { Kind = kind; Message = message; ActorId = actorId; From = from; To = to; Amount = amount; }
    }
    public sealed class ActionResult
    {
        private readonly List<GameEvent> events = new List<GameEvent>();
        public bool ConsumesTurn { get; internal set; }
        public bool ChangedFloor { get; internal set; }
        public IReadOnlyList<GameEvent> Events => events;
        internal void Add(GameEvent e) => events.Add(e);
        internal void Append(ActionResult result) => events.AddRange(result.events);
        internal void Say(string text) => Add(new GameEvent(EventKind.Message, text));
    }
}
