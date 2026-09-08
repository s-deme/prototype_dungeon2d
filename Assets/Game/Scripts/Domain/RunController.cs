using System;

namespace LanternDepths
{
    public sealed class RunController
    {
        private readonly RunRules rules;
        private readonly FloorFactory factory;
        private readonly IEnemyBrain brain;
        private RunRandom random;
        private TurnProcessor turns;
        public int Seed { get; private set; }
        public int FinalFloor => rules.FinalFloor;
        public RunState State { get; private set; }
        public RunController(RunRules rules, IDungeonGenerator generator = null, IEnemyBrain brain = null)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            factory = new FloorFactory(generator ?? new RoomCorridorGenerator(), rules);
            this.brain = brain ?? new BasicEnemyBrain();
        }
        public void StartNewRun(int seed)
        {
            var nextRandom = new RunRandom(unchecked((uint)seed));
            var floor = factory.Create(nextRandom, 1);
            var player = new CharacterState(0, "Wayfarer", floor.Entrance, rules.PlayerHp, rules.PlayerAttack, rules.PlayerDefense);
            var next = new RunState(player, floor, new PlayerProgression(rules.Progression));
            InitializeRun(next, seed, nextRandom);
        }
        public ActionResult Execute(PlayerCommand command)
        {
            if (State == null) throw new InvalidOperationException("Start a run first.");
            return turns.Execute(State, command);
        }
        public byte[] Save() => RunSave.Write(rules, State, Seed, random.State);
        public void Load(byte[] data)
        {
            var next = RunSave.Read(data, rules, out int seed, out uint randomState);
            InitializeRun(next, seed, new RunRandom(randomState));
        }
        private void InitializeRun(RunState state, int seed, RunRandom nextRandom)
        {
            State = state; Seed = seed; random = nextRandom;
            turns = new TurnProcessor(new ActionResolver(Descend), brain, random);
        }
        private void Descend(RunState state)
        {
            if (state.FloorNumber == rules.FinalFloor) { state.IsVictory = true; return; }
            var next = factory.Create(random, checked(state.FloorNumber + 1));
            state.Floor = next; state.FloorNumber++; state.Player.Position = next.Entrance;
        }
    }
}
