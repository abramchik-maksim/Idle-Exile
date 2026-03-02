using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Game.Domain.Skills;
using Game.Presentation.UI.Base;
using Game.Presentation.UI.DragDrop;
using Game.Presentation.UI.Tooltip;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class SkillsTabView : LayoutView
    {
        private VisualElement _categoryChooser;
        private VisualElement _cardMain;
        private VisualElement _cardUtility;

        private VisualElement _mainSkillsView;
        private VisualElement _mainSkillsGrid;
        private Button _btnBackMain;
        private VisualElement _loadoutSlotsMain;

        private VisualElement _utilitySkillsView;
        private VisualElement _utilitySkillsContainer;
        private Button _btnBackUtility;
        private VisualElement _loadoutSlotsUtility;

        public event Action<string> OnMainSkillRightClicked;
        public event Action<string> OnUtilitySkillRightClicked;
        public event Action<string, int> OnSkillDroppedOnSlot;
        public event Action<int> OnLoadoutSlotRightClicked;
        public event Action<int> OnLoadoutSlotDraggedOff;

        protected override void OnBind()
        {
            _categoryChooser = Q("category-chooser");
            _cardMain = Q("card-main");
            _cardUtility = Q("card-utility");

            _mainSkillsView = Q("main-skills-view");
            _mainSkillsGrid = Q("main-skills-grid");
            _btnBackMain = Q<Button>("btn-back-main");
            _loadoutSlotsMain = Q("loadout-slots-main");

            _utilitySkillsView = Q("utility-skills-view");
            _utilitySkillsContainer = Q("utility-skills-container");
            _btnBackUtility = Q<Button>("btn-back-utility");
            _loadoutSlotsUtility = Q("loadout-slots-utility");

            _cardMain.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 0) ShowMainSkills();
            });
            _cardUtility.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 0) ShowUtilitySkills();
            });

            _btnBackMain.clicked += ShowCategoryChooser;
            _btnBackUtility.clicked += ShowCategoryChooser;
        }

        protected override void OnShow()
        {
            ShowCategoryChooser();
        }

        private void ShowCategoryChooser()
        {
            _categoryChooser.style.display = DisplayStyle.Flex;
            _mainSkillsView.style.display = DisplayStyle.None;
            _utilitySkillsView.style.display = DisplayStyle.None;

            _cardMain.RemoveFromClassList("skill-category-card--selected");
            _cardUtility.RemoveFromClassList("skill-category-card--selected");
        }

        private void ShowMainSkills()
        {
            _cardMain.AddToClassList("skill-category-card--selected");
            _cardUtility.RemoveFromClassList("skill-category-card--selected");

            _categoryChooser.style.display = DisplayStyle.None;
            _mainSkillsView.style.display = DisplayStyle.Flex;
            _utilitySkillsView.style.display = DisplayStyle.None;
        }

        private void ShowUtilitySkills()
        {
            _cardUtility.AddToClassList("skill-category-card--selected");
            _cardMain.RemoveFromClassList("skill-category-card--selected");

            _categoryChooser.style.display = DisplayStyle.None;
            _mainSkillsView.style.display = DisplayStyle.None;
            _utilitySkillsView.style.display = DisplayStyle.Flex;
        }

        public void RenderLoadout(SkillLoadout loadout)
        {
            RenderLoadoutBar(_loadoutSlotsMain, loadout);
            RenderLoadoutBar(_loadoutSlotsUtility, loadout);
        }

        private void RenderLoadoutBar(VisualElement container, SkillLoadout loadout)
        {
            container.Clear();

            for (int i = 0; i < SkillLoadout.TotalSlots; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("skill-slot");
                slot.AddToClassList(i == 0 ? "skill-slot--main" : "skill-slot--utility");
                slot.userData = i;

                var skill = loadout.GetSlot(i);
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
                    var nameLabel = new Label(skill.Definition.Name);
                    nameLabel.style.fontSize = i == 0 ? 11 : 9;
                    nameLabel.style.color = new StyleColor(new UnityEngine.Color(0.9f, 0.85f, 0.7f));
                    nameLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
                    nameLabel.style.whiteSpace = WhiteSpace.Normal;
                    slot.Add(nameLabel);

                    int capturedIndex = i;
                    var capturedSkill = skill;

                    var dragManip = new SkillDragManipulator(
                        category: skill.Definition.Category,
                        explicitSkill: skill,
                        onDragReleased: () => OnLoadoutSlotDraggedOff?.Invoke(capturedIndex));
                    slot.AddManipulator(dragManip);

                    slot.RegisterCallback<PointerEnterEvent>(_ =>
                        SkillTooltip.Show(slot, capturedSkill, Root));
                    slot.RegisterCallback<PointerLeaveEvent>(_ => SkillTooltip.Hide());
                }

                int idx = i;
                slot.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (evt.button == 1 && loadout.GetSlot(idx) != null)
                    {
                        SkillTooltip.Hide();
                        OnLoadoutSlotRightClicked?.Invoke(idx);
                        evt.StopPropagation();
                    }
                });

                container.Add(slot);
            }
        }

        public void RenderMainSkills(IReadOnlyList<SkillInstance> mainSkills)
        {
            _mainSkillsGrid.Clear();

            foreach (var skill in mainSkills)
                _mainSkillsGrid.Add(CreateSkillSlot(skill, SkillCategory.Main));
        }

        public void RenderUtilitySkills(
            IReadOnlyList<SkillInstance> recovery,
            IReadOnlyList<SkillInstance> defense,
            IReadOnlyList<SkillInstance> enhancement)
        {
            _utilitySkillsContainer.Clear();

            AddSubCategorySection("Recovery", recovery);
            AddSubCategorySection("Defense", defense);
            AddSubCategorySection("Enhancement", enhancement);
        }

        private void AddSubCategorySection(string title, IReadOnlyList<SkillInstance> skills)
        {
            var header = new Label(title);
            header.AddToClassList("skill-category-header");
            _utilitySkillsContainer.Add(header);

            var row = new VisualElement();
            row.AddToClassList("skills-grid");

            if (skills.Count == 0)
            {
                var emptyLabel = new Label("No skills");
                emptyLabel.AddToClassList("stat-label");
                emptyLabel.style.opacity = 0.4f;
                row.Add(emptyLabel);
            }
            else
            {
                foreach (var skill in skills)
                    row.Add(CreateSkillSlot(skill, SkillCategory.Utility));
            }

            _utilitySkillsContainer.Add(row);
        }

        private VisualElement CreateSkillSlot(SkillInstance skill, SkillCategory category)
        {
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot");
            slot.userData = skill;

            var nameLabel = new Label(skill.Definition.Name);
            nameLabel.AddToClassList("item-label");
            nameLabel.style.fontSize = 10;
            slot.Add(nameLabel);

            var levelLabel = new Label($"Lv.{skill.Level}");
            levelLabel.style.fontSize = 9;
            levelLabel.style.color = new StyleColor(new UnityEngine.Color(0.7f, 0.7f, 0.7f));
            levelLabel.style.position = Position.Absolute;
            levelLabel.style.bottom = 2;
            levelLabel.style.right = 2;
            slot.Add(levelLabel);

            var dragManip = new SkillDragManipulator(
                category: category,
                onDroppedOnSlot: (uid, slotIndex) => OnSkillDroppedOnSlot?.Invoke(uid, slotIndex));
            slot.AddManipulator(dragManip);

            slot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button != 1) return;
                SkillTooltip.Hide();
                if (category == SkillCategory.Utility)
                    OnUtilitySkillRightClicked?.Invoke(skill.Uid);
                else
                    OnMainSkillRightClicked?.Invoke(skill.Uid);
                evt.StopPropagation();
            });

            slot.RegisterCallback<PointerEnterEvent>(_ =>
                SkillTooltip.Show(slot, skill, Root));
            slot.RegisterCallback<PointerLeaveEvent>(_ => SkillTooltip.Hide());

            return slot;
        }
    }
}
