using System;
using System.Collections.Generic;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Infrastructure.ItemAffixes
{
    public sealed class ScriptableObjectModCatalogProvider : IModCatalogProvider
    {
        private readonly Dictionary<string, ModCatalogEntry> _byModId =
            new(StringComparer.Ordinal);

        public ScriptableObjectModCatalogProvider(ItemAffixDatabaseSO database)
        {
            if (database?.modCatalogRows == null) return;

            foreach (var r in database.modCatalogRows)
            {
                if (string.IsNullOrWhiteSpace(r.modId)) continue;
                _byModId[r.modId.Trim()] = new ModCatalogEntry(r.modId.Trim(), r.valueType ?? string.Empty, r.textTemplate ?? string.Empty);
            }
        }

        public bool TryGetEntry(string modId, out ModCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(modId) || !_byModId.TryGetValue(modId, out entry))
            {
                entry = default;
                return false;
            }

            return !string.IsNullOrWhiteSpace(entry.TextTemplate);
        }
    }
}
