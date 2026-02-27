using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Skills;
using Game.Domain.DTOs.Skills;
using Game.Domain.Skills;
using Game.Presentation.UI.MainScreen;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class SkillsPresenter : IStartable, IDisposable
    {
        private readonly SkillsTabView _view;
        private readonly IGameStateProvider _gameState;
        private readonly EquipSkillUseCase _equipSkillUseCase;
        private readonly UnequipSkillUseCase _unequipSkillUseCase;
        private readonly IPublisher<SkillEquippedDTO> _skillEquippedPub;
        private readonly IPublisher<SkillUnequippedDTO> _skillUnequippedPub;
        private readonly IPublisher<SkillsChangedDTO> _skillsChangedPub;
        private readonly ISubscriber<SkillsChangedDTO> _skillsChangedSub;
        private readonly ISubscriber<SkillEquippedDTO> _skillEquippedSub;
        private readonly ISubscriber<SkillUnequippedDTO> _skillUnequippedSub;

        private readonly List<IDisposable> _subscriptions = new();

        public SkillsPresenter(
            SkillsTabView view,
            IGameStateProvider gameState,
            EquipSkillUseCase equipSkillUseCase,
            UnequipSkillUseCase unequipSkillUseCase,
            IPublisher<SkillEquippedDTO> skillEquippedPub,
            IPublisher<SkillUnequippedDTO> skillUnequippedPub,
            IPublisher<SkillsChangedDTO> skillsChangedPub,
            ISubscriber<SkillsChangedDTO> skillsChangedSub,
            ISubscriber<SkillEquippedDTO> skillEquippedSub,
            ISubscriber<SkillUnequippedDTO> skillUnequippedSub)
        {
            _view = view;
            _gameState = gameState;
            _equipSkillUseCase = equipSkillUseCase;
            _unequipSkillUseCase = unequipSkillUseCase;
            _skillEquippedPub = skillEquippedPub;
            _skillUnequippedPub = skillUnequippedPub;
            _skillsChangedPub = skillsChangedPub;
            _skillsChangedSub = skillsChangedSub;
            _skillEquippedSub = skillEquippedSub;
            _skillUnequippedSub = skillUnequippedSub;
        }

        public void Start()
        {
            _view.OnMainSkillRightClicked += HandleEquipMainSkill;
            _view.OnUtilitySkillRightClicked += HandleEquipUtilitySkill;

            _subscriptions.Add(_skillsChangedSub.Subscribe(_ => RefreshAll()));
            _subscriptions.Add(_skillEquippedSub.Subscribe(_ => RefreshAll()));
            _subscriptions.Add(_skillUnequippedSub.Subscribe(_ => RefreshAll()));

            RefreshAll();

            Debug.Log("[SkillsPresenter] Initialized.");
        }

        private void HandleEquipMainSkill(string skillUid)
        {
            var skills = _gameState.Skills;
            var loadout = _gameState.Loadout;
            var inventory = _gameState.Inventory;

            var result = _equipSkillUseCase.Execute(skills, loadout, inventory, skillUid);
            if (!result.Success)
            {
                Debug.Log($"[SkillsPresenter] Failed to equip main skill {skillUid}");
                return;
            }

            _skillEquippedPub.Publish(new SkillEquippedDTO(skillUid, result.SlotIndex));
            _skillsChangedPub.Publish(new SkillsChangedDTO());
        }

        private void HandleEquipUtilitySkill(string skillUid)
        {
            var skills = _gameState.Skills;
            var loadout = _gameState.Loadout;
            var inventory = _gameState.Inventory;

            var result = _equipSkillUseCase.Execute(skills, loadout, inventory, skillUid);
            if (!result.Success)
            {
                Debug.Log($"[SkillsPresenter] Failed to equip utility skill {skillUid}");
                return;
            }

            _skillEquippedPub.Publish(new SkillEquippedDTO(skillUid, result.SlotIndex));
            _skillsChangedPub.Publish(new SkillsChangedDTO());
        }

        private void RefreshAll()
        {
            var skills = _gameState.Skills;

            _view.RenderMainSkills(skills.GetByCategory(SkillCategory.Main));
            _view.RenderUtilitySkills(
                skills.GetBySubCategory(UtilitySubCategory.Recovery),
                skills.GetBySubCategory(UtilitySubCategory.Defense),
                skills.GetBySubCategory(UtilitySubCategory.Enhancement));
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
