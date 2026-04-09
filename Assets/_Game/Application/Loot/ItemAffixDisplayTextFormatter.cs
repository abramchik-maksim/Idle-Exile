using System;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Application.Loot
{
    public sealed class ItemAffixDisplayTextFormatter : IItemAffixDisplayTextFormatter
    {
        private readonly IModCatalogProvider _catalog;

        public ItemAffixDisplayTextFormatter(IModCatalogProvider catalog)
        {
            _catalog = catalog;
        }

        public string FormatRolledLine(RolledItemAffix affix)
        {
            string valueStr = AffixDisplayValueString.Format(affix);

            if (_catalog.TryGetEntry(affix.ModId, out var entry) && !string.IsNullOrWhiteSpace(entry.TextTemplate))
            {
                string t = entry.TextTemplate.Trim();
                t = t.Replace("{min}-{max}", "{value}", StringComparison.Ordinal);
                t = t.Replace("{min} - {max}", "{value}", StringComparison.Ordinal);
                t = t.Replace("{value}", valueStr, StringComparison.Ordinal);
                t = t.Replace("{min}", valueStr, StringComparison.Ordinal);
                t = t.Replace("{max}", valueStr, StringComparison.Ordinal);
                return t;
            }

            return $"{affix.ModId}: {valueStr}";
        }
    }
}
