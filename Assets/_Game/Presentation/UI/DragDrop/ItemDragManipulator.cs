using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.DragDrop
{
    public sealed class ItemDragManipulator : PointerManipulator
    {
        private readonly Action<string, EquipmentSlotType> _onDroppedOnSlot;
        private readonly Action<ItemInstance> _onClicked;
        private readonly Action _onDragReleased;
        private readonly Action<string> _onDroppedOnSellZone;
        private readonly ItemInstance _explicitItem;
        private VisualElement _ghost;
        private bool _isDragging;
        private Vector2 _startPosition;
        private ItemInstance _draggedItem;

        private const float DragThreshold = 5f;

        public ItemDragManipulator(
            Action<string, EquipmentSlotType> onDroppedOnSlot = null,
            Action<ItemInstance> onClicked = null,
            Action onDragReleased = null,
            ItemInstance explicitItem = null,
            Action<string> onDroppedOnSellZone = null)
        {
            _onDroppedOnSlot = onDroppedOnSlot;
            _onClicked = onClicked;
            _onDragReleased = onDragReleased;
            _explicitItem = explicitItem;
            _onDroppedOnSellZone = onDroppedOnSellZone;
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

            _draggedItem = _explicitItem ?? target.userData as ItemInstance;
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
                HighlightMatchingSlots();
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
                bool handled = false;
                var dropTarget = FindDropTarget(evt.position);
                if (dropTarget != null
                    && !dropTarget.ClassListContains("equipment-slot--blocked")
                    && dropTarget.userData is EquipmentSlotType slotType)
                {
                    if (EquipmentSlotHelper.IsSlotMatch(
                            _draggedItem.Definition.Slot, slotType, _draggedItem.Definition.Handedness)
                        && _onDroppedOnSlot != null)
                    {
                        _onDroppedOnSlot.Invoke(_draggedItem.Uid, slotType);
                        handled = true;
                    }
                }

                if (!handled && _onDroppedOnSellZone != null && FindSellZone(evt.position) != null)
                {
                    _onDroppedOnSellZone.Invoke(_draggedItem.Uid);
                    handled = true;
                }

                if (!handled)
                    _onDragReleased?.Invoke();

                ClearAllHighlights();
                RemoveGhost();
            }
            else
            {
                _onClicked?.Invoke(_draggedItem);
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
            var rarity = _draggedItem.Definition.Rarity;

            GetRarityColors(rarity, out var ghostBg, out var ghostBorder, out var placeholderBg);

            _ghost = new VisualElement();
            _ghost.pickingMode = PickingMode.Ignore;
            _ghost.style.position = Position.Absolute;
            _ghost.style.width = 64;
            _ghost.style.height = 64;
            _ghost.style.borderTopWidth = 2;
            _ghost.style.borderBottomWidth = 2;
            _ghost.style.borderLeftWidth = 2;
            _ghost.style.borderRightWidth = 2;
            _ghost.style.borderTopLeftRadius = 4;
            _ghost.style.borderTopRightRadius = 4;
            _ghost.style.borderBottomLeftRadius = 4;
            _ghost.style.borderBottomRightRadius = 4;
            _ghost.style.alignItems = Align.Center;
            _ghost.style.justifyContent = Justify.Center;
            _ghost.style.opacity = 0.92f;
            _ghost.style.backgroundColor = ghostBg;
            _ghost.style.borderTopColor = ghostBorder;
            _ghost.style.borderBottomColor = ghostBorder;
            _ghost.style.borderLeftColor = ghostBorder;
            _ghost.style.borderRightColor = ghostBorder;

            var icon = new VisualElement();
            icon.pickingMode = PickingMode.Ignore;
            icon.style.width = 48;
            icon.style.height = 48;
            icon.style.borderTopLeftRadius = 3;
            icon.style.borderTopRightRadius = 3;
            icon.style.borderBottomLeftRadius = 3;
            icon.style.borderBottomRightRadius = 3;
            icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;

            var sourceIcon = target.Q("item-icon");
            if (sourceIcon != null)
            {
                var bgImg = sourceIcon.resolvedStyle.backgroundImage;
                if (bgImg.sprite != null)
                    icon.style.backgroundImage = new StyleBackground(bgImg.sprite);
                else if (bgImg.texture != null)
                    icon.style.backgroundImage = new StyleBackground(bgImg.texture);
                else
                    icon.style.backgroundColor = placeholderBg;
            }
            else
            {
                icon.style.backgroundColor = placeholderBg;
            }

            _ghost.Add(icon);

            _ghost.style.left = position.x - 32;
            _ghost.style.top = position.y - 32;

            root.Add(_ghost);
        }

        private static void GetRarityColors(
            Rarity rarity, out Color ghostBg, out Color ghostBorder, out Color placeholderBg)
        {
            switch (rarity)
            {
                case Rarity.Magic:
                    ghostBg = new Color(0.12f, 0.14f, 0.25f, 0.95f);
                    ghostBorder = new Color(0.35f, 0.51f, 1f, 0.7f);
                    placeholderBg = new Color(0.27f, 0.39f, 0.86f, 0.55f);
                    break;
                case Rarity.Rare:
                    ghostBg = new Color(0.20f, 0.20f, 0.12f, 0.95f);
                    ghostBorder = new Color(1f, 1f, 0.35f, 0.8f);
                    placeholderBg = new Color(0.78f, 0.78f, 0.24f, 0.50f);
                    break;
                case Rarity.Unique:
                    ghostBg = new Color(0.20f, 0.14f, 0.08f, 0.95f);
                    ghostBorder = new Color(0.69f, 0.38f, 0.15f, 0.9f);
                    placeholderBg = new Color(0.69f, 0.38f, 0.15f, 0.55f);
                    break;
                default:
                    ghostBg = new Color(0.16f, 0.16f, 0.22f, 0.95f);
                    ghostBorder = new Color(0.78f, 0.78f, 0.78f, 0.5f);
                    placeholderBg = new Color(0.71f, 0.71f, 0.71f, 0.55f);
                    break;
            }
        }

        private void RemoveGhost()
        {
            if (_ghost == null) return;
            _ghost.RemoveFromHierarchy();
            _ghost = null;
        }

        private void HighlightMatchingSlots()
        {
            if (_draggedItem == null) return;

            var root = target.panel?.visualTree;
            if (root == null) return;

            var itemSlot = _draggedItem.Definition.Slot;
            var handedness = _draggedItem.Definition.Handedness;
            root.Query(className: "equipment-slot").ForEach(el =>
            {
                if (el.ClassListContains("equipment-slot--blocked")) return;
                if (el.userData is EquipmentSlotType st && EquipmentSlotHelper.IsSlotMatch(itemSlot, st, handedness))
                    el.AddToClassList("equipment-slot--drop-hint");
            });

            if (_onDroppedOnSellZone != null)
            {
                root.Query(className: "sell-zone").ForEach(el =>
                    el.AddToClassList("sell-zone--drop-hint"));
            }
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

        private VisualElement FindSellZone(Vector2 position)
        {
            var picked = target.panel.Pick(position);
            while (picked != null)
            {
                if (picked.ClassListContains("sell-zone"))
                    return picked;
                picked = picked.parent;
            }
            return null;
        }

        private void UpdateDropTargetHighlights(Vector2 position)
        {
            ClearHoverHighlights();

            var dropTarget = FindDropTarget(position);
            if (dropTarget != null && !dropTarget.ClassListContains("equipment-slot--blocked")
                && dropTarget.userData is EquipmentSlotType slotType
                && EquipmentSlotHelper.IsSlotMatch(
                    _draggedItem.Definition.Slot, slotType, _draggedItem.Definition.Handedness))
            {
                dropTarget.AddToClassList("equipment-slot--drop-hover");
                return;
            }

            var sellTarget = FindSellZone(position);
            if (sellTarget != null)
                sellTarget.AddToClassList("sell-zone--drop-hover");
        }

        private void ClearHoverHighlights()
        {
            var root = target.panel?.visualTree;
            if (root == null) return;

            root.Query(className: "equipment-slot--drop-hover")
                .ForEach(el => el.RemoveFromClassList("equipment-slot--drop-hover"));
            root.Query(className: "sell-zone--drop-hover")
                .ForEach(el => el.RemoveFromClassList("sell-zone--drop-hover"));
        }

        private void ClearAllHighlights()
        {
            var root = target.panel?.visualTree;
            if (root == null) return;

            root.Query(className: "equipment-slot--drop-hover")
                .ForEach(el => el.RemoveFromClassList("equipment-slot--drop-hover"));
            root.Query(className: "equipment-slot--drop-hint")
                .ForEach(el => el.RemoveFromClassList("equipment-slot--drop-hint"));
            root.Query(className: "sell-zone--drop-hint")
                .ForEach(el => el.RemoveFromClassList("sell-zone--drop-hint"));
            root.Query(className: "sell-zone--drop-hover")
                .ForEach(el => el.RemoveFromClassList("sell-zone--drop-hover"));
        }
    }
}
