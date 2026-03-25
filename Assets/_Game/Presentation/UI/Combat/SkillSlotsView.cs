using System;
using UnityEngine.UIElements;
using Game.Domain.Skills;
using Game.Presentation.UI.Base;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.Combat
{
    public sealed class SkillSlotsView : LayoutView
    {
        private readonly VisualElement[] _slots = new VisualElement[SkillLoadout.TotalSlots];
        private readonly SkillInstance[] _currentSkills = new SkillInstance[SkillLoadout.TotalSlots];
        private readonly RadialFillElement[] _cooldownOverlays = new RadialFillElement[SkillLoadout.TotalSlots];
        private readonly EventCallback<PointerUpEvent>[] _rightClickCallbacks = new EventCallback<PointerUpEvent>[SkillLoadout.TotalSlots];
        private readonly EventCallback<PointerEnterEvent>[] _pointerEnterCallbacks = new EventCallback<PointerEnterEvent>[SkillLoadout.TotalSlots];
        private readonly EventCallback<PointerLeaveEvent>[] _pointerLeaveCallbacks = new EventCallback<PointerLeaveEvent>[SkillLoadout.TotalSlots];

        public event Action<int> OnSlotRightClicked;

        protected override void OnBind()
        {
            for (int i = 0; i < SkillLoadout.TotalSlots; i++)
            {
                _slots[i] = Q($"slot-{i}");

                var overlay = new RadialFillElement();
                _slots[i].Add(overlay);
                _cooldownOverlays[i] = overlay;

                int capturedIndex = i;
                _rightClickCallbacks[i] = evt =>
                {
                    if (evt.button == 1)
                    {
                        SkillTooltip.Hide();
                        OnSlotRightClicked?.Invoke(capturedIndex);
                        evt.StopPropagation();
                    }
                };

                _pointerEnterCallbacks[i] = _ =>
                {
                    var skill = _currentSkills[capturedIndex];
                    if (skill != null)
                        SkillTooltip.Show(_slots[capturedIndex], skill, Root);
                };
                _pointerLeaveCallbacks[i] = _ => SkillTooltip.Hide();

                _slots[i].RegisterCallback(_rightClickCallbacks[i]);
                _slots[i].RegisterCallback(_pointerEnterCallbacks[i]);
                _slots[i].RegisterCallback(_pointerLeaveCallbacks[i]);
            }
        }

        public void RenderLoadout(SkillLoadout loadout)
        {
            for (int i = 0; i < SkillLoadout.TotalSlots; i++)
            {
                var slot = _slots[i];
                var skill = loadout.GetSlot(i);
                _currentSkills[i] = skill;

                while (slot.childCount > 1)
                    slot.RemoveAt(0);

                if (skill == null)
                {
                    slot.AddToClassList("skill-slot--empty");
                    var emptyLabel = new Label(i == 0 ? "Main" : "Util");
                    emptyLabel.style.fontSize = 10;
                    emptyLabel.style.color = new StyleColor(new UnityEngine.Color(0.4f, 0.4f, 0.4f));
                    emptyLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
                    slot.Insert(0, emptyLabel);
                    _cooldownOverlays[i].FillAmount = 0f;
                }
                else
                {
                    slot.RemoveFromClassList("skill-slot--empty");

                    var nameLabel = new Label(skill.Definition.Name);
                    nameLabel.style.fontSize = i == 0 ? 11 : 9;
                    nameLabel.style.color = new StyleColor(new UnityEngine.Color(0.9f, 0.85f, 0.7f));
                    nameLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
                    nameLabel.style.whiteSpace = WhiteSpace.Normal;
                    slot.Insert(0, nameLabel);
                }
            }
        }

        public void UpdateCooldown(int slotIndex, float normalizedCooldown)
        {
            if (slotIndex < 0 || slotIndex >= SkillLoadout.TotalSlots) return;
            _cooldownOverlays[slotIndex].FillAmount = normalizedCooldown;
        }

        public override void Dispose()
        {
            for (int i = 0; i < SkillLoadout.TotalSlots; i++)
            {
                if (_slots[i] == null) continue;
                if (_rightClickCallbacks[i] != null) _slots[i].UnregisterCallback(_rightClickCallbacks[i]);
                if (_pointerEnterCallbacks[i] != null) _slots[i].UnregisterCallback(_pointerEnterCallbacks[i]);
                if (_pointerLeaveCallbacks[i] != null) _slots[i].UnregisterCallback(_pointerLeaveCallbacks[i]);
            }
        }
    }
}
