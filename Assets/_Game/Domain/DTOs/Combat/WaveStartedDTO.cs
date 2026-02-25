namespace Game.Domain.DTOs.Combat
{
    public readonly struct WaveStartedDTO
    {
        public int WaveIndex { get; }
        public int TotalWaves { get; }

        public WaveStartedDTO(int waveIndex, int totalWaves)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
        }
    }
}
