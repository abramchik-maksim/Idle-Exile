using System;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.GameMenu
{
    public sealed class GameMenuView : LayoutView
    {
        private Button _btnMenu;
        private Button _btnSettings;
        private Button _btnMainMenu;
        private Button _btnQuit;
        private Button _btnClose;
        private VisualElement _overlay;

        public event Action OnSettingsClicked;
        public event Action OnQuitClicked;
        public event Action OnMainMenuClicked;

        protected override void OnBind()
        {
            _btnMenu = Q<Button>("btn-menu");
            _btnSettings = Q<Button>("btn-settings");
            _btnMainMenu = Q<Button>("btn-main-menu");
            _btnQuit = Q<Button>("btn-quit");
            _btnClose = Q<Button>("btn-close");
            _overlay = Q("menu-overlay");

            _btnMenu.clicked += ToggleMenu;
            _btnSettings.clicked += () => OnSettingsClicked?.Invoke();
            _btnMainMenu.clicked += () => OnMainMenuClicked?.Invoke();
            _btnQuit.clicked += () => OnQuitClicked?.Invoke();
            _btnClose.clicked += CloseMenu;
        }

        public void OpenMenu()
        {
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void CloseMenu()
        {
            _overlay.style.display = DisplayStyle.None;
        }

        private void ToggleMenu()
        {
            bool isOpen = _overlay.resolvedStyle.display == DisplayStyle.Flex;
            if (isOpen)
                CloseMenu();
            else
                OpenMenu();
        }
    }
}
