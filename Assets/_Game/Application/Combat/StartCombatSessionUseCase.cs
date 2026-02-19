using Game.Application.Ports;
using Game.Domain.Characters;

namespace Game.Application.Combat
{
    public sealed class StartCombatSessionUseCase
    {
        private readonly IConfigProvider _config;

        public StartCombatSessionUseCase(IConfigProvider config)
        {
            _config = config;
        }

        public CombatSession Execute(HeroState hero, int waveIndex)
        {
            float hp = _config.GetEnemyHealthBase(waveIndex);
            float dmg = _config.GetEnemyDamageBase(waveIndex);

            var enemy = new EnemyState("skeleton", waveIndex, hp, dmg);
            return new CombatSession(hero, enemy, waveIndex);
        }
    }

    public sealed class CombatSession
    {
        public HeroState Hero { get; }
        public EnemyState Enemy { get; }
        public int WaveIndex { get; }

        public CombatSession(HeroState hero, EnemyState enemy, int waveIndex)
        {
            Hero = hero;
            Enemy = enemy;
            WaveIndex = waveIndex;
        }
    }
}
