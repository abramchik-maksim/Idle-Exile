using System;
using System.Collections.Generic;

namespace Game.Domain.Items
{
    public static class ModCatalogElementExtensions
    {
        public static bool TryParse(string text, out ModCatalogElement element)
        {
            element = ModCatalogElement.NonSpecific;
            if (string.IsNullOrWhiteSpace(text)) return true;

            var s = text.Trim();
            if (string.Equals(s, "NonSpecific", StringComparison.OrdinalIgnoreCase))
            {
                element = ModCatalogElement.NonSpecific;
                return true;
            }

            if (Enum.TryParse(s, true, out CombatElement combat))
            {
                element = FromCombatElement(combat);
                return true;
            }

            return false;
        }

        public static ModCatalogElement FromCombatElement(CombatElement combat) =>
            combat switch
            {
                CombatElement.Physical => ModCatalogElement.Physical,
                CombatElement.Fire => ModCatalogElement.Fire,
                CombatElement.Cold => ModCatalogElement.Cold,
                CombatElement.Lightning => ModCatalogElement.Lightning,
                CombatElement.Corrosion => ModCatalogElement.Corrosion,
                _ => ModCatalogElement.NonSpecific
            };

        public static bool TryToCombatElement(this ModCatalogElement tag, out CombatElement combat)
        {
            switch (tag)
            {
                case ModCatalogElement.NonSpecific:
                    combat = default;
                    return false;
                case ModCatalogElement.Physical:
                    combat = CombatElement.Physical;
                    return true;
                case ModCatalogElement.Fire:
                    combat = CombatElement.Fire;
                    return true;
                case ModCatalogElement.Cold:
                    combat = CombatElement.Cold;
                    return true;
                case ModCatalogElement.Lightning:
                    combat = CombatElement.Lightning;
                    return true;
                case ModCatalogElement.Corrosion:
                    combat = CombatElement.Corrosion;
                    return true;
                default:
                    combat = default;
                    return false;
            }
        }

        /// <summary>
        /// True if the mod is allowed: NonSpecific, or its element is in the hero list.
        /// </summary>
        public static bool IsAllowedForHero(
            this ModCatalogElement modTag,
            IReadOnlyList<CombatElement> allowedElements)
        {
            if (modTag == ModCatalogElement.NonSpecific) return true;
            if (!modTag.TryToCombatElement(out var ce)) return false;

            for (int i = 0; i < allowedElements.Count; i++)
            {
                if (allowedElements[i] == ce) return true;
            }

            return false;
        }
    }
}
