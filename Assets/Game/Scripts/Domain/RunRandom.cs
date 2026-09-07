using System;

namespace LanternDepths
{
    // Explicit state keeps saved runs independent of System.Random's private implementation.
    internal sealed class RunRandom : Random
    {
        public uint State { get; private set; }
        public RunRandom(uint state) { State = state == 0 ? 0x6D2B79F5u : state; }
        protected override double Sample()
        {
            uint x = State; x ^= x << 13; x ^= x >> 17; x ^= x << 5; State = x;
            return x / 4294967296.0;
        }
        public override int Next() => (int)(Sample() * int.MaxValue);
        public override int Next(int maxValue) => Next(0, maxValue);
        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(maxValue));
            return (int)(minValue + (long)(Sample() * ((long)maxValue - minValue)));
        }
        public override double NextDouble() => Sample();
        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)Next(256);
        }
    }
}
