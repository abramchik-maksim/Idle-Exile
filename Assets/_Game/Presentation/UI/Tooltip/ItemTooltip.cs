using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Domain.Stats;

namespace Game.Presentation.UI.Tooltip
{
    public static class ItemTooltip
    {
        private static VisualElement _tooltip;
        private static VisualElement _currentOwner;

        public static void Show(VisualElement owner, ItemInstance item, VisualElement root)
        {
            Hide();
            if (item == null) return;

            _currentOwner = owner;
            _tooltip = new VisualElement();
            _tooltip.AddToClassList("item-tooltip");
            _tooltip.pickingMode = PickingMode.Ignore;

            var title = new Label(item.Definition.Name);
            title.AddToClassList("item-tooltip__title");
            title.AddToClassList(RarityClass(item.Definition.Rarity));
            title.pickingMode = PickingMode.Ignore;
            _tooltip.Add(title);

            if (item.Definition.Rarity != Rarity.Normal)
            {
                var rarityLabel = new Label(item.Definition.Rarity.ToString());
                rarityLabel.AddToClassList("item-tooltip__rarity");
                rarityLabel.AddToClassList(RarityClass(item.Definition.Rarity));
                rarityLabel.pickingMode = PickingMode.Ignore;
                _tooltip.Add(rarityLabel);
            }

            if (item.Definition.Slot != EquipmentSlotType.None)
            {
                var slotLabel = new Label(FormatSlotName(item.Definition.Slot));
                slotLabel.AddToClassList("item-tooltip__slot");
                slotLabel.pickingMode = PickingMode.Ignore;
                _tooltip.Add(slotLabel);
            }

            var allMods = item.GetAllModifiers();
            bool hasMods = false;
            foreach (var mod in allMods)
            {
                if (!hasMods)
                {
                    var separator = new VisualElement();
                    separator.AddToClassList("item-tooltip__separator");
                    separator.pickingMode = PickingMode.Ignore;
                    _tooltip.Add(separator);
                    hasMods = true;
                }

                string prefix = mod.Source == "implicit" ? "" : "+";
                var modLabel = new Label($"{prefix}{FormatModValue(mod)} {FormatStatName(mod.Stat)}");
                modLabel.AddToClassList("item-tooltip__mod");
                if (mod.Source == "implicit")
                    modLabel.AddToClassList("item-tooltip__mod--implicit");
                modLabel.pickingMode = PickingMode.Ignore;
                _tooltip.Add(modLabel);
            }

            _tooltip.style.position = Position.Absolute;

            var ownerRect = owner.worldBound;
            _tooltip.style.left = ownerRect.xMax + 8;
            _tooltip.style.top = ownerRect.yMin;

            root.Add(_tooltip);

            _tooltip.RegisterCallback<GeometryChangedEvent>(_ => ClampToScreen(_tooltip, root));
        }

        public static void Hide()
        {
            if (_tooltip == null) return;
            _tooltip.RemoveFromHierarchy();
            _tooltip = null;
            _currentOwner = null;
        }

        public static bool IsShownFor(VisualElement owner) => _currentOwner == owner;

        private static void ClampToScreen(VisualElement tooltip, VisualElement root)
        {
            var rootBounds = root.worldBound;
            var tipBounds = tooltip.worldBound;

            if (tipBounds.xMax > rootBounds.xMax)
                tooltip.style.left = tipBounds.x - (tipBounds.xMax - rootBounds.xMax) - 8;

            if (tipBounds.yMax > rootBounds.yMax)
                tooltip.style.top = tipBounds.y - (tipBounds.yMax - rootBounds.yMax) - 8;
        }

        private static string FormatModValue(Modifier mod) => mod.Type switch
        {
            ModifierType.Flat => $"{mod.Value:F0}",
            ModifierType.Increased => $"{mod.Value * 100f:F0}%",
            ModifierType.More => $"{mod.Value * 100f:F0}% more",
            _ => $"{mod.Value:F1}"
        };

        private static string FormatStatName(StatType stat) => stat switch
        {
            StatType.MaxHealth => "Max Health",
            StatType.PhysicalDamage => "Physical Damage",
            StatType.AttackSpeed => "Attack Speed",
            StatType.CriticalChance => "Critical Chance",
            StatType.CriticalMultiplier => "Critical Multiplier",
            StatType.Armor => "Armor",
            StatType.Evasion => "Evasion",
            StatType.MovementSpeed => "Movement Speed",
            StatType.HealthRegen => "Health Regen",
            _ => stat.ToString()
        };

        private static string FormatSlotName(EquipmentSlotType slot) => slot switch
        {
            EquipmentSlotType.Weapon => "Weapon",
            EquipmentSlotType.Helmet => "Helmet",
            EquipmentSlotType.BodyArmor => "Body Armor",
            EquipmentSlotType.Gloves => "Gloves",
            EquipmentSlotType.Boots => "Boots",
            _ => slot.ToString()
        };

        private static string RarityClass(Rarity r) => r switch
        {
            Rarity.Magic => "rarity-magic",
            Rarity.Rare => "rarity-rare",
            Rarity.Unique => "rarity-unique",
            _ => "rarity-normal"
        };
    }
}
