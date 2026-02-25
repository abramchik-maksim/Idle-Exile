using System;
using UnityEngine;
using UnityEngine.UIElements;
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

        public event Action<int> OnTabSelected;

        protected override void OnBind()
        {
            _tierLabel = Q<Label>("tier-label");
            _battleLabel = Q<Label>("battle-label");
            _healthFill = Q("hero-health-fill");
            _btnCharacterTab = Q<Button>("btn-tab-character");
            _btnEquipmentTab = Q<Button>("btn-tab-equipment");

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

        public void SelectTab(int index)
        {
            _btnCharacterTab.EnableInClassList("tab-btn--active", index == 0);
            _btnEquipmentTab.EnableInClassList("tab-btn--active", index == 1);
            OnTabSelected?.Invoke(index);
        }
    }
}
