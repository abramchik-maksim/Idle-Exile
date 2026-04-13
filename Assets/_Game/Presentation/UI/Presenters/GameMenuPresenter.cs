using System;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Presentation.UI.GameMenu;
using Game.Presentation.UI.Settings;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class GameMenuPresenter : IStartable, IDisposable
    {
        private readonly GameMenuView _view;
        private readonly SettingsView _settingsView;
        private readonly ISceneLoader _sceneLoader;

        public GameMenuPresenter(GameMenuView view, SettingsView settingsView, ISceneLoader sceneLoader)
        {
            _view = view;
            _settingsView = settingsView;
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            _view.OnSettingsClicked += HandleSettings;
            _view.OnMainMenuClicked += HandleMainMenu;
            _view.OnQuitClicked += HandleQuit;
        }

        private void HandleSettings()
        {
            _view.CloseMenu();
            _settingsView.OpenSettings();
        }

        private void HandleMainMenu()
        {
            _view.CloseMenu();
            _ = _sceneLoader.LoadMainMenuAsync();
        }

        private void HandleQuit()
        {
            Debug.Log("[GameMenuPresenter] Quit requested.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        public void Dispose()
        {
            _view.OnSettingsClicked -= HandleSettings;
            _view.OnMainMenuClicked -= HandleMainMenu;
            _view.OnQuitClicked -= HandleQuit;
        }
    }
}
