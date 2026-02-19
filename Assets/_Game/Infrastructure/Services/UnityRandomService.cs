using Game.Application.Ports;

namespace Game.Infrastructure.Services
{
    public sealed class UnityRandomService : IRandomService
    {
        private readonly System.Random _rng;

        public UnityRandomService(int? seed = null)
        {
            _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        public double NextDouble() => _rng.NextDouble();
        public int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public float NextFloat(float min, float max) => (float)(_rng.NextDouble() * (max - min) + min);
    }
}
