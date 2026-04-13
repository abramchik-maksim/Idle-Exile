using System;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainMenu
{
    public sealed class TitleScreenView : LayoutView
    {
        private VisualElement _root;
        private Button _btnNewGame;
        private Button _btnContinue;
        private Button _btnExit;

        public event Action OnNewGameClicked;
        public event Action OnContinueClicked;
        public event Action OnExitClicked;

        protected override void OnBind()
        {
            _root = Q("title-screen");
            _btnNewGame = Q<Button>("btn-new-game");
            _btnContinue = Q<Button>("btn-continue");
            _btnExit = Q<Button>("btn-exit");

            _btnNewGame.clicked += () => OnNewGameClicked?.Invoke();
            _btnContinue.clicked += () => OnContinueClicked?.Invoke();
            _btnExit.clicked += () => OnExitClicked?.Invoke();
        }

        public void SetContinueEnabled(bool enabled) => _btnContinue.SetEnabled(enabled);

        public void ShowScreen() => _root.style.display = DisplayStyle.Flex;

        public void HideScreen() => _root.style.display = DisplayStyle.None;
    }
}
