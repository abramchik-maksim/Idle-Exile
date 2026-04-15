using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using Game.Application.Combat;
using Game.Application.Inventory;
using Game.Application.Ports;
using Game.Domain.Combat.Progression;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Progression;

namespace Game.Presentation.Combat
{
    public sealed class BattleFlowController : IStartable, ITickable, IDisposable
    {
        private readonly CombatBridge _bridge;
        private readonly IGameStateProvider _gameState;
        private readonly ICombatConfigProvider _combatConfig;
        private readonly ProgressBattleUseCase _progressBattle;
        private readonly GrantBattleRewardUseCase _grantReward;
        private readonly InventoryCommandService _inventoryCommands;
        private readonly IPublisher<BattleStartedDTO> _battleStartedPub;
        private readonly IPublisher<BattleCompletedDTO> _battleCompletedPub;
        private readonly IPublisher<WaveStartedDTO> _waveStartedPub;
        private readonly IPublisher<LootDroppedDTO> _lootDroppedPub;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private readonly IPublisher<MapChangedDTO> _mapChangedPub;
        private readonly IPublisher<MapChoiceOfferedDTO> _mapChoiceOfferedPub;
        private readonly ISubscriber<MapChosenDTO> _mapChosenSub;

        private BattleDefinition _currentBattle;
        private int _currentWaveIndex;
        private float _waveDelayTimer;
        private ProgressionResult _pendingChoice;
        private readonly List<IDisposable> _subscriptions = new();

        private enum BattlePhase { WaitingForBridge, WaveDelay, WaveActive, BattleComplete, WaitingForMapChoice }
        private BattlePhase _phase = BattlePhase.WaitingForBridge;

        public BattleFlowController(
            CombatBridge bridge,
            IGameStateProvider gameState,
            ICombatConfigProvider combatConfig,
            ProgressBattleUseCase progressBattle,
            GrantBattleRewardUseCase grantReward,
            InventoryCommandService inventoryCommands,
            IPublisher<BattleStartedDTO> battleStartedPub,
            IPublisher<BattleCompletedDTO> battleCompletedPub,
            IPublisher<WaveStartedDTO> waveStartedPub,
            IPublisher<LootDroppedDTO> lootDroppedPub,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            IPublisher<MapChangedDTO> mapChangedPub,
            IPublisher<MapChoiceOfferedDTO> mapChoiceOfferedPub,
            ISubscriber<MapChosenDTO> mapChosenSub)
        {
            _bridge = bridge;
            _gameState = gameState;
            _combatConfig = combatConfig;
            _progressBattle = progressBattle;
            _grantReward = grantReward;
            _inventoryCommands = inventoryCommands;
            _battleStartedPub = battleStartedPub;
            _battleCompletedPub = battleCompletedPub;
            _waveStartedPub = waveStartedPub;
            _lootDroppedPub = lootDroppedPub;
            _inventoryChangedPub = inventoryChangedPub;
            _mapChangedPub = mapChangedPub;
            _mapChoiceOfferedPub = mapChoiceOfferedPub;
            _mapChosenSub = mapChosenSub;
        }

        public void Start()
        {
            _subscriptions.Add(_mapChosenSub.Subscribe(OnMapChosen));
            _phase = BattlePhase.WaitingForBridge;
        }

        public void Tick()
        {
            switch (_phase)
            {
                case BattlePhase.WaitingForBridge:
                    TryStart();
                    break;
                case BattlePhase.WaveDelay:
                    UpdateWaveDelay();
                    break;
                case BattlePhase.WaveActive:
                    UpdateWaveActive();
                    break;
                case BattlePhase.BattleComplete:
                    OnBattleComplete();
                    break;
                case BattlePhase.WaitingForMapChoice:
                    break;
            }
        }

        private void TryStart()
        {
            if (!_bridge.IsReady) return;

            var progress = _gameState.Progress;
            _currentBattle = _combatConfig.GetBattle(
                progress.CurrentTier, progress.CurrentMap, progress.CurrentBattle);

            if (_currentBattle == null)
            {
                Debug.LogError("[BattleFlow] No battle found for current progress!");
                return;
            }

            StartBattle();
        }

        private void StartBattle()
        {
            _currentWaveIndex = 0;

            var progress = _gameState.Progress;
            var tier = _combatConfig.GetTier(progress.CurrentTier);
            int totalBattles = _combatConfig.GetBattleCount(progress.CurrentTier, progress.CurrentMap);

            _battleStartedPub.Publish(new BattleStartedDTO(
                progress.CurrentTier,
                progress.CurrentMap,
                progress.CurrentBattle,
                totalBattles,
                tier?.Name ?? "Unknown"
            ));

            Debug.Log($"[BattleFlow] Battle started: {_currentBattle.Id} (waves: {_currentBattle.Waves.Count})");
            StartWaveDelay();
        }

        private void StartWaveDelay()
        {
            if (_currentWaveIndex >= _currentBattle.Waves.Count)
            {
                _phase = BattlePhase.BattleComplete;
                return;
            }

            var wave = _currentBattle.Waves[_currentWaveIndex];
            _waveDelayTimer = wave.DelayBeforeWave;
            _phase = BattlePhase.WaveDelay;

            _waveStartedPub.Publish(new WaveStartedDTO(_currentWaveIndex, _currentBattle.Waves.Count));
        }

        private void UpdateWaveDelay()
        {
            _waveDelayTimer -= Time.deltaTime;
            if (_waveDelayTimer > 0f) return;

            float tierScaling = _combatConfig.GetTierScaling(_gameState.Progress.CurrentTier);
            _bridge.SpawnWave(_currentBattle.Waves[_currentWaveIndex], tierScaling);
            _phase = BattlePhase.WaveActive;
        }

        private void UpdateWaveActive()
        {
            if (_bridge.IsHeroDead())
            {
                OnHeroDied();
                return;
            }

            if (_bridge.GetAliveEnemyCount() > 0) return;

            _currentWaveIndex++;
            StartWaveDelay();
        }

        private void OnHeroDied()
        {
            var progress = _gameState.Progress;

            Debug.Log($"[BattleFlow] Hero died at battle {progress.CurrentBattle}! Retreating...");

            _bridge.DespawnAllEnemies();
            _bridge.RestoreHeroHealth();
            _bridge.ResetHeroPosition();

            if (progress.CurrentBattle > 0)
            {
                progress.CurrentBattle--;
                _currentBattle = _combatConfig.GetBattle(
                    progress.CurrentTier, progress.CurrentMap, progress.CurrentBattle);
            }

            StartBattle();
        }

        private void OnBattleComplete()
        {
            var progress = _gameState.Progress;

            _battleCompletedPub.Publish(new BattleCompletedDTO(
                progress.CurrentTier,
                progress.CurrentMap,
                progress.CurrentBattle,
                _currentBattle.Rewards
            ));

            Debug.Log($"[BattleFlow] Battle {_currentBattle.Id} completed!");

            _bridge.ResetHeroPosition();

            int globalStage = ComputeGlobalStage(progress);
            GrantRewards(progress.CurrentBattle, progress.CurrentTier, globalStage);

            var result = _progressBattle.Execute(progress);

            if (result.NeedsMapChoice)
            {
                _pendingChoice = result;
                OfferMapChoice(result);
                return;
            }

            _currentBattle = result.NextBattle;

            if (result.TierChanged)
                Debug.Log($"[BattleFlow] Tier advanced to {progress.CurrentTier}!");
            if (result.MapChanged)
            {
                Debug.Log($"[BattleFlow] Map advanced to {progress.CurrentMap}!");
                var newMap = _combatConfig.GetMap(progress.CurrentTier, progress.CurrentMap);
                if (newMap != null)
                    _mapChangedPub.Publish(new MapChangedDTO(newMap.LocationId, progress.CurrentTier, progress.CurrentMap));
            }

            StartBattle();
        }

        private void OfferMapChoice(ProgressionResult result)
        {
            _phase = BattlePhase.WaitingForMapChoice;

            int tierIdx = result.PendingTierIndex;
            int choiceIdx = result.PendingMapIndex;
            var (opt1, opt2) = _combatConfig.GetMapChoice(tierIdx, choiceIdx);

            var info1 = opt1 != null
                ? new MapOptionInfo(opt1.Id, opt1.Name, opt1.Description)
                : new MapOptionInfo("unknown", "Unknown Map", "");
            var info2 = opt2 != null
                ? new MapOptionInfo(opt2.Id, opt2.Name, opt2.Description)
                : new MapOptionInfo("unknown", "Unknown Map", "");

            _mapChoiceOfferedPub.Publish(new MapChoiceOfferedDTO(info1, info2, tierIdx, choiceIdx));
            Debug.Log($"[BattleFlow] Offering map choice: {info1.Name} vs {info2.Name}");
        }

        private void OnMapChosen(MapChosenDTO dto)
        {
            if (_phase != BattlePhase.WaitingForMapChoice || _pendingChoice == null) return;

            var progress = _gameState.Progress;
            int tierIdx = _pendingChoice.PendingTierIndex;
            int mapIdx = _pendingChoice.PendingMapIndex;

            var applyResult = _progressBattle.ApplyMapChoice(progress, tierIdx, mapIdx);
            _currentBattle = applyResult.NextBattle;
            _pendingChoice = null;

            Debug.Log($"[BattleFlow] Map chosen (option {dto.ChosenOptionIndex}). Tier={tierIdx}, Map={mapIdx}");

            var newMap = _combatConfig.GetMap(progress.CurrentTier, progress.CurrentMap);
            if (newMap != null)
                _mapChangedPub.Publish(new MapChangedDTO(newMap.LocationId, progress.CurrentTier, progress.CurrentMap));

            StartBattle();
        }

        private void GrantRewards(int battleIndex, int tierIndex, int globalStage)
        {
            var drops = _grantReward.Execute(battleIndex, tierIndex, globalStage);
            if (drops.Count == 0) return;

            var inventory = _gameState.Inventory;
            bool changed = false;

            foreach (var item in drops)
            {
                if (_inventoryCommands.TryAddItem(inventory, item))
                {
                    changed = true;
                    _lootDroppedPub.Publish(new LootDroppedDTO(
                        item.Definition.Name, item.Rarity));
                    Debug.Log($"[BattleFlow] Loot: {item.Definition.Name} ({item.Rarity})");
                }
                else
                {
                    Debug.Log($"[BattleFlow] Inventory full, discarded: {item.Definition.Name}");
                }
            }

            if (changed)
                _inventoryChangedPub.Publish(new InventoryChangedDTO());
        }

        private int ComputeGlobalStage(PlayerProgressData progress)
        {
            int stage = 0;
            for (int t = 0; t < progress.CurrentTier; t++)
            {
                int maps = _combatConfig.GetMapCount(t);
                for (int m = 0; m < maps; m++)
                    stage += _combatConfig.GetBattleCount(t, m);
            }
            for (int m = 0; m < progress.CurrentMap; m++)
                stage += _combatConfig.GetBattleCount(progress.CurrentTier, m);

            stage += progress.CurrentBattle + 1;
            return stage;
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
