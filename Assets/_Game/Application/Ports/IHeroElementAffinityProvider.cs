using System.Collections.Generic;
using Game.Domain.Items;

namespace Game.Application.Ports
{
    /// <summary>Combat elements allowed for this hero (item affix filtering).</summary>
    public interface IHeroElementAffinityProvider
    {
        /// <summary>
        /// Non-empty list. Empty preset means all five elements (legacy behavior).
        /// </summary>
        IReadOnlyList<CombatElement> GetAllowedElements();
    }
}
