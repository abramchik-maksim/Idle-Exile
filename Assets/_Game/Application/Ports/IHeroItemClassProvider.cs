using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.Skills;

namespace Game.Application.Ports
{
    public interface IHeroItemClassProvider
    {
        HeroItemClass GetHeroItemClass();

        /// <summary>
        /// Weapon types this hero class can find as drops.
        /// Empty list means all weapon types are allowed.
        /// </summary>
        IReadOnlyList<WeaponType> GetAllowedWeaponTypes();
    }
}
