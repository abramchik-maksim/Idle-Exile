using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;
using Game.Domain.Skills;

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

        public IReadOnlyList<WeaponType> GetAllowedWeaponTypes() =>
            _preset != null && _preset.allowedWeaponTypes.Count > 0
                ? _preset.allowedWeaponTypes
                : Array.Empty<WeaponType>();
    }
}
