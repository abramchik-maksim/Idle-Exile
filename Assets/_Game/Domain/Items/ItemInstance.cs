using System;
using System.Collections.Generic;
using Game.Domain.Stats;

namespace Game.Domain.Items
{
    public sealed class ItemInstance
    {
        public string Uid { get; }
        public ItemDefinition Definition { get; }
        /// <summary>Rolled affix lines from the item affix pool (source of truth for display).</summary>
        public IReadOnlyList<RolledItemAffix> RolledAffixes { get; }
        /// <summary>Stat modifiers derived from <see cref="RolledAffixes"/> (may be partial until all mods are implemented).</summary>
        public IReadOnlyList<Modifier> RolledModifiers { get; }

        public ItemInstance(ItemDefinition definition, List<Modifier> rolledModifiers)
            : this(definition, new List<RolledItemAffix>(), rolledModifiers)
        {
        }

        public ItemInstance(ItemDefinition definition, List<RolledItemAffix> rolledAffixes, List<Modifier> rolledModifiers)
        {
            Uid = Guid.NewGuid().ToString("N");
            Definition = definition;
            RolledAffixes = (rolledAffixes ?? new List<RolledItemAffix>()).AsReadOnly();
            RolledModifiers = (rolledModifiers ?? new List<Modifier>()).AsReadOnly();
        }

        public ItemInstance(string uid, ItemDefinition definition, List<Modifier> rolledModifiers)
            : this(uid, definition, new List<RolledItemAffix>(), rolledModifiers)
        {
        }

        public ItemInstance(string uid, ItemDefinition definition, List<RolledItemAffix> rolledAffixes, List<Modifier> rolledModifiers)
        {
            Uid = uid;
            Definition = definition;
            RolledAffixes = (rolledAffixes ?? new List<RolledItemAffix>()).AsReadOnly();
            RolledModifiers = (rolledModifiers ?? new List<Modifier>()).AsReadOnly();
        }

        public IEnumerable<Modifier> GetAllModifiers()
        {
            foreach (var m in Definition.ImplicitModifiers) yield return m;
            foreach (var m in RolledModifiers) yield return m;
        }
    }
}
