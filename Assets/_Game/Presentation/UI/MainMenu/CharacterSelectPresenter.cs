using System;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Domain.Items;

namespace Game.Presentation.UI.MainMenu
{
    public sealed class CharacterSelectPresenter : IStartable, IDisposable
    {
        private readonly CharacterSelectView _view;
        private readonly SaveSlotView _saveSlotView;
        private readonly ICharacterConfigProvider _characterConfig;
        private readonly ISaveSlotManager _saveSlotManager;
        private readonly GameSessionContext _session;
        private readonly ISceneLoader _sceneLoader;
        private readonly MainMenuState _state;

        private HeroItemClass _selectedClass = HeroItemClass.Warrior;
        private bool _hasSelection;

        public CharacterSelectPresenter(
            CharacterSelectView view,
            SaveSlotView saveSlotView,
            ICharacterConfigProvider characterConfig,
            ISaveSlotManager saveSlotManager,
            GameSessionContext session,
            ISceneLoader sceneLoader,
            MainMenuState state)
        {
            _view = view;
            _saveSlotView = saveSlotView;
            _characterConfig = characterConfig;
            _saveSlotManager = saveSlotManager;
            _session = session;
            _sceneLoader = sceneLoader;
            _state = state;
        }

        public void Start()
        {
            _view.OnClassSelected += HandleClassSelected;
            _view.OnStartClicked += HandleStartClicked;
            _view.OnBackClicked += HandleBackClicked;
        }

        private void HandleClassSelected(HeroItemClass heroClass)
        {
            _selectedClass = heroClass;
            _hasSelection = true;
            var definition = _characterConfig.GetByClass(heroClass);
            _view.SetSelection(heroClass, definition.Description);
            _view.SetStartEnabled(true);
        }

        private void HandleStartClicked()
        {
            if (!_hasSelection || _state.SelectedSlotIndex < 0)
                return;

            int slot = _state.SelectedSlotIndex;
            string heroId = $"{_selectedClass.ToString().ToLowerInvariant()}_hero";
            _saveSlotManager.DeleteSlot(slot);
            _saveSlotManager.CreateSlot(slot, heroId, _selectedClass);
            _saveSlotManager.SetActiveSlot(slot);

            _session.IsNewGame = true;
            _session.SaveSlotIndex = slot;
            _session.SelectedClass = _selectedClass;

            _ = _sceneLoader.LoadGameplayAsync();
        }

        private void HandleBackClicked()
        {
            _view.HideScreen();
            _saveSlotView.ShowScreen();
        }

        public void Dispose()
        {
            _view.OnClassSelected -= HandleClassSelected;
            _view.OnStartClicked -= HandleStartClicked;
            _view.OnBackClicked -= HandleBackClicked;
        }
    }
}
