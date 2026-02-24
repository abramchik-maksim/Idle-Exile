using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class MainScreenView : LayoutView
    {
        private Label _waveLabel;
        private VisualElement _healthFill;
        private Button _btnCharacterTab;
        private Button _btnEquipmentTab;

        public event Action<int> OnTabSelected;

        protected override void OnBind()
        {
            _waveLabel = Q<Label>("wave-label");
            _healthFill = Q("hero-health-fill");
            _btnCharacterTab = Q<Button>("btn-tab-character");
            _btnEquipmentTab = Q<Button>("btn-tab-equipment");

            _btnCharacterTab.clicked += () => SelectTab(0);
            _btnEquipmentTab.clicked += () => SelectTab(1);
        }

        public void SetWave(int wave) =>
            _waveLabel.text = $"Wave {wave}";

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
