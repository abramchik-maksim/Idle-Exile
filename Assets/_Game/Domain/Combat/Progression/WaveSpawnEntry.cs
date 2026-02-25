namespace Game.Domain.Combat.Progression
{
    public readonly struct WaveSpawnEntry
    {
        public string EnemyDefinitionId { get; }
        public int Count { get; }

        public WaveSpawnEntry(string enemyDefinitionId, int count)
        {
            EnemyDefinitionId = enemyDefinitionId;
            Count = count;
        }
    }
}
