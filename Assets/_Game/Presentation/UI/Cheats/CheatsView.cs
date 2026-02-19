using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Cheats
{
    public sealed class CheatsView : LayoutView
    {
        private Button _btnSendTest;
        private Label _feedbackLabel;

        public event System.Action OnSendTestClicked;

        protected override void OnBind()
        {
            _btnSendTest = Q<Button>("btn-send-test");
            _feedbackLabel = Q<Label>("feedback-label");

            _btnSendTest.clicked += () => OnSendTestClicked?.Invoke();
        }

        public void SetFeedback(string text)
        {
            _feedbackLabel.text = text;
        }
    }
}
