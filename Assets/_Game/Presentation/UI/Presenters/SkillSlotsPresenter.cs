using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Skills;
using Game.Domain.DTOs.Skills;
using Game.Presentation.UI.Combat;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class SkillSlotsPresenter : IStartable, IDisposable
    {
        private readonly SkillSlotsView _view;
        private readonly IGameStateProvider _gameState;
        private readonly UnequipSkillUseCase _unequipSkillUseCase;
        private readonly IPublisher<SkillUnequippedDTO> _skillUnequippedPub;
        private readonly IPublisher<SkillsChangedDTO> _skillsChangedPub;
        private readonly ISubscriber<SkillEquippedDTO> _skillEquippedSub;
        private readonly ISubscriber<SkillUnequippedDTO> _skillUnequippedSub;
        private readonly ISubscriber<SkillsChangedDTO> _skillsChangedSub;

        private readonly List<IDisposable> _subscriptions = new();

        public SkillSlotsPresenter(
            SkillSlotsView view,
            IGameStateProvider gameState,
            UnequipSkillUseCase unequipSkillUseCase,
            IPublisher<SkillUnequippedDTO> skillUnequippedPub,
            IPublisher<SkillsChangedDTO> skillsChangedPub,
            ISubscriber<SkillEquippedDTO> skillEquippedSub,
            ISubscriber<SkillUnequippedDTO> skillUnequippedSub,
            ISubscriber<SkillsChangedDTO> skillsChangedSub)
        {
            _view = view;
            _gameState = gameState;
            _unequipSkillUseCase = unequipSkillUseCase;
            _skillUnequippedPub = skillUnequippedPub;
            _skillsChangedPub = skillsChangedPub;
            _skillEquippedSub = skillEquippedSub;
            _skillUnequippedSub = skillUnequippedSub;
            _skillsChangedSub = skillsChangedSub;
        }

        public void Start()
        {
            _view.OnSlotRightClicked += HandleSlotRightClicked;

            _subscriptions.Add(_skillEquippedSub.Subscribe(_ => RefreshSlots()));
            _subscriptions.Add(_skillUnequippedSub.Subscribe(_ => RefreshSlots()));
            _subscriptions.Add(_skillsChangedSub.Subscribe(_ => RefreshSlots()));

            RefreshSlots();

            Debug.Log("[SkillSlotsPresenter] Initialized.");
        }

        private void HandleSlotRightClicked(int slotIndex)
        {
            var loadout = _gameState.Loadout;

            var result = _unequipSkillUseCase.Execute(loadout, slotIndex);
            if (!result.Success) return;

            _skillUnequippedPub.Publish(new SkillUnequippedDTO(slotIndex));
            _skillsChangedPub.Publish(new SkillsChangedDTO());
        }

        private void RefreshSlots()
        {
            _view.RenderLoadout(_gameState.Loadout);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
