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
        private EventCallback<ChangeEvent<bool>> _onHpBarsChanged;
        private EventCallback<ChangeEvent<bool>> _onEffectIndicatorsChanged;
        private EventCallback<ChangeEvent<bool>> _onDamageNumbersChanged;

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

            _onHpBarsChanged = HandleHpBarsChanged;
            _onEffectIndicatorsChanged = HandleEffectIndicatorsChanged;
            _onDamageNumbersChanged = HandleDamageNumbersChanged;

            _toggleHpBars.RegisterValueChangedCallback(_onHpBarsChanged);
            _toggleEffectIndicators.RegisterValueChangedCallback(_onEffectIndicatorsChanged);
            _toggleDamageNumbers.RegisterValueChangedCallback(_onDamageNumbersChanged);
            _btnClose.clicked += HandleCloseClicked;
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

        public override void Dispose()
        {
            if (_toggleHpBars != null && _onHpBarsChanged != null)
                _toggleHpBars.UnregisterValueChangedCallback(_onHpBarsChanged);
            if (_toggleEffectIndicators != null && _onEffectIndicatorsChanged != null)
                _toggleEffectIndicators.UnregisterValueChangedCallback(_onEffectIndicatorsChanged);
            if (_toggleDamageNumbers != null && _onDamageNumbersChanged != null)
                _toggleDamageNumbers.UnregisterValueChangedCallback(_onDamageNumbersChanged);
            if (_btnClose != null)
                _btnClose.clicked -= HandleCloseClicked;
        }

        private void HandleHpBarsChanged(ChangeEvent<bool> evt) => OnHpBarsChanged?.Invoke(evt.newValue);
        private void HandleEffectIndicatorsChanged(ChangeEvent<bool> evt) => OnEffectIndicatorsChanged?.Invoke(evt.newValue);
        private void HandleDamageNumbersChanged(ChangeEvent<bool> evt) => OnDamageNumbersChanged?.Invoke(evt.newValue);
        private void HandleCloseClicked() => OnCloseClicked?.Invoke();
    }
}
