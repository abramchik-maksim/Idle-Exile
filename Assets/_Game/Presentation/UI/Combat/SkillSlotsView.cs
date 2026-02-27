using System;
using UnityEngine.UIElements;
using Game.Domain.Skills;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.Combat
{
    public sealed class SkillSlotsView : LayoutView
    {
        private readonly VisualElement[] _slots = new VisualElement[SkillLoadout.TotalSlots];

        public event Action<int> OnSlotRightClicked;

        protected override void OnBind()
        {
            for (int i = 0; i < SkillLoadout.TotalSlots; i++)
            {
                _slots[i] = Q($"slot-{i}");

                int capturedIndex = i;
                _slots[i].RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (evt.button == 1)
                    {
                        OnSlotRightClicked?.Invoke(capturedIndex);
                        evt.StopPropagation();
                    }
                });
            }
        }

        public void RenderLoadout(SkillLoadout loadout)
        {
            for (int i = 0; i < SkillLoadout.TotalSlots; i++)
            {
                var slot = _slots[i];
                var skill = loadout.GetSlot(i);

                slot.Clear();

                if (skill == null)
                {
                    slot.AddToClassList("skill-slot--empty");
                    var emptyLabel = new Label(i == 0 ? "Main" : "Util");
                    emptyLabel.style.fontSize = 10;
                    emptyLabel.style.color = new StyleColor(new UnityEngine.Color(0.4f, 0.4f, 0.4f));
                    emptyLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
                    slot.Add(emptyLabel);
                }
                else
                {
                    slot.RemoveFromClassList("skill-slot--empty");

                    var nameLabel = new Label(skill.Definition.Name);
                    nameLabel.style.fontSize = i == 0 ? 11 : 9;
                    nameLabel.style.color = new StyleColor(new UnityEngine.Color(0.9f, 0.85f, 0.7f));
                    nameLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
                    nameLabel.style.whiteSpace = WhiteSpace.Normal;
                    slot.Add(nameLabel);
                }
            }
        }
    }
}
