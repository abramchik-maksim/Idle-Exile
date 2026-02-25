using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Domain.DTOs.Combat;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CombatPresenter : IStartable, IDisposable
    {
        private readonly MainScreenView _mainScreenView;
        private readonly ISubscriber<BattleStartedDTO> _battleStartedSub;
        private readonly ISubscriber<BattleCompletedDTO> _battleCompletedSub;

        private readonly List<IDisposable> _subscriptions = new();

        public CombatPresenter(
            MainScreenView mainScreenView,
            ISubscriber<BattleStartedDTO> battleStartedSub,
            ISubscriber<BattleCompletedDTO> battleCompletedSub)
        {
            _mainScreenView = mainScreenView;
            _battleStartedSub = battleStartedSub;
            _battleCompletedSub = battleCompletedSub;
        }

        public void Start()
        {
            _subscriptions.Add(
                _battleStartedSub.Subscribe(dto =>
                    _mainScreenView.SetBattleInfo(dto.TierName, dto.BattleIndex, dto.TotalBattles)));

            _subscriptions.Add(
                _battleCompletedSub.Subscribe(dto =>
                    UnityEngine.Debug.Log($"[CombatPresenter] Battle {dto.BattleIndex + 1} completed! Rewards: {dto.Rewards.Count}")));

            UnityEngine.Debug.Log("[CombatPresenter] Initialized.");
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
