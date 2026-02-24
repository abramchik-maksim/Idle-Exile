using System;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Cheats
{
    public sealed class CheatsView : LayoutView
    {
        private Button _btnGenerateItem;
        private Button _btnSendTest;
        private Label _feedbackLabel;

        public event Action OnGenerateItemClicked;
        public event Action OnSendTestClicked;

        protected override void OnBind()
        {
            _btnGenerateItem = Q<Button>("btn-generate-item");
            _btnSendTest = Q<Button>("btn-send-test");
            _feedbackLabel = Q<Label>("feedback-label");

            _btnGenerateItem.clicked += () => OnGenerateItemClicked?.Invoke();
            _btnSendTest.clicked += () => OnSendTestClicked?.Invoke();
        }

        public void SetFeedback(string text)
        {
            _feedbackLabel.text = text;
        }
    }
}
