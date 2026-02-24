using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.DragDrop
{
    public sealed class ItemDragManipulator : PointerManipulator
    {
        private readonly Action<string, EquipmentSlotType> _onDroppedOnSlot;
        private VisualElement _ghost;
        private bool _isDragging;
        private Vector2 _startPosition;
        private ItemInstance _draggedItem;

        private const float DragThreshold = 5f;

        public ItemDragManipulator(Action<string, EquipmentSlotType> onDroppedOnSlot)
        {
            _onDroppedOnSlot = onDroppedOnSlot;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            _draggedItem = target.userData as ItemInstance;
            if (_draggedItem == null) return;

            _startPosition = evt.position;
            _isDragging = false;
            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_draggedItem == null || !target.HasPointerCapture(evt.pointerId))
                return;

            Vector2 currentPos = evt.position;

            if (!_isDragging)
            {
                if (Vector2.Distance(_startPosition, currentPos) < DragThreshold)
                    return;

                _isDragging = true;
                ItemTooltip.Hide();
                CreateGhost(currentPos);
            }

            if (_ghost != null)
            {
                _ghost.style.left = currentPos.x - 32;
                _ghost.style.top = currentPos.y - 32;
            }

            UpdateDropTargetHighlights(currentPos);

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_draggedItem == null) return;

            if (_isDragging)
            {
                var dropTarget = FindDropTarget(evt.position);
                if (dropTarget != null && dropTarget.userData is EquipmentSlotType slotType)
                {
                    if (_draggedItem.Definition.Slot == slotType)
                        _onDroppedOnSlot?.Invoke(_draggedItem.Uid, slotType);
                }

                ClearAllHighlights();
                RemoveGhost();
            }

            _isDragging = false;
            _draggedItem = null;
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            ClearAllHighlights();
            RemoveGhost();
            _isDragging = false;
            _draggedItem = null;
        }

        private void CreateGhost(Vector2 position)
        {
            var root = target.panel.visualTree;
            var rarityKey = EquipmentTabView.RarityKey(_draggedItem.Definition.Rarity);

            _ghost = new VisualElement();
            _ghost.AddToClassList("drag-ghost");
            _ghost.AddToClassList($"drag-ghost--{rarityKey}");
            _ghost.pickingMode = PickingMode.Ignore;

            var icon = new VisualElement();
            icon.AddToClassList("item-icon");
            icon.pickingMode = PickingMode.Ignore;

            var sourceIcon = target.Q("item-icon");
            if (sourceIcon != null)
            {
                var bgImg = sourceIcon.resolvedStyle.backgroundImage;
                if (bgImg.sprite != null)
                    icon.style.backgroundImage = new StyleBackground(bgImg.sprite);
                else if (bgImg.texture != null)
                    icon.style.backgroundImage = new StyleBackground(bgImg.texture);
                else
                    icon.AddToClassList($"item-icon--placeholder-{rarityKey}");
            }
            else
            {
                icon.AddToClassList($"item-icon--placeholder-{rarityKey}");
            }

            _ghost.Add(icon);

            _ghost.style.position = Position.Absolute;
            _ghost.style.left = position.x - 32;
            _ghost.style.top = position.y - 32;

            root.Add(_ghost);
        }

        private void RemoveGhost()
        {
            if (_ghost == null) return;
            _ghost.RemoveFromHierarchy();
            _ghost = null;
        }

        private VisualElement FindDropTarget(Vector2 position)
        {
            var picked = target.panel.Pick(position);
            while (picked != null)
            {
                if (picked.ClassListContains("equipment-slot"))
                    return picked;
                picked = picked.parent;
            }
            return null;
        }

        private void UpdateDropTargetHighlights(Vector2 position)
        {
            ClearAllHighlights();

            var dropTarget = FindDropTarget(position);
            if (dropTarget == null) return;

            if (dropTarget.userData is EquipmentSlotType slotType
                && _draggedItem.Definition.Slot == slotType)
            {
                dropTarget.AddToClassList("equipment-slot--drop-hover");
            }
        }

        private void ClearAllHighlights()
        {
            var root = target.panel?.visualTree;
            if (root == null) return;

            root.Query(className: "equipment-slot--drop-hover")
                .ForEach(el => el.RemoveFromClassList("equipment-slot--drop-hover"));
        }
    }
}
