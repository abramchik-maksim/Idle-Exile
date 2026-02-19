using System.Collections.Generic;
using Game.Domain.Characters;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Application.Stats
{
    public sealed class CalculateHeroStatsUseCase
    {
        public Dictionary<StatType, float> Execute(HeroState hero, IReadOnlyDictionary<EquipmentSlotType, ItemInstance> equipped)
        {
            hero.Stats.ClearModifiers();

            foreach (var kvp in equipped)
            {
                string source = kvp.Value.Uid;
                foreach (var mod in kvp.Value.GetAllModifiers())
                {
                    hero.Stats.AddModifier(new Modifier(mod.Stat, mod.Type, mod.Value, source));
                }
            }

            return hero.Stats.GetAllFinal();
        }
    }
}
