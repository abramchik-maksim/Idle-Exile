using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class MainScreenView : LayoutView
    {
        private Label _tierLabel;
        private Label _battleLabel;
        private VisualElement _healthFill;
        private Button _btnCharacterTab;
        private Button _btnEquipmentTab;
        private VisualElement _lootNotification;
        private Label _lootText;
        private IVisualElementScheduledItem _lootHideHandle;

        public event Action<int> OnTabSelected;

        protected override void OnBind()
        {
            _tierLabel = Q<Label>("tier-label");
            _battleLabel = Q<Label>("battle-label");
            _healthFill = Q("hero-health-fill");
            _btnCharacterTab = Q<Button>("btn-tab-character");
            _btnEquipmentTab = Q<Button>("btn-tab-equipment");
            _lootNotification = Q("loot-notification");
            _lootText = Q<Label>("loot-text");

            _btnCharacterTab.clicked += () => SelectTab(0);
            _btnEquipmentTab.clicked += () => SelectTab(1);
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
            OnTabSelected?.Invoke(index);
        }
    }
}
