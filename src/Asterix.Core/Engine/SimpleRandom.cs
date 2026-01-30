using System;
using System.Security.Cryptography;
using Asterix.Core.Interfaces;

namespace Asterix.Core.Engine
{
    public class SimpleRandom : IRandomSource
    {
        private readonly Random _rnd;
        public ulong Seed { get; }

        public SimpleRandom(ulong seed)
        {
            Seed = seed;
            _rnd = new Random((int)(seed & 0xFFFFFFFF));
        }

        public int NextInt(int exclusiveUpperBound) => _rnd.Next(exclusiveUpperBound);
        public double NextDouble() => _rnd.NextDouble();
        public byte[] GetState()
        {
            // Minimal state snapshot: serialize seed (not full Random state)
            return BitConverter.GetBytes(Seed);
        }

        public IRandomSource Fork(ulong streamId)
        {
            // Simple fork: combine seeds — replace later with strong fork/jump
            unchecked
            {
                var newSeed = Seed ^ (streamId * 0x9E3779B97F4A7C15UL);
                return new SimpleRandom(newSeed);
            }
        }
    }
}
