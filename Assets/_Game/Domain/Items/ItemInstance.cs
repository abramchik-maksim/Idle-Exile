using System;
using System.Collections.Generic;
using Game.Domain.Stats;

namespace Game.Domain.Items
{
    public sealed class ItemInstance
    {
        public string Uid { get; }
        public ItemDefinition Definition { get; }
        public List<Modifier> RolledModifiers { get; }

        public ItemInstance(ItemDefinition definition, List<Modifier> rolledModifiers)
        {
            Uid = Guid.NewGuid().ToString("N");
            Definition = definition;
            RolledModifiers = rolledModifiers;
        }

        public ItemInstance(string uid, ItemDefinition definition, List<Modifier> rolledModifiers)
        {
            Uid = uid;
            Definition = definition;
            RolledModifiers = rolledModifiers;
        }

        public IEnumerable<Modifier> GetAllModifiers()
        {
            foreach (var m in Definition.ImplicitModifiers) yield return m;
            foreach (var m in RolledModifiers) yield return m;
        }
    }
}
