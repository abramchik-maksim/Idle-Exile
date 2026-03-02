using System;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Skills;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.DragDrop
{
    public sealed class SkillDragManipulator : PointerManipulator
    {
        private readonly Action<string, int> _onDroppedOnSlot;
        private readonly Action _onDragReleased;
        private readonly SkillInstance _explicitSkill;
        private readonly SkillCategory _category;

        private VisualElement _ghost;
        private bool _isDragging;
        private Vector2 _startPosition;
        private SkillInstance _draggedSkill;

        private const float DragThreshold = 5f;

        public SkillDragManipulator(
            SkillCategory category,
            Action<string, int> onDroppedOnSlot = null,
            Action onDragReleased = null,
            SkillInstance explicitSkill = null)
        {
            _category = category;
            _onDroppedOnSlot = onDroppedOnSlot;
            _onDragReleased = onDragReleased;
            _explicitSkill = explicitSkill;
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

            _draggedSkill = _explicitSkill ?? target.userData as SkillInstance;
            if (_draggedSkill == null) return;

            _startPosition = evt.position;
            _isDragging = false;
            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_draggedSkill == null || !target.HasPointerCapture(evt.pointerId))
                return;

            Vector2 currentPos = evt.position;

            if (!_isDragging)
            {
                if (Vector2.Distance(_startPosition, currentPos) < DragThreshold)
                    return;

                _isDragging = true;
                SkillTooltip.Hide();
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
            if (_draggedSkill == null) return;

            if (_isDragging)
            {
                bool handled = false;
                var dropTarget = FindDropTarget(evt.position);
                if (dropTarget != null && dropTarget.userData is int slotIndex)
                {
                    bool validDrop = (_category == SkillCategory.Main && slotIndex == SkillLoadout.MainSlotIndex)
                                     || (_category == SkillCategory.Utility && slotIndex >= SkillLoadout.FirstUtilitySlotIndex);

                    if (validDrop)
                    {
                        _onDroppedOnSlot?.Invoke(_draggedSkill.Uid, slotIndex);
                        handled = true;
                    }
                }

                if (!handled)
                    _onDragReleased?.Invoke();

                ClearAllHighlights();
                RemoveGhost();
            }

            _isDragging = false;
            _draggedSkill = null;
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            ClearAllHighlights();
            RemoveGhost();
            _isDragging = false;
            _draggedSkill = null;
        }

        private void CreateGhost(Vector2 position)
        {
            var root = target.panel.visualTree;

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

            var isMain = _category == SkillCategory.Main;
            _ghost.style.backgroundColor = isMain
                ? new Color(0.25f, 0.20f, 0.10f, 0.95f)
                : new Color(0.12f, 0.18f, 0.25f, 0.95f);
            var borderColor = isMain
                ? new Color(0.55f, 0.43f, 0.20f, 0.9f)
                : new Color(0.30f, 0.50f, 0.70f, 0.9f);
            _ghost.style.borderTopColor = borderColor;
            _ghost.style.borderBottomColor = borderColor;
            _ghost.style.borderLeftColor = borderColor;
            _ghost.style.borderRightColor = borderColor;

            var label = new Label(_draggedSkill.Definition.Name);
            label.pickingMode = PickingMode.Ignore;
            label.style.fontSize = 10;
            label.style.color = new Color(0.9f, 0.85f, 0.7f);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.whiteSpace = WhiteSpace.Normal;
            _ghost.Add(label);

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

        private void HighlightMatchingSlots()
        {
            var root = target.panel?.visualTree;
            if (root == null) return;

            root.Query(className: "skill-slot").ForEach(el =>
            {
                if (el.userData is not int slotIndex) return;

                bool matches = (_category == SkillCategory.Main && slotIndex == SkillLoadout.MainSlotIndex)
                               || (_category == SkillCategory.Utility && slotIndex >= SkillLoadout.FirstUtilitySlotIndex);

                if (matches)
                    el.AddToClassList("skill-slot--drop-hint");
            });
        }

        private VisualElement FindDropTarget(Vector2 position)
        {
            var picked = target.panel.Pick(position);
            while (picked != null)
            {
                if (picked.ClassListContains("skill-slot"))
                    return picked;
                picked = picked.parent;
            }
            return null;
        }

        private void UpdateDropTargetHighlights(Vector2 position)
        {
            ClearHoverHighlights();

            var dropTarget = FindDropTarget(position);
            if (dropTarget == null || dropTarget.userData is not int slotIndex) return;

            bool valid = (_category == SkillCategory.Main && slotIndex == SkillLoadout.MainSlotIndex)
                         || (_category == SkillCategory.Utility && slotIndex >= SkillLoadout.FirstUtilitySlotIndex);

            if (valid)
                dropTarget.AddToClassList("skill-slot--drop-hover");
        }

        private void ClearHoverHighlights()
        {
            var root = target.panel?.visualTree;
            if (root == null) return;

            root.Query(className: "skill-slot--drop-hover")
                .ForEach(el => el.RemoveFromClassList("skill-slot--drop-hover"));
        }

        private void ClearAllHighlights()
        {
            var root = target.panel?.visualTree;
            if (root == null) return;

            root.Query(className: "skill-slot--drop-hover")
                .ForEach(el => el.RemoveFromClassList("skill-slot--drop-hover"));
            root.Query(className: "skill-slot--drop-hint")
                .ForEach(el => el.RemoveFromClassList("skill-slot--drop-hint"));
        }
    }
}
