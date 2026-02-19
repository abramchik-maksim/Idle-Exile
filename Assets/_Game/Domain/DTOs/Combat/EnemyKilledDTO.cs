namespace Game.Domain.DTOs.Combat
{
    public readonly struct EnemyKilledDTO
    {
        public string EnemyDefinitionId { get; }
        public int WaveIndex { get; }
        public EnemyKilledDTO(string enemyDefinitionId, int waveIndex)
        {
            EnemyDefinitionId = enemyDefinitionId;
            WaveIndex = waveIndex;
        }
    }
}
