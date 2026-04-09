using System.Collections.Generic;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Application.Ports
{
    /// <summary>
    /// Maps rolled affix lines to <see cref="Modifier"/> for stats that exist in <see cref="StatType"/>.
    /// Unimplemented mods yield no modifiers (rolled lines still show on tooltip).
    /// </summary>
    public interface IItemAffixModifierResolver
    {
        IReadOnlyList<Modifier> ResolveModifiers(RolledItemAffix affix);
    }
}
