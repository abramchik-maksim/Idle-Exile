using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Domain.DTOs.Stats;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CharacterPresenter : IStartable, IDisposable
    {
        private readonly CharacterTabView _view;
        private readonly IGameStateProvider _gameState;
        private readonly ISubscriber<HeroStatsChangedDTO> _statsSub;

        private readonly List<IDisposable> _subscriptions = new();

        public CharacterPresenter(
            CharacterTabView view,
            IGameStateProvider gameState,
            ISubscriber<HeroStatsChangedDTO> statsSub)
        {
            _view = view;
            _gameState = gameState;
            _statsSub = statsSub;
        }

        public void Start()
        {
            var initialStats = _gameState.Hero.Stats.GetAllFinal();
            _view.RenderStats(initialStats);

            _subscriptions.Add(
                _statsSub.Subscribe(dto => _view.RenderStats(dto.FinalStats)));

            UnityEngine.Debug.Log("[CharacterPresenter] Initialized.");
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
