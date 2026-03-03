using System;
using VContainer.Unity;
using Game.Presentation.UI.GameMenu;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class GameMenuPresenter : IStartable, IDisposable
    {
        private readonly GameMenuView _view;

        public GameMenuPresenter(GameMenuView view)
        {
            _view = view;
        }

        public void Start()
        {
            _view.OnSettingsClicked += HandleSettings;
            _view.OnQuitClicked += HandleQuit;
        }

        private void HandleSettings()
        {
            _view.CloseMenu();
            Debug.Log("[GameMenuPresenter] Settings requested (not yet implemented).");
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
            _view.OnQuitClicked -= HandleQuit;
        }
    }
}
