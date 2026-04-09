using System;
using Game.Domain.Items;

namespace Game.Application.Loot
{
    /// <summary>Formats stored rolled values for UI (matches <see cref="AffixRolledValueNormalizer"/> rules).</summary>
    public static class AffixDisplayValueString
    {
        public static string Format(RolledItemAffix affix) => Format(affix.ModId, affix.ValueFormat, affix.RolledValue);

        public static string Format(string modId, string valueFormat, float value)
        {
            if (string.IsNullOrEmpty(modId)) modId = string.Empty;
            string vf = valueFormat ?? string.Empty;

            if (string.Equals(modId, "Utility_MovementSpeed", StringComparison.Ordinal))
                return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

            if (string.Equals(modId, "Crit_Chance", StringComparison.Ordinal)
                && string.Equals(vf, "ChancePercent", StringComparison.OrdinalIgnoreCase))
                return value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

            if (string.Equals(vf, "FlatNumber", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vf, "Percent", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vf, "ChancePercent", StringComparison.OrdinalIgnoreCase))
                return value.ToString("0", System.Globalization.CultureInfo.InvariantCulture);

            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
