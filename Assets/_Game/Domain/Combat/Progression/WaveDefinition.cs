using System.Collections.Generic;

namespace Game.Domain.Combat.Progression
{
    public readonly struct WaveDefinition
    {
        public IReadOnlyList<WaveSpawnEntry> Spawns { get; }
        public float DelayBeforeWave { get; }

        public WaveDefinition(IReadOnlyList<WaveSpawnEntry> spawns, float delayBeforeWave)
        {
            Spawns = spawns;
            DelayBeforeWave = delayBeforeWave;
        }
    }
}
