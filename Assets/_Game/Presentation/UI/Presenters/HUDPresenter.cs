using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Domain.DTOs.Combat;
using Game.Presentation.UI.HUD;
using Game.Presentation.UI.Inventory;
using Game.Presentation.UI.CharacterPanel;
using Game.Presentation.UI.Cheats;

namespace Game.Presentation.UI.Presenters
{
    public sealed class HUDPresenter : IStartable, IDisposable
    {
        private readonly HUDView _hudView;
        private readonly InventoryView _inventoryView;
        private readonly CharacterPanelView _characterView;
        private readonly CheatsView _cheatsView;
        private readonly ISubscriber<CombatStartedDTO> _combatStartedSub;

        private readonly List<IDisposable> _subscriptions = new();

        public HUDPresenter(
            HUDView hudView,
            InventoryView inventoryView,
            CharacterPanelView characterView,
            CheatsView cheatsView,
            ISubscriber<CombatStartedDTO> combatStartedSub)
        {
            _hudView = hudView;
            _inventoryView = inventoryView;
            _characterView = characterView;
            _cheatsView = cheatsView;
            _combatStartedSub = combatStartedSub;
        }

        public void Start()
        {
            _hudView.OnInventoryClicked += HandleInventoryToggle;
            _hudView.OnCharacterClicked += HandleCharacterToggle;
            _hudView.OnCheatsClicked += HandleCheatsToggle;

            _inventoryView.OnCloseClicked += () => _inventoryView.Hide();
            _characterView.OnCloseClicked += () => _characterView.Hide();

            _subscriptions.Add(
                _combatStartedSub.Subscribe(e => _hudView.SetWave(e.WaveIndex)));

            UnityEngine.Debug.Log("[HUDPresenter] Initialized and listening.");
        }

        private void HandleInventoryToggle()
        {
            _inventoryView.Toggle();
            if (_characterView.IsVisible)
                _characterView.Hide();
        }

        private void HandleCharacterToggle()
        {
            _characterView.Toggle();
            if (_inventoryView.IsVisible)
                _inventoryView.Hide();
        }

        private void HandleCheatsToggle()
        {
            _cheatsView.Toggle();
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
