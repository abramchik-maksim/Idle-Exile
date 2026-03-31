using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Cheats
{
    public sealed class CheatsView : LayoutView
    {
        private Button _btnGenerateItem;
        private Button _btnAddSkillGem;
        private Button _btnAddRemovalOrb;
        private Button _btnAddTreeXp;
        private Button _btnResetSave;
        private Label _feedbackLabel;
        private VisualElement _panel;
        private VisualElement _header;

        private bool _isDragging;
        private bool _positionConverted;
        private Vector2 _dragOffset;

        public event Action OnGenerateItemClicked;
        public event Action OnAddSkillGemClicked;
        public event Action OnAddRemovalOrbClicked;
        public event Action OnAddTreeXpClicked;
        public event Action OnResetSaveClicked;

        protected override void OnBind()
        {
            _btnGenerateItem = Q<Button>("btn-generate-item");
            _btnAddSkillGem = Q<Button>("btn-add-skill-gem");
            _btnAddRemovalOrb = Q<Button>("btn-add-removal-orb");
            _btnAddTreeXp = Q<Button>("btn-add-tree-xp");
            _btnResetSave = Q<Button>("btn-reset-save");
            _feedbackLabel = Q<Label>("feedback-label");
            _panel = Q("cheats-panel");
            _header = Q("cheats-header");

            _btnGenerateItem.clicked += RaiseGenerateItemClicked;
            _btnAddSkillGem.clicked += RaiseAddSkillGemClicked;
            _btnAddRemovalOrb.clicked += RaiseAddRemovalOrbClicked;
            _btnAddTreeXp.clicked += RaiseAddTreeXpClicked;
            _btnResetSave.clicked += RaiseResetSaveClicked;

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

        public override void Dispose()
        {
            if (_btnGenerateItem != null) _btnGenerateItem.clicked -= RaiseGenerateItemClicked;
            if (_btnAddSkillGem != null) _btnAddSkillGem.clicked -= RaiseAddSkillGemClicked;
            if (_btnAddRemovalOrb != null) _btnAddRemovalOrb.clicked -= RaiseAddRemovalOrbClicked;
            if (_btnAddTreeXp != null) _btnAddTreeXp.clicked -= RaiseAddTreeXpClicked;
            if (_btnResetSave != null) _btnResetSave.clicked -= RaiseResetSaveClicked;
            if (_header != null)
            {
                _header.UnregisterCallback<PointerDownEvent>(OnHeaderPointerDown);
                _header.UnregisterCallback<PointerMoveEvent>(OnHeaderPointerMove);
                _header.UnregisterCallback<PointerUpEvent>(OnHeaderPointerUp);
            }
        }

        private void RaiseGenerateItemClicked() => OnGenerateItemClicked?.Invoke();
        private void RaiseAddSkillGemClicked() => OnAddSkillGemClicked?.Invoke();
        private void RaiseAddRemovalOrbClicked() => OnAddRemovalOrbClicked?.Invoke();
        private void RaiseAddTreeXpClicked() => OnAddTreeXpClicked?.Invoke();
        private void RaiseResetSaveClicked() => OnResetSaveClicked?.Invoke();
    }
}
