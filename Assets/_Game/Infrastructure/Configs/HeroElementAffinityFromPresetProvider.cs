using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Infrastructure.Configs
{
    public sealed class HeroElementAffinityFromPresetProvider : IHeroElementAffinityProvider
    {
        private static readonly CombatElement[] AllElements =
        {
            CombatElement.Physical,
            CombatElement.Fire,
            CombatElement.Cold,
            CombatElement.Lightning,
            CombatElement.Corrosion
        };

        private readonly StartingPresetSO _preset;

        public HeroElementAffinityFromPresetProvider(StartingPresetSO preset)
        {
            _preset = preset;
        }

        public IReadOnlyList<CombatElement> GetAllowedElements()
        {
            if (_preset == null || _preset.allowedElements == null || _preset.allowedElements.Count == 0)
                return AllElements;

            return _preset.allowedElements;
        }
    }
}
