namespace Game.Domain.DTOs.Combat
{
    public readonly struct CombatStartedDTO
    {
        public int WaveIndex { get; }
        public CombatStartedDTO(int waveIndex) => WaveIndex = waveIndex;
    }
}
