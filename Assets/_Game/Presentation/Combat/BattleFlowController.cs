using System;
using MessagePipe;
using UnityEngine;
using VContainer.Unity;
using Game.Application.Combat;
using Game.Application.Inventory;
using Game.Application.Ports;
using Game.Domain.Combat.Progression;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Inventory;

namespace Game.Presentation.Combat
{
    public sealed class BattleFlowController : IStartable, ITickable, IDisposable
    {
        private readonly CombatBridge _bridge;
        private readonly IGameStateProvider _gameState;
        private readonly ICombatConfigProvider _combatConfig;
        private readonly ProgressBattleUseCase _progressBattle;
        private readonly GrantBattleRewardUseCase _grantReward;
        private readonly AddItemToInventoryUseCase _addItem;
        private readonly IPublisher<BattleStartedDTO> _battleStartedPub;
        private readonly IPublisher<BattleCompletedDTO> _battleCompletedPub;
        private readonly IPublisher<WaveStartedDTO> _waveStartedPub;
        private readonly IPublisher<LootDroppedDTO> _lootDroppedPub;
        private readonly IPublisher<InventoryChangedDTO> _inventoryChangedPub;

        private BattleDefinition _currentBattle;
        private int _currentWaveIndex;
        private float _waveDelayTimer;

        private enum BattlePhase { WaitingForBridge, WaveDelay, WaveActive, BattleComplete }
        private BattlePhase _phase = BattlePhase.WaitingForBridge;

        public BattleFlowController(
            CombatBridge bridge,
            IGameStateProvider gameState,
            ICombatConfigProvider combatConfig,
            ProgressBattleUseCase progressBattle,
            GrantBattleRewardUseCase grantReward,
            AddItemToInventoryUseCase addItem,
            IPublisher<BattleStartedDTO> battleStartedPub,
            IPublisher<BattleCompletedDTO> battleCompletedPub,
            IPublisher<WaveStartedDTO> waveStartedPub,
            IPublisher<LootDroppedDTO> lootDroppedPub,
            IPublisher<InventoryChangedDTO> inventoryChangedPub)
        {
            _bridge = bridge;
            _gameState = gameState;
            _combatConfig = combatConfig;
            _progressBattle = progressBattle;
            _grantReward = grantReward;
            _addItem = addItem;
            _battleStartedPub = battleStartedPub;
            _battleCompletedPub = battleCompletedPub;
            _waveStartedPub = waveStartedPub;
            _lootDroppedPub = lootDroppedPub;
            _inventoryChangedPub = inventoryChangedPub;
        }

        public void Start()
        {
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
            if (_bridge.GetAliveEnemyCount() > 0) return;

            _currentWaveIndex++;
            StartWaveDelay();
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

            GrantRewards(progress.CurrentBattle, progress.CurrentTier);

            var result = _progressBattle.Execute(progress);
            _currentBattle = result.NextBattle;

            if (result.TierChanged)
                Debug.Log($"[BattleFlow] Tier advanced to {progress.CurrentTier}!");
            if (result.MapChanged)
                Debug.Log($"[BattleFlow] Map advanced to {progress.CurrentMap}!");

            StartBattle();
        }

        private void GrantRewards(int battleIndex, int tierIndex)
        {
            var drops = _grantReward.Execute(battleIndex, tierIndex);
            if (drops.Count == 0) return;

            var inventory = _gameState.Inventory;
            bool changed = false;

            foreach (var item in drops)
            {
                if (_addItem.Execute(inventory, item))
                {
                    changed = true;
                    _lootDroppedPub.Publish(new LootDroppedDTO(
                        item.Definition.Name, item.Definition.Rarity));
                    Debug.Log($"[BattleFlow] Loot: {item.Definition.Name} ({item.Definition.Rarity})");
                }
                else
                {
                    Debug.Log($"[BattleFlow] Inventory full, discarded: {item.Definition.Name}");
                }
            }

            if (changed)
                _inventoryChangedPub.Publish(new InventoryChangedDTO());
        }

        public void Dispose() { }
    }
}
