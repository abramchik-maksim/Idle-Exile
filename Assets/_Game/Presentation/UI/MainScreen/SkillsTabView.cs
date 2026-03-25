using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Game.Domain.Items;
using Game.Domain.Skills;
using Game.Domain.Skills.Crafting;
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

        private ScrollView _gemList;
        private VisualElement _skillDropSlot;
        private Label _skillDropLabel;
        private VisualElement _tooltipEmpty;
        private VisualElement _tooltipDetails;
        private Label _tooltipSkillName;
        private Label _tooltipCategory;
        private Label _tooltipWeapon;
        private Label _tooltipDamageMult;
        private Label _tooltipAsMult;
        private VisualElement _tooltipAffixes;
        private Label _removalCurrencyLabel;
        private readonly VisualElement[] _affixSlots = new VisualElement[SkillAffixSlots.MaxSlots];
        private Button _filterAll, _filterFire, _filterCold, _filterLightning, _filterPhysical;
        private EventCallback<PointerUpEvent> _onCardMainPointerUp;
        private EventCallback<PointerUpEvent> _onCardUtilityPointerUp;
        private EventCallback<PointerUpEvent> _onSkillDropSlotRightClick;
        private readonly EventCallback<PointerUpEvent>[] _onAffixSlotRightClick = new EventCallback<PointerUpEvent>[SkillAffixSlots.MaxSlots];

        public event Action<string> OnMainSkillRightClicked;
        public event Action<string> OnUtilitySkillRightClicked;
        public event Action<string, int> OnSkillDroppedOnSlot;
        public event Action<int> OnLoadoutSlotRightClicked;
        public event Action<int> OnLoadoutSlotDraggedOff;

        public event Action<string> OnGemClicked;
        public event Action<int> OnAffixRemoveClicked;
        public event Action<string> OnCraftSkillSelected;
        public event Action OnCraftSkillRemoved;
        public event Action<SkillGemElement> OnFilterChanged;

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

            BindCraftingElements();

            _onCardMainPointerUp = HandleCardMainPointerUp;
            _onCardUtilityPointerUp = HandleCardUtilityPointerUp;
            _cardMain.RegisterCallback(_onCardMainPointerUp);
            _cardUtility.RegisterCallback(_onCardUtilityPointerUp);

            _btnBackMain.clicked += HandleBackClicked;
            _btnBackUtility.clicked += HandleBackClicked;
        }

        private void BindCraftingElements()
        {
            _gemList = Q<ScrollView>("gem-list");
            _skillDropSlot = Q("skill-drop-slot");
            _skillDropLabel = Q<Label>("skill-drop-label");
            _tooltipEmpty = Q("tooltip-empty");
            _tooltipDetails = Q("tooltip-details");
            _tooltipSkillName = Q<Label>("tooltip-skill-name");
            _tooltipCategory = Q<Label>("tooltip-skill-category");
            _tooltipWeapon = Q<Label>("tooltip-weapon");
            _tooltipDamageMult = Q<Label>("tooltip-damage-mult");
            _tooltipAsMult = Q<Label>("tooltip-as-mult");
            _tooltipAffixes = Q("tooltip-affixes");
            _removalCurrencyLabel = Q<Label>("removal-currency-label");

            for (int i = 0; i < SkillAffixSlots.MaxSlots; i++)
            {
                _affixSlots[i] = Q($"affix-slot-{i}");
                int capturedIndex = i;
                _onAffixSlotRightClick[i] = evt =>
                {
                    if (evt.button == 1) OnAffixRemoveClicked?.Invoke(capturedIndex);
                };
                _affixSlots[i].RegisterCallback(_onAffixSlotRightClick[i]);
            }

            _onSkillDropSlotRightClick = evt =>
            {
                if (evt.button == 1) OnCraftSkillRemoved?.Invoke();
            };
            _skillDropSlot.RegisterCallback(_onSkillDropSlotRightClick);

            _filterAll = Q<Button>("filter-all");
            _filterFire = Q<Button>("filter-fire");
            _filterCold = Q<Button>("filter-cold");
            _filterLightning = Q<Button>("filter-lightning");
            _filterPhysical = Q<Button>("filter-physical");

            _filterAll.clicked += HandleFilterAllClicked;
            _filterFire.clicked += HandleFilterFireClicked;
            _filterCold.clicked += HandleFilterColdClicked;
            _filterLightning.clicked += HandleFilterLightningClicked;
            _filterPhysical.clicked += HandleFilterPhysicalClicked;
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
                    emptyLabel.style.color = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
                    emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    slot.Add(emptyLabel);
                }
                else
                {
                    var nameLabel = new Label(skill.Definition.Name);
                    nameLabel.style.fontSize = i == 0 ? 11 : 9;
                    nameLabel.style.color = new StyleColor(new Color(0.9f, 0.85f, 0.7f));
                    nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
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
            levelLabel.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            levelLabel.style.position = Position.Absolute;
            levelLabel.style.bottom = 2;
            levelLabel.style.right = 2;
            slot.Add(levelLabel);

            if (category == SkillCategory.Main)
            {
                int affixCount = skill.Affixes.FilledCount;
                if (affixCount > 0)
                {
                    var affixBadge = new Label($"{affixCount}/6");
                    affixBadge.style.fontSize = 8;
                    affixBadge.style.color = new StyleColor(new Color(0.6f, 0.8f, 1f));
                    affixBadge.style.position = Position.Absolute;
                    affixBadge.style.top = 2;
                    affixBadge.style.right = 2;
                    slot.Add(affixBadge);
                }
            }

            var dragManip = new SkillDragManipulator(
                category: category,
                onDroppedOnSlot: (uid, slotIndex) => OnSkillDroppedOnSlot?.Invoke(uid, slotIndex));
            slot.AddManipulator(dragManip);

            slot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    SkillTooltip.Hide();
                    if (category == SkillCategory.Utility)
                        OnUtilitySkillRightClicked?.Invoke(skill.Uid);
                    else
                        OnMainSkillRightClicked?.Invoke(skill.Uid);
                    evt.StopPropagation();
                }
                else if (evt.button == 0 && category == SkillCategory.Main)
                {
                    OnCraftSkillSelected?.Invoke(skill.Uid);
                }
            });

            slot.RegisterCallback<PointerEnterEvent>(_ =>
                SkillTooltip.Show(slot, skill, Root));
            slot.RegisterCallback<PointerLeaveEvent>(_ => SkillTooltip.Hide());

            return slot;
        }

        #region Crafting Rendering

        public void RenderGemList(IReadOnlyList<GemDisplayData> gems)
        {
            _gemList.Clear();

            foreach (var gem in gems)
            {
                var entry = new VisualElement();
                entry.AddToClassList("gem-entry");
                entry.AddToClassList($"gem-entry--{gem.Element.ToString().ToLower()}");

                var gemName = new Label($"{gem.Name} ({gem.Level})");
                gemName.AddToClassList("gem-entry__name");
                entry.Add(gemName);

                var countLabel = new Label($"x{gem.Count}");
                countLabel.AddToClassList("gem-entry__count");
                entry.Add(countLabel);

                string capturedId = gem.Id;
                entry.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (evt.button == 0) OnGemClicked?.Invoke(capturedId);
                });

                _gemList.Add(entry);
            }
        }

        public void RenderCraftedSkill(SkillInstance skill)
        {
            if (skill == null)
            {
                _skillDropLabel.text = "No Skill";
                _skillDropLabel.style.display = DisplayStyle.Flex;
                _tooltipEmpty.style.display = DisplayStyle.Flex;
                _tooltipDetails.style.display = DisplayStyle.None;
                ClearAffixSlots();
                return;
            }

            _skillDropLabel.text = skill.Definition.Name;
            _skillDropLabel.style.display = DisplayStyle.Flex;

            _tooltipEmpty.style.display = DisplayStyle.None;
            _tooltipDetails.style.display = DisplayStyle.Flex;

            _tooltipSkillName.text = skill.Definition.Name;
            _tooltipCategory.text = $"Main Skill — Lv.{skill.Level}";
            _tooltipWeapon.text = $"Weapon: {skill.Definition.RequiredWeapon}";
            _tooltipDamageMult.text = $"Damage: {skill.Definition.DamageMultiplierPercent:F0}%";
            _tooltipAsMult.text = $"Attack Speed: {skill.Definition.AttackSpeedMultiplierPercent:F0}%";

            RenderAffixSlots(skill);
            RenderTooltipAffixes(skill);
        }

        public void RenderRemovalCurrency(int count)
        {
            _removalCurrencyLabel.text = $"Removal Orbs: {count}";
        }

        private void RenderAffixSlots(SkillInstance skill)
        {
            for (int i = 0; i < SkillAffixSlots.MaxSlots; i++)
            {
                _affixSlots[i].Clear();
                var affix = skill.Affixes.GetSlot(i);

                if (affix == null)
                {
                    _affixSlots[i].RemoveFromClassList("crafting-affix-slot--occupied");
                    RemoveRarityBorder(_affixSlots[i]);
                    var emptyLabel = new Label("Empty");
                    emptyLabel.AddToClassList("item-label--empty");
                    emptyLabel.style.fontSize = 9;
                    _affixSlots[i].Add(emptyLabel);
                }
                else
                {
                    _affixSlots[i].AddToClassList("crafting-affix-slot--occupied");
                    ApplyRarityBorder(_affixSlots[i], affix.Rarity);

                    var textLabel = new Label(affix.GetDescription());
                    textLabel.AddToClassList("crafting-affix-slot__text");
                    _affixSlots[i].Add(textLabel);

                    var tierLabel = new Label($"T{affix.Tier}");
                    tierLabel.AddToClassList("crafting-affix-slot__tier");
                    _affixSlots[i].Add(tierLabel);
                }
            }
        }

        private void RenderTooltipAffixes(SkillInstance skill)
        {
            _tooltipAffixes.Clear();

            var affixes = skill.Affixes.GetAll();
            if (affixes.Count == 0)
            {
                var noAffixes = new Label("No affixes");
                noAffixes.style.fontSize = 11;
                noAffixes.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                _tooltipAffixes.Add(noAffixes);
                return;
            }

            foreach (var affix in affixes)
            {
                var row = new VisualElement();
                row.style.marginBottom = 2;

                var desc = new Label(affix.GetDescription());
                desc.AddToClassList("item-tooltip__mod");
                desc.AddToClassList($"rarity-{affix.Rarity.ToString().ToLower()}");
                row.Add(desc);

                var tier = new Label($"Tier {affix.Tier} — {affix.Rarity}");
                tier.style.fontSize = 9;
                tier.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                row.Add(tier);

                _tooltipAffixes.Add(row);
            }
        }

        private void ClearAffixSlots()
        {
            for (int i = 0; i < SkillAffixSlots.MaxSlots; i++)
            {
                _affixSlots[i].Clear();
                RemoveRarityBorder(_affixSlots[i]);
                var emptyLabel = new Label("Empty");
                emptyLabel.AddToClassList("item-label--empty");
                emptyLabel.style.fontSize = 9;
                _affixSlots[i].Add(emptyLabel);
            }
        }

        private static void ApplyRarityBorder(VisualElement element, Rarity rarity)
        {
            RemoveRarityBorder(element);
            element.AddToClassList($"slot-border--{rarity.ToString().ToLower()}");
        }

        private static void RemoveRarityBorder(VisualElement element)
        {
            element.RemoveFromClassList("slot-border--normal");
            element.RemoveFromClassList("slot-border--magic");
            element.RemoveFromClassList("slot-border--rare");
            element.RemoveFromClassList("slot-border--unique");
        }

        public override void Dispose()
        {
            if (_cardMain != null && _onCardMainPointerUp != null) _cardMain.UnregisterCallback(_onCardMainPointerUp);
            if (_cardUtility != null && _onCardUtilityPointerUp != null) _cardUtility.UnregisterCallback(_onCardUtilityPointerUp);
            if (_btnBackMain != null) _btnBackMain.clicked -= HandleBackClicked;
            if (_btnBackUtility != null) _btnBackUtility.clicked -= HandleBackClicked;

            for (int i = 0; i < SkillAffixSlots.MaxSlots; i++)
            {
                if (_affixSlots[i] == null || _onAffixSlotRightClick[i] == null) continue;
                _affixSlots[i].UnregisterCallback(_onAffixSlotRightClick[i]);
            }

            if (_skillDropSlot != null && _onSkillDropSlotRightClick != null)
                _skillDropSlot.UnregisterCallback(_onSkillDropSlotRightClick);

            if (_filterAll != null) _filterAll.clicked -= HandleFilterAllClicked;
            if (_filterFire != null) _filterFire.clicked -= HandleFilterFireClicked;
            if (_filterCold != null) _filterCold.clicked -= HandleFilterColdClicked;
            if (_filterLightning != null) _filterLightning.clicked -= HandleFilterLightningClicked;
            if (_filterPhysical != null) _filterPhysical.clicked -= HandleFilterPhysicalClicked;
        }

        private void HandleCardMainPointerUp(PointerUpEvent evt)
        {
            if (evt.button == 0) ShowMainSkills();
        }

        private void HandleCardUtilityPointerUp(PointerUpEvent evt)
        {
            if (evt.button == 0) ShowUtilitySkills();
        }

        private void HandleBackClicked() => ShowCategoryChooser();
        private void HandleFilterAllClicked() => OnFilterChanged?.Invoke(SkillGemElement.Generic);
        private void HandleFilterFireClicked() => OnFilterChanged?.Invoke(SkillGemElement.Fire);
        private void HandleFilterColdClicked() => OnFilterChanged?.Invoke(SkillGemElement.Cold);
        private void HandleFilterLightningClicked() => OnFilterChanged?.Invoke(SkillGemElement.Lightning);
        private void HandleFilterPhysicalClicked() => OnFilterChanged?.Invoke(SkillGemElement.Physical);

        #endregion
    }

    public struct GemDisplayData
    {
        public string Id;
        public string Name;
        public SkillGemElement Element;
        public SkillGemLevel Level;
        public int Count;
    }
}
