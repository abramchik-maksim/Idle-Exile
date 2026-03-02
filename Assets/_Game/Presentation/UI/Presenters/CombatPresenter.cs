using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Skills;
using Game.Domain.DTOs.Combat;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CombatPresenter : IStartable, ITickable, IDisposable
    {
        private readonly MainScreenView _mainScreenView;
        private readonly UtilitySkillRunner _utilityRunner;
        private readonly ISubscriber<BattleStartedDTO> _battleStartedSub;
        private readonly ISubscriber<BattleCompletedDTO> _battleCompletedSub;
        private readonly ISubscriber<LootDroppedDTO> _lootDroppedSub;

        private readonly List<IDisposable> _subscriptions = new();

        public CombatPresenter(
            MainScreenView mainScreenView,
            UtilitySkillRunner utilityRunner,
            ISubscriber<BattleStartedDTO> battleStartedSub,
            ISubscriber<BattleCompletedDTO> battleCompletedSub,
            ISubscriber<LootDroppedDTO> lootDroppedSub)
        {
            _mainScreenView = mainScreenView;
            _utilityRunner = utilityRunner;
            _battleStartedSub = battleStartedSub;
            _battleCompletedSub = battleCompletedSub;
            _lootDroppedSub = lootDroppedSub;
        }

        public void Start()
        {
            _subscriptions.Add(
                _battleStartedSub.Subscribe(dto =>
                    _mainScreenView.SetBattleInfo(dto.TierName, dto.BattleIndex, dto.TotalBattles)));

            _subscriptions.Add(
                _battleCompletedSub.Subscribe(dto =>
                    UnityEngine.Debug.Log($"[CombatPresenter] Battle {dto.BattleIndex + 1} completed! Rewards: {dto.Rewards.Count}")));

            _subscriptions.Add(
                _lootDroppedSub.Subscribe(dto =>
                    _mainScreenView.ShowLootNotification(dto.ItemName, dto.Rarity)));

            UnityEngine.Debug.Log("[CombatPresenter] Initialized.");
        }

        public void Tick()
        {
            _mainScreenView.RenderBuffs(_utilityRunner.GetActiveBuffs());
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
