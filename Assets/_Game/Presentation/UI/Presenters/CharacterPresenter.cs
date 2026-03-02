using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Skills;
using Game.Domain.DTOs.Stats;
using Game.Domain.Stats;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CharacterPresenter : IStartable, IDisposable
    {
        private readonly CharacterTabView _view;
        private readonly IGameStateProvider _gameState;
        private readonly UtilitySkillRunner _utilityRunner;
        private readonly ISubscriber<HeroStatsChangedDTO> _statsSub;

        private readonly List<IDisposable> _subscriptions = new();

        public CharacterPresenter(
            CharacterTabView view,
            IGameStateProvider gameState,
            UtilitySkillRunner utilityRunner,
            ISubscriber<HeroStatsChangedDTO> statsSub)
        {
            _view = view;
            _gameState = gameState;
            _utilityRunner = utilityRunner;
            _statsSub = statsSub;
        }

        public void Start()
        {
            RefreshStats();

            _subscriptions.Add(
                _statsSub.Subscribe(_ => RefreshStats()));

            _utilityRunner.OnBuffsChanged += RefreshStats;
        }

        private void RefreshStats()
        {
            var baseStats = _gameState.Hero.Stats.GetAllFinal();
            var buffBonuses = _utilityRunner.GetBuffBonuses();

            if (buffBonuses.Count == 0)
            {
                _view.RenderStats(baseStats);
                return;
            }

            var combined = new Dictionary<StatType, float>(baseStats);
            foreach (var kvp in buffBonuses)
            {
                if (combined.ContainsKey(kvp.Key))
                    combined[kvp.Key] += kvp.Value;
                else
                    combined[kvp.Key] = kvp.Value;
            }

            _view.RenderStats(combined);
        }

        public void Dispose()
        {
            _utilityRunner.OnBuffsChanged -= RefreshStats;

            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
