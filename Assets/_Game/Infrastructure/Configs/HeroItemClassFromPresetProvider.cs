using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Infrastructure.Configs
{
    public sealed class HeroItemClassFromPresetProvider : IHeroItemClassProvider
    {
        private readonly StartingPresetSO _preset;

        public HeroItemClassFromPresetProvider(StartingPresetSO preset)
        {
            _preset = preset;
        }

        public HeroItemClass GetHeroItemClass() =>
            _preset != null ? _preset.heroItemClass : HeroItemClass.Warrior;
    }
}
