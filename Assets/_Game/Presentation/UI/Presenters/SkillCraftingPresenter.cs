using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Skills;
using Game.Domain.DTOs.Skills;
using Game.Domain.Skills;
using Game.Domain.Skills.Crafting;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class SkillCraftingPresenter : IStartable, IDisposable
    {
        private readonly SkillsTabView _view;
        private readonly IGameStateProvider _gameState;
        private readonly ISkillGemConfigProvider _gemConfig;
        private readonly ApplySkillGemUseCase _applyGemUseCase;
        private readonly RemoveSkillAffixUseCase _removeAffixUseCase;
        private readonly SkillGemInventory _gemInventory;
        private readonly IPublisher<SkillAffixAddedDTO> _affixAddedPub;
        private readonly IPublisher<SkillAffixRemovedDTO> _affixRemovedPub;
        private readonly IPublisher<SkillGemUsedDTO> _gemUsedPub;

        private readonly List<IDisposable> _subscriptions = new();
        private SkillInstance _currentSkill;
        private SkillGemElement _currentFilter = SkillGemElement.Generic;

        public SkillCraftingPresenter(
            SkillsTabView view,
            IGameStateProvider gameState,
            ISkillGemConfigProvider gemConfig,
            ApplySkillGemUseCase applyGemUseCase,
            RemoveSkillAffixUseCase removeAffixUseCase,
            SkillGemInventory gemInventory,
            IPublisher<SkillAffixAddedDTO> affixAddedPub,
            IPublisher<SkillAffixRemovedDTO> affixRemovedPub,
            IPublisher<SkillGemUsedDTO> gemUsedPub)
        {
            _view = view;
            _gameState = gameState;
            _gemConfig = gemConfig;
            _applyGemUseCase = applyGemUseCase;
            _removeAffixUseCase = removeAffixUseCase;
            _gemInventory = gemInventory;
            _affixAddedPub = affixAddedPub;
            _affixRemovedPub = affixRemovedPub;
            _gemUsedPub = gemUsedPub;
        }

        public void Start()
        {
            _view.OnGemClicked += HandleGemClicked;
            _view.OnAffixRemoveClicked += HandleAffixRemove;
            _view.OnCraftSkillSelected += HandleSkillPlaced;
            _view.OnCraftSkillRemoved += HandleSkillRemoved;
            _view.OnFilterChanged += HandleFilterChanged;
            _gemInventory.OnChanged += HandleGemInventoryChanged;

            RefreshAll();
        }

        public void Dispose()
        {
            _view.OnGemClicked -= HandleGemClicked;
            _view.OnAffixRemoveClicked -= HandleAffixRemove;
            _view.OnCraftSkillSelected -= HandleSkillPlaced;
            _view.OnCraftSkillRemoved -= HandleSkillRemoved;
            _view.OnFilterChanged -= HandleFilterChanged;
            _gemInventory.OnChanged -= HandleGemInventoryChanged;

            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }

        private void HandleSkillPlaced(string skillUid)
        {
            _currentSkill = _gameState.Skills.Find(skillUid);
            if (_currentSkill == null) return;

            if (_currentSkill.Definition.Category != SkillCategory.Main)
            {
                _currentSkill = null;
                return;
            }

            _view.RenderCraftedSkill(_currentSkill);
        }

        private void HandleSkillRemoved()
        {
            _currentSkill = null;
            _view.RenderCraftedSkill(null);
        }

        private void HandleGemClicked(string gemId)
        {
            if (_currentSkill == null) return;

            var result = _applyGemUseCase.Execute(_currentSkill, gemId);

            if (result.IsSuccess)
            {
                _affixAddedPub.Publish(new SkillAffixAddedDTO(
                    _currentSkill.Uid, result.RolledAffix, result.SlotIndex));
                _gemUsedPub.Publish(new SkillGemUsedDTO(
                    gemId, result.RemainingGemCount));
            }

            _view.RenderCraftedSkill(_currentSkill);
            RefreshGemList();
        }

        private void HandleAffixRemove(int slotIndex)
        {
            if (_currentSkill == null) return;

            var result = _removeAffixUseCase.Execute(_currentSkill, slotIndex);

            if (result.IsSuccess)
            {
                _affixRemovedPub.Publish(new SkillAffixRemovedDTO(
                    _currentSkill.Uid, slotIndex));
            }

            _view.RenderCraftedSkill(_currentSkill);
            _view.RenderRemovalCurrency(_gemInventory.RemovalCurrencyCount);
        }

        private void HandleFilterChanged(SkillGemElement element)
        {
            _currentFilter = element;
            RefreshGemList();
        }

        private void HandleGemInventoryChanged()
        {
            RefreshGemList();
            _view.RenderRemovalCurrency(_gemInventory.RemovalCurrencyCount);
        }

        private void RefreshAll()
        {
            RefreshGemList();
            _view.RenderCraftedSkill(_currentSkill);
            _view.RenderRemovalCurrency(_gemInventory.RemovalCurrencyCount);
        }

        private void RefreshGemList()
        {
            var allGems = _gemConfig.GetAllGems();
            var displayList = new List<GemDisplayData>();

            foreach (var gem in allGems)
            {
                int count = _gemInventory.GetCount(gem.Id);
                if (count <= 0) continue;

                if (_currentFilter != SkillGemElement.Generic && gem.Element != _currentFilter)
                    continue;

                displayList.Add(new GemDisplayData
                {
                    Id = gem.Id,
                    Name = gem.Name,
                    Element = gem.Element,
                    Level = gem.Level,
                    Count = count
                });
            }

            _view.RenderGemList(displayList);
        }
    }
}
