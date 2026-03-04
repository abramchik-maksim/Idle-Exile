using System;
using UnityEngine;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Presentation.UI.Settings;

namespace Game.Presentation.UI.Presenters
{
    public sealed class SettingsPresenter : IStartable, IDisposable
    {
        private readonly SettingsView _view;
        private readonly ICombatDisplaySettings _displaySettings;

        private const string PrefHpBars = "Settings_ShowHpBars";
        private const string PrefEffects = "Settings_ShowEffectIndicators";
        private const string PrefDamageNumbers = "Settings_ShowDamageNumbers";

        public SettingsPresenter(SettingsView view, ICombatDisplaySettings displaySettings)
        {
            _view = view;
            _displaySettings = displaySettings;
        }

        public void Start()
        {
            bool hp = PlayerPrefs.GetInt(PrefHpBars, 1) == 1;
            bool fx = PlayerPrefs.GetInt(PrefEffects, 1) == 1;
            bool dmg = PlayerPrefs.GetInt(PrefDamageNumbers, 1) == 1;

            ApplySettings(hp, fx, dmg);
            _view.SetValues(hp, fx, dmg);

            _view.OnHpBarsChanged += val =>
            {
                _displaySettings.ShowHpBars = val;
                PlayerPrefs.SetInt(PrefHpBars, val ? 1 : 0);
            };

            _view.OnEffectIndicatorsChanged += val =>
            {
                _displaySettings.ShowEffectIndicators = val;
                PlayerPrefs.SetInt(PrefEffects, val ? 1 : 0);
            };

            _view.OnDamageNumbersChanged += val =>
            {
                _displaySettings.ShowDamageNumbers = val;
                PlayerPrefs.SetInt(PrefDamageNumbers, val ? 1 : 0);
            };

            _view.OnCloseClicked += () => _view.CloseSettings();
        }

        public void OpenSettings()
        {
            _view.OpenSettings();
        }

        private void ApplySettings(bool hpBars, bool effects, bool damageNumbers)
        {
            _displaySettings.ShowHpBars = hpBars;
            _displaySettings.ShowEffectIndicators = effects;
            _displaySettings.ShowDamageNumbers = damageNumbers;
        }

        public void Dispose()
        {
            PlayerPrefs.Save();
        }
    }
}
