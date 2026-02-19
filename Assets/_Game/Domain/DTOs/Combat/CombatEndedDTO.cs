namespace Game.Domain.DTOs.Combat
{
    public readonly struct CombatEndedDTO
    {
        public int WaveIndex { get; }
        public bool Victory { get; }
        public CombatEndedDTO(int waveIndex, bool victory)
        {
            WaveIndex = waveIndex;
            Victory = victory;
        }
    }
}
