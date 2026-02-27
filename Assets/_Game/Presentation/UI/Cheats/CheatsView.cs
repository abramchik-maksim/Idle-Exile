using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Cheats
{
    public sealed class CheatsView : LayoutView
    {
        private Button _btnGenerateItem;
        private Button _btnSendTest;
        private Button _btnResetSave;
        private Label _feedbackLabel;
        private VisualElement _panel;
        private VisualElement _header;

        private bool _isDragging;
        private bool _positionConverted;
        private Vector2 _dragOffset;

        public event Action OnGenerateItemClicked;
        public event Action OnSendTestClicked;
        public event Action OnResetSaveClicked;

        protected override void OnBind()
        {
            _btnGenerateItem = Q<Button>("btn-generate-item");
            _btnSendTest = Q<Button>("btn-send-test");
            _btnResetSave = Q<Button>("btn-reset-save");
            _feedbackLabel = Q<Label>("feedback-label");
            _panel = Q("cheats-panel");
            _header = Q("cheats-header");

            _btnGenerateItem.clicked += () => OnGenerateItemClicked?.Invoke();
            _btnSendTest.clicked += () => OnSendTestClicked?.Invoke();
            _btnResetSave.clicked += () => OnResetSaveClicked?.Invoke();

            _header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
            _header.RegisterCallback<PointerMoveEvent>(OnHeaderPointerMove);
            _header.RegisterCallback<PointerUpEvent>(OnHeaderPointerUp);
        }

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            _isDragging = true;
            _header.CapturePointer(evt.pointerId);

            SwitchToLeftTopPositioning();

            var panelPos = new Vector2(_panel.resolvedStyle.left, _panel.resolvedStyle.top);
            _dragOffset = (Vector2)evt.position - panelPos;

            evt.StopPropagation();
        }

        private void OnHeaderPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || !_header.HasPointerCapture(evt.pointerId)) return;

            float newLeft = evt.position.x - _dragOffset.x;
            float newTop = evt.position.y - _dragOffset.y;

            _panel.style.left = newLeft;
            _panel.style.top = newTop;

            evt.StopPropagation();
        }

        private void OnHeaderPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;

            _isDragging = false;
            _header.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void SwitchToLeftTopPositioning()
        {
            if (_positionConverted) return;
            _positionConverted = true;

            float left = _panel.worldBound.x;
            float top = _panel.worldBound.y;

            _panel.style.right = StyleKeyword.Auto;
            _panel.style.bottom = StyleKeyword.Auto;
            _panel.style.left = left;
            _panel.style.top = top;
        }

        public void SetFeedback(string text)
        {
            _feedbackLabel.text = text;
        }
    }
}
