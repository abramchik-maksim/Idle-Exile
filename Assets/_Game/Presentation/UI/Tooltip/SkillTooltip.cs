using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Skills;
using Game.Application.Skills;

namespace Game.Presentation.UI.Tooltip
{
    public static class SkillTooltip
    {
        private static VisualElement _tooltip;
        private static VisualElement _currentOwner;

        public static void Show(VisualElement owner, SkillInstance skill, VisualElement root)
        {
            Hide();
            if (skill == null) return;

            _currentOwner = owner;
            _tooltip = BuildPanel(skill);

            var ownerRect = owner.worldBound;
            _tooltip.style.left = ownerRect.xMax + 8;
            _tooltip.style.top = ownerRect.yMax;

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

        public static void HideIfOwnerDetached()
        {
            if (_currentOwner != null && _currentOwner.panel == null)
                Hide();
        }

        private static VisualElement BuildPanel(SkillInstance skill)
        {
            var def = skill.Definition;

            var panel = new VisualElement();
            panel.AddToClassList("item-tooltip");
            panel.pickingMode = PickingMode.Ignore;

            var title = new Label(def.Name);
            title.AddToClassList("item-tooltip__title");
            title.AddToClassList(CategoryColor(def.Category));
            title.pickingMode = PickingMode.Ignore;
            panel.Add(title);

            var categoryLabel = new Label(def.Category == SkillCategory.Main ? "Main Skill" : "Utility Skill");
            categoryLabel.AddToClassList("item-tooltip__rarity");
            categoryLabel.AddToClassList(CategoryColor(def.Category));
            categoryLabel.pickingMode = PickingMode.Ignore;
            panel.Add(categoryLabel);

            if (def.Category == SkillCategory.Utility && def.SubCategory != UtilitySubCategory.None)
            {
                var subLabel = new Label(FormatSubCategory(def.SubCategory));
                subLabel.AddToClassList("item-tooltip__slot");
                subLabel.pickingMode = PickingMode.Ignore;
                panel.Add(subLabel);
            }

            var levelLabel = new Label($"Level {skill.Level}");
            levelLabel.AddToClassList("item-tooltip__slot");
            levelLabel.pickingMode = PickingMode.Ignore;
            panel.Add(levelLabel);

            AddSeparator(panel);

            if (def.Category == SkillCategory.Main)
                BuildMainStats(panel, def);
            else
                BuildUtilityStats(panel, def);

            return panel;
        }

        private static void BuildMainStats(VisualElement panel, SkillDefinition def)
        {
            if (def.RequiredWeapon != WeaponType.None)
                AddStatLine(panel, $"Requires: {def.RequiredWeapon}", false);

            AddStatLine(panel, $"Damage: {def.DamageMultiplierPercent:F0}%",
                def.DamageMultiplierPercent != 100f);

            AddStatLine(panel, $"Attack Speed: {def.AttackSpeedMultiplierPercent:F0}%",
                def.AttackSpeedMultiplierPercent != 100f);

            if (def.Effects.Count > 0)
            {
                AddSeparator(panel);
                foreach (var effect in def.Effects)
                    AddStatLine(panel, FormatEffect(effect), true);
            }
        }

        private static void BuildUtilityStats(VisualElement panel, SkillDefinition def)
        {
            if (def.Cooldown > 0)
                AddStatLine(panel, $"Cooldown: {def.Cooldown:F0}s", false);

            if (def.EffectDuration > 0)
                AddStatLine(panel, $"Duration: {def.EffectDuration:F0}s", false);

            if (def.EffectType != SkillEffectType.None)
                AddStatLine(panel, FormatUtilityEffect(def.EffectType, def.EffectValue), true);
        }

        private static void AddStatLine(VisualElement panel, string text, bool highlight)
        {
            var label = new Label(text);
            label.AddToClassList("item-tooltip__mod");
            if (!highlight)
                label.AddToClassList("item-tooltip__mod--implicit");
            label.pickingMode = PickingMode.Ignore;
            panel.Add(label);
        }

        private static void AddSeparator(VisualElement panel)
        {
            var sep = new VisualElement();
            sep.AddToClassList("item-tooltip__separator");
            sep.pickingMode = PickingMode.Ignore;
            panel.Add(sep);
        }

        private static string FormatSubCategory(UtilitySubCategory sub) => sub switch
        {
            UtilitySubCategory.Recovery => "Recovery",
            UtilitySubCategory.Defense => "Defense",
            UtilitySubCategory.Enhancement => "Enhancement",
            _ => ""
        };

        private static string FormatEffect(SkillEffectType effect) => effect switch
        {
            SkillEffectType.AoE => "Area of Effect",
            SkillEffectType.Split => "Projectile Split",
            SkillEffectType.Chain => "Chain to Nearby Enemies",
            SkillEffectType.Penetration => "Penetrates Enemies",
            _ => effect.ToString()
        };

        private static string FormatUtilityEffect(SkillEffectType effect, float value) => effect switch
        {
            SkillEffectType.HealOverTime => $"Heal {value:F0} HP/s",
            SkillEffectType.BuffArmor => $"+{value:F0} Armor",
            SkillEffectType.BuffEvasion => $"+{value:F0} Evasion",
            SkillEffectType.BuffAttackSpeed => $"+{value:F0}% Attack Speed",
            _ => $"{effect}: {value:F0}"
        };

        public static void ShowBuffTooltip(VisualElement owner, UtilitySkillRunner.ActiveBuff buff, VisualElement root)
        {
            Hide();

            _currentOwner = owner;

            var panel = new VisualElement();
            panel.AddToClassList("item-tooltip");
            panel.pickingMode = PickingMode.Ignore;
            panel.style.position = Position.Absolute;

            var title = new Label(buff.SkillName);
            title.AddToClassList("item-tooltip__title");
            title.AddToClassList("rarity-magic");
            title.pickingMode = PickingMode.Ignore;
            panel.Add(title);

            AddSeparator(panel);

            AddStatLine(panel, FormatUtilityEffect(buff.EffectType, buff.EffectValue), true);
            AddStatLine(panel, $"Remaining: {buff.RemainingTime:F1}s", false);

            var ownerRect = owner.worldBound;
            panel.style.left = ownerRect.xMax + 4;
            panel.style.top = ownerRect.yMin;

            _tooltip = panel;
            root.Add(panel);

            panel.RegisterCallback<GeometryChangedEvent>(_ => ClampToScreen(panel, root));
        }

        private static string CategoryColor(SkillCategory cat) =>
            cat == SkillCategory.Main ? "rarity-unique" : "rarity-magic";

        private static void ClampToScreen(VisualElement tooltip, VisualElement root)
        {
            var rootBounds = root.worldBound;
            var tipBounds = tooltip.worldBound;

            if (tipBounds.xMax > rootBounds.xMax)
                tooltip.style.left = tipBounds.x - (tipBounds.xMax - rootBounds.xMax) - 8;

            if (tipBounds.yMax > rootBounds.yMax)
                tooltip.style.top = tipBounds.y - (tipBounds.yMax - rootBounds.yMax) - 8;
        }
    }
}
