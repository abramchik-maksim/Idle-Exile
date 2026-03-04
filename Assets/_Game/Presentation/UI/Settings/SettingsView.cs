using System;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Settings
{
    public sealed class SettingsView : LayoutView
    {
        private VisualElement _overlay;
        private Toggle _toggleHpBars;
        private Toggle _toggleEffectIndicators;
        private Toggle _toggleDamageNumbers;
        private Button _btnClose;

        public event Action<bool> OnHpBarsChanged;
        public event Action<bool> OnEffectIndicatorsChanged;
        public event Action<bool> OnDamageNumbersChanged;
        public event Action OnCloseClicked;

        protected override void OnBind()
        {
            _overlay = Q("settings-overlay");
            _toggleHpBars = Q<Toggle>("toggle-hp-bars");
            _toggleEffectIndicators = Q<Toggle>("toggle-effect-indicators");
            _toggleDamageNumbers = Q<Toggle>("toggle-damage-numbers");
            _btnClose = Q<Button>("btn-settings-close");

            _toggleHpBars.RegisterValueChangedCallback(evt => OnHpBarsChanged?.Invoke(evt.newValue));
            _toggleEffectIndicators.RegisterValueChangedCallback(evt => OnEffectIndicatorsChanged?.Invoke(evt.newValue));
            _toggleDamageNumbers.RegisterValueChangedCallback(evt => OnDamageNumbersChanged?.Invoke(evt.newValue));
            _btnClose.clicked += () => OnCloseClicked?.Invoke();
        }

        public void OpenSettings()
        {
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void CloseSettings()
        {
            _overlay.style.display = DisplayStyle.None;
        }

        public void SetValues(bool hpBars, bool effectIndicators, bool damageNumbers)
        {
            _toggleHpBars.SetValueWithoutNotify(hpBars);
            _toggleEffectIndicators.SetValueWithoutNotify(effectIndicators);
            _toggleDamageNumbers.SetValueWithoutNotify(damageNumbers);
        }
    }
}
