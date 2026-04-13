using System;
using System.Collections.Generic;
using Game.Domain.SaveSystem;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainMenu
{
    public sealed class SaveSlotView : LayoutView
    {
        private VisualElement _root;
        private readonly Button[] _slotButtons = new Button[3];
        private readonly Button[] _deleteButtons = new Button[3];
        private readonly Label[] _slotLabels = new Label[3];
        private readonly VisualElement[] _slotIconPlaceholders = new VisualElement[3];
        private Button _btnBack;

        private VisualElement _overwriteDialog;
        private Label _overwriteLabel;
        private Button _btnOverwriteConfirm;
        private Button _btnOverwriteCancel;
        private int _pendingOverwriteSlot = -1;

        public event Action<int> OnSlotClicked;
        public event Action<int> OnDeleteClicked;
        public event Action OnBackClicked;
        public event Action<int> OnOverwriteConfirmed;
        public event Action OnScreenShown;

        protected override void OnBind()
        {
            _root = Q("save-slots-screen");
            _btnBack = Q<Button>("btn-save-slots-back");

            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                _slotButtons[i] = Q<Button>($"btn-slot-{captured}");
                _deleteButtons[i] = Q<Button>($"btn-slot-delete-{captured}");
                _slotLabels[i] = Q<Label>($"label-slot-{captured}");
                _slotIconPlaceholders[i] = Q($"slot-icon-{captured}");

                _slotButtons[i].clicked += () => OnSlotClicked?.Invoke(captured);
                _deleteButtons[i].clicked += () => OnDeleteClicked?.Invoke(captured);
            }

            _btnBack.clicked += () => OnBackClicked?.Invoke();

            _overwriteDialog = Q("overwrite-dialog");
            _overwriteLabel = Q<Label>("overwrite-dialog-label");
            _btnOverwriteConfirm = Q<Button>("btn-overwrite-confirm");
            _btnOverwriteCancel = Q<Button>("btn-overwrite-cancel");

            _btnOverwriteConfirm.clicked += ConfirmOverwrite;
            _btnOverwriteCancel.clicked += HideOverwriteDialog;
            HideOverwriteDialog();
        }

        public void ShowScreen()
        {
            _root.style.display = DisplayStyle.Flex;
            OnScreenShown?.Invoke();
        }

        public void HideScreen() => _root.style.display = DisplayStyle.None;

        public void RenderSlots(IReadOnlyList<SaveSlotMetadata> slots, MainMenuFlowMode mode)
        {
            for (int i = 0; i < _slotButtons.Length; i++)
            {
                var slot = slots[i];
                _slotLabels[i].text = slot.IsEmpty
                    ? $"Slot {i + 1}\nClass: -\nLevel: -\nProgress: Empty"
                    : $"Slot {i + 1}\nClass: {slot.HeroClass}\nLevel: {slot.Level}\nProgress: Tier {slot.CurrentTier + 1}, Map {slot.CurrentMap + 1}";

                // Placeholder block to attach class/character icon later.
                _slotIconPlaceholders[i].style.unityBackgroundImageTintColor = slot.IsEmpty
                    ? new UnityEngine.Color(1f, 1f, 1f, 0.15f)
                    : new UnityEngine.Color(1f, 1f, 1f, 0.35f);

                bool canClick = mode == MainMenuFlowMode.NewGame || !slot.IsEmpty;
                _slotButtons[i].SetEnabled(canClick);
                _deleteButtons[i].style.display = slot.IsEmpty ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public void ShowOverwriteDialog(int slotIndex)
        {
            _pendingOverwriteSlot = slotIndex;
            _overwriteLabel.text = $"Overwrite Slot {slotIndex + 1}?";
            _overwriteDialog.style.display = DisplayStyle.Flex;
        }

        public void HideOverwriteDialog()
        {
            _pendingOverwriteSlot = -1;
            _overwriteDialog.style.display = DisplayStyle.None;
        }

        private void ConfirmOverwrite()
        {
            if (_pendingOverwriteSlot >= 0)
                OnOverwriteConfirmed?.Invoke(_pendingOverwriteSlot);
            HideOverwriteDialog();
        }
    }
}
