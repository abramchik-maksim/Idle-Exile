using Game.Domain.Stats;

namespace Game.Domain.Characters
{
    public sealed class EnemyState
    {
        public string DefinitionId { get; }
        public int WaveIndex { get; }
        public StatCollection Stats { get; }

        public EnemyState(string definitionId, int waveIndex, float healthBase, float damageBase)
        {
            DefinitionId = definitionId;
            WaveIndex = waveIndex;
            Stats = new StatCollection();

            Stats.SetBase(StatType.MaxHealth, healthBase);
            Stats.SetBase(StatType.CurrentHealth, healthBase);
            Stats.SetBase(StatType.PhysicalDamage, damageBase);
            Stats.SetBase(StatType.AttackSpeed, 0.8f);
            Stats.SetBase(StatType.Armor, 2f);
            Stats.SetBase(StatType.MovementSpeed, 2f);
        }
    }
}
