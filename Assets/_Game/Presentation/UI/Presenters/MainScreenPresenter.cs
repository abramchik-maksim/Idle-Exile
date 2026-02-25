using System;
using System.Collections.Generic;
using VContainer.Unity;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class MainScreenPresenter : IStartable, IDisposable
    {
        private readonly MainScreenView _mainScreenView;
        private readonly CharacterTabView _characterTabView;
        private readonly EquipmentTabView _equipmentTabView;

        private readonly List<IDisposable> _subscriptions = new();
        private int _activeTab;

        public MainScreenPresenter(
            MainScreenView mainScreenView,
            CharacterTabView characterTabView,
            EquipmentTabView equipmentTabView)
        {
            _mainScreenView = mainScreenView;
            _characterTabView = characterTabView;
            _equipmentTabView = equipmentTabView;
        }

        public void Start()
        {
            _mainScreenView.OnTabSelected += HandleTabSelected;

            ShowTab(0);

            UnityEngine.Debug.Log("[MainScreenPresenter] Initialized.");
        }

        private void HandleTabSelected(int tabIndex)
        {
            ShowTab(tabIndex);
        }

        private void ShowTab(int tabIndex)
        {
            _activeTab = tabIndex;

            if (tabIndex == 0)
            {
                _characterTabView.Show();
                _equipmentTabView.Hide();
            }
            else
            {
                _characterTabView.Hide();
                _equipmentTabView.Show();
            }
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
