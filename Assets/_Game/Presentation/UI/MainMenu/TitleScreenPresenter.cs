using System;
using VContainer.Unity;
using Game.Application.Ports;
using UnityEngine;

namespace Game.Presentation.UI.MainMenu
{
    public sealed class TitleScreenPresenter : IStartable, IDisposable
    {
        private readonly TitleScreenView _view;
        private readonly SaveSlotView _saveSlotView;
        private readonly CharacterSelectView _characterView;
        private readonly ISaveSlotManager _saveSlotManager;
        private readonly MainMenuState _state;

        public TitleScreenPresenter(
            TitleScreenView view,
            SaveSlotView saveSlotView,
            CharacterSelectView characterView,
            ISaveSlotManager saveSlotManager,
            MainMenuState state)
        {
            _view = view;
            _saveSlotView = saveSlotView;
            _characterView = characterView;
            _saveSlotManager = saveSlotManager;
            _state = state;
        }

        public void Start()
        {
            _view.OnNewGameClicked += HandleNewGame;
            _view.OnContinueClicked += HandleContinue;
            _view.OnExitClicked += HandleExit;

            _view.ShowScreen();
            _saveSlotView.HideScreen();
            _characterView.HideScreen();
            RefreshContinueState();
        }

        private void HandleNewGame()
        {
            _state.Mode = MainMenuFlowMode.NewGame;
            _view.HideScreen();
            _saveSlotView.ShowScreen();
            _characterView.HideScreen();
            _saveSlotView.RenderSlots(_saveSlotManager.GetAllSlots(), _state.Mode);
        }

        private void HandleContinue()
        {
            _state.Mode = MainMenuFlowMode.Continue;
            _view.HideScreen();
            _saveSlotView.ShowScreen();
            _characterView.HideScreen();
            _saveSlotView.RenderSlots(_saveSlotManager.GetAllSlots(), _state.Mode);
        }

        private static void HandleExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void RefreshContinueState()
        {
            var slots = _saveSlotManager.GetAllSlots();
            bool hasAny = false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty)
                {
                    hasAny = true;
                    break;
                }
            }
            _view.SetContinueEnabled(hasAny);
        }

        public void Dispose()
        {
            _view.OnNewGameClicked -= HandleNewGame;
            _view.OnContinueClicked -= HandleContinue;
            _view.OnExitClicked -= HandleExit;
        }
    }
}
