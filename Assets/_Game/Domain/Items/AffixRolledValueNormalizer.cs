using System;

namespace Game.Domain.Items
{
    /// <summary>
    /// Rounds rolled affix values once when the item is created. Combat reads final stats from
    /// <see cref="Game.Domain.Stats.Modifier"/> / hero stats — no per-enemy work here.
    /// </summary>
    public static class AffixRolledValueNormalizer
    {
        private const StringComparison Ord = StringComparison.Ordinal;

        /// <summary>Keep fractional steps (e.g. 0.5% crit on gear).</summary>
        public static float Normalize(string modId, string valueFormat, float raw)
        {
            if (string.IsNullOrEmpty(modId)) modId = string.Empty;
            string vf = valueFormat ?? string.Empty;

            if (string.Equals(modId, "Utility_MovementSpeed", Ord))
                return (float)Math.Round(raw, 2, MidpointRounding.AwayFromZero);

            if (string.Equals(modId, "Crit_Chance", Ord)
                && string.Equals(vf, "ChancePercent", StringComparison.OrdinalIgnoreCase))
                return (float)Math.Round(raw, 1, MidpointRounding.AwayFromZero);

            if (string.Equals(vf, "FlatNumber", StringComparison.OrdinalIgnoreCase))
                return (float)Math.Round(raw, 0, MidpointRounding.AwayFromZero);

            if (string.Equals(vf, "Percent", StringComparison.OrdinalIgnoreCase))
                return (float)Math.Round(raw, 0, MidpointRounding.AwayFromZero);

            if (string.Equals(vf, "ChancePercent", StringComparison.OrdinalIgnoreCase))
                return (float)Math.Round(raw, 0, MidpointRounding.AwayFromZero);

            return (float)Math.Round(raw, 2, MidpointRounding.AwayFromZero);
        }
    }
}
