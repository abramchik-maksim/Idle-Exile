using System;
using VContainer.Unity;
using Game.Application.Ports;

namespace Game.Presentation.UI.MainMenu
{
    public sealed class SaveSlotPresenter : IStartable, IDisposable
    {
        private readonly TitleScreenView _titleView;
        private readonly SaveSlotView _saveSlotView;
        private readonly CharacterSelectView _characterView;
        private readonly ISaveSlotManager _saveSlotManager;
        private readonly GameSessionContext _session;
        private readonly ISceneLoader _sceneLoader;
        private readonly MainMenuState _state;

        public SaveSlotPresenter(
            TitleScreenView titleView,
            SaveSlotView saveSlotView,
            CharacterSelectView characterView,
            ISaveSlotManager saveSlotManager,
            GameSessionContext session,
            ISceneLoader sceneLoader,
            MainMenuState state)
        {
            _titleView = titleView;
            _saveSlotView = saveSlotView;
            _characterView = characterView;
            _saveSlotManager = saveSlotManager;
            _session = session;
            _sceneLoader = sceneLoader;
            _state = state;
        }

        public void Start()
        {
            _saveSlotView.OnScreenShown += RefreshSlots;
            _saveSlotView.OnSlotClicked += HandleSlotClicked;
            _saveSlotView.OnDeleteClicked += HandleDeleteClicked;
            _saveSlotView.OnBackClicked += HandleBackClicked;
            _saveSlotView.OnOverwriteConfirmed += HandleOverwriteConfirmed;

            if (_saveSlotView.IsVisible)
                RefreshSlots();
        }

        private void RefreshSlots()
        {
            _saveSlotView.RenderSlots(_saveSlotManager.GetAllSlots(), _state.Mode);
        }

        private void HandleSlotClicked(int slotIndex)
        {
            var slot = _saveSlotManager.GetSlot(slotIndex);
            _state.SelectedSlotIndex = slotIndex;

            if (_state.Mode == MainMenuFlowMode.Continue)
            {
                if (slot.IsEmpty) return;
                _session.IsNewGame = false;
                _session.SaveSlotIndex = slotIndex;
                _session.SelectedClass = slot.HeroClass;
                _saveSlotManager.SetActiveSlot(slotIndex);
                _ = _sceneLoader.LoadGameplayAsync();
                return;
            }

            if (!slot.IsEmpty)
            {
                _saveSlotView.ShowOverwriteDialog(slotIndex);
                return;
            }

            OpenCharacterSelect(slotIndex);
        }

        private void HandleOverwriteConfirmed(int slotIndex)
        {
            _saveSlotManager.DeleteSlot(slotIndex);
            OpenCharacterSelect(slotIndex);
        }

        private void OpenCharacterSelect(int slotIndex)
        {
            _state.SelectedSlotIndex = slotIndex;
            _saveSlotView.HideScreen();
            _characterView.ShowScreen();
        }

        private void HandleDeleteClicked(int slotIndex)
        {
            _saveSlotManager.DeleteSlot(slotIndex);
            RefreshSlots();
        }

        private void HandleBackClicked()
        {
            _saveSlotView.HideScreen();
            _characterView.HideScreen();
            _titleView.ShowScreen();
        }

        public void Dispose()
        {
            _saveSlotView.OnScreenShown -= RefreshSlots;
            _saveSlotView.OnSlotClicked -= HandleSlotClicked;
            _saveSlotView.OnDeleteClicked -= HandleDeleteClicked;
            _saveSlotView.OnBackClicked -= HandleBackClicked;
            _saveSlotView.OnOverwriteConfirmed -= HandleOverwriteConfirmed;
        }
    }
}
