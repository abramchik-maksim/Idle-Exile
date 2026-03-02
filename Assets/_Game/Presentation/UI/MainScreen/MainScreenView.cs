using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Domain.Skills;
using Game.Application.Skills;
using Game.Presentation.UI.Base;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class MainScreenView : LayoutView
    {
        private Label _tierLabel;
        private Label _battleLabel;
        private VisualElement _healthFill;
        private VisualElement _buffBar;
        private Button _btnCharacterTab;
        private Button _btnEquipmentTab;
        private Button _btnSkillsTab;
        private VisualElement _lootNotification;
        private Label _lootText;
        private IVisualElementScheduledItem _lootHideHandle;

        public event Action<int> OnTabSelected;

        protected override void OnBind()
        {
            _tierLabel = Q<Label>("tier-label");
            _battleLabel = Q<Label>("battle-label");
            _healthFill = Q("hero-health-fill");
            _buffBar = Q("buff-bar");
            _btnCharacterTab = Q<Button>("btn-tab-character");
            _btnEquipmentTab = Q<Button>("btn-tab-equipment");
            _btnSkillsTab = Q<Button>("btn-tab-skills");
            _lootNotification = Q("loot-notification");
            _lootText = Q<Label>("loot-text");

            _btnCharacterTab.clicked += () => SelectTab(0);
            _btnEquipmentTab.clicked += () => SelectTab(1);
            _btnSkillsTab.clicked += () => SelectTab(2);
        }

        public void SetBattleInfo(string tierName, int battleIndex, int totalBattles)
        {
            _tierLabel.text = tierName;
            _battleLabel.text = $"Battle {battleIndex + 1} / {totalBattles}";
        }

        public void SetHealthPercent(float normalized)
        {
            float pct = Mathf.Clamp01(normalized) * 100f;
            _healthFill.style.width = new Length(pct, LengthUnit.Percent);
        }

        public void ShowLootNotification(string itemName, Rarity rarity)
        {
            _lootHideHandle?.Pause();

            _lootText.text = itemName;
            _lootText.RemoveFromClassList("rarity-normal");
            _lootText.RemoveFromClassList("rarity-magic");
            _lootText.RemoveFromClassList("rarity-rare");
            _lootText.RemoveFromClassList("rarity-unique");
            _lootText.AddToClassList($"rarity-{rarity.ToString().ToLower()}");

            _lootNotification.style.display = DisplayStyle.Flex;
            _lootNotification.style.opacity = 1f;

            _lootHideHandle = _lootNotification.schedule.Execute(() =>
            {
                _lootNotification.style.opacity = 0f;
                _lootNotification.schedule.Execute(() =>
                    _lootNotification.style.display = DisplayStyle.None
                ).StartingIn(300);
            }).StartingIn(2500);
        }

        public void SelectTab(int index)
        {
            _btnCharacterTab.EnableInClassList("tab-btn--active", index == 0);
            _btnEquipmentTab.EnableInClassList("tab-btn--active", index == 1);
            _btnSkillsTab.EnableInClassList("tab-btn--active", index == 2);
            OnTabSelected?.Invoke(index);
        }

        public void RenderBuffs(List<UtilitySkillRunner.ActiveBuff> buffs)
        {
            if (_buffBar == null) return;
            _buffBar.Clear();

            foreach (var buff in buffs)
            {
                var icon = new VisualElement();
                icon.AddToClassList("buff-icon");

                var color = GetBuffColor(buff.EffectType);
                icon.style.borderBottomColor = color;
                icon.style.borderTopColor = color;
                icon.style.borderLeftColor = color;
                icon.style.borderRightColor = color;

                var label = new Label(GetBuffAbbrev(buff.EffectType));
                label.AddToClassList("buff-icon__label");
                icon.Add(label);

                var timerLabel = new Label($"{buff.RemainingTime:F0}s");
                timerLabel.AddToClassList("buff-icon__timer");
                icon.Add(timerLabel);

                var capturedBuff = buff;
                icon.RegisterCallback<PointerEnterEvent>(_ =>
                    SkillTooltip.ShowBuffTooltip(icon, capturedBuff, Root));
                icon.RegisterCallback<PointerLeaveEvent>(_ =>
                    SkillTooltip.Hide());

                _buffBar.Add(icon);
            }

            SkillTooltip.HideIfOwnerDetached();
        }

        private static Color GetBuffColor(SkillEffectType effect) => effect switch
        {
            SkillEffectType.HealOverTime => new Color(0.3f, 0.8f, 0.3f),
            SkillEffectType.BuffArmor => new Color(0.7f, 0.6f, 0.3f),
            SkillEffectType.BuffEvasion => new Color(0.4f, 0.7f, 0.9f),
            SkillEffectType.BuffAttackSpeed => new Color(0.9f, 0.5f, 0.2f),
            _ => new Color(0.6f, 0.6f, 0.6f)
        };

        private static string GetBuffAbbrev(SkillEffectType effect) => effect switch
        {
            SkillEffectType.HealOverTime => "H",
            SkillEffectType.BuffArmor => "A",
            SkillEffectType.BuffEvasion => "E",
            SkillEffectType.BuffAttackSpeed => "S",
            _ => "?"
        };
    }
}
