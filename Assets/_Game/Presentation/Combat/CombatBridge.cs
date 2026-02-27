using System;
using System.Collections.Generic;
using MessagePipe;
using UnityEngine;
using VContainer;
using Unity.Entities;
using Unity.Mathematics;
using Game.Application.Combat;
using Game.Application.Inventory;
using Game.Application.Ports;
using Game.Domain.Combat;
using Game.Domain.Combat.Progression;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Inventory;
using Game.Domain.DTOs.Stats;
using Game.Domain.Items;
using Game.Domain.Stats;
using Game.Presentation.Combat.Components;
using Game.Presentation.Combat.Rendering;
using Game.Presentation.Combat.Systems;

namespace Game.Presentation.Combat
{
    public sealed class CombatBridge : MonoBehaviour
    {
        private IGameStateProvider _gameState;
        private ICombatConfigProvider _combatConfig;
        private ProgressBattleUseCase _progressBattle;
        private GrantBattleRewardUseCase _grantReward;
        private AddItemToInventoryUseCase _addItem;
        private DamageNumberPool _damagePool;
        private IPublisher<BattleStartedDTO> _battleStartedPub;
        private IPublisher<BattleCompletedDTO> _battleCompletedPub;
        private IPublisher<WaveStartedDTO> _waveStartedPub;
        private IPublisher<DamageDealtDTO> _damageDealtPub;
        private IPublisher<EnemyKilledDTO> _enemyKilledPub;
        private IPublisher<LootDroppedDTO> _lootDroppedPub;
        private IPublisher<InventoryChangedDTO> _inventoryChangedPub;
        private ISubscriber<HeroStatsChangedDTO> _heroStatsChangedSub;

        private EntityManager _entityManager;
        private EntityQuery _aliveEnemyQuery;
        private DamageEventBufferSystem _damageBufferSystem;
        private Entity _heroEntity;
        private IDisposable _statsSubscription;

        private BattleDefinition _currentBattle;
        private int _currentWaveIndex;
        private float _waveDelayTimer;
        private int _nextActorId;

        private bool _initialized;

        private enum BattlePhase { Idle, WaveDelay, WaveActive, BattleComplete }
        private BattlePhase _phase = BattlePhase.Idle;

        [Inject]
        public void Construct(
            IGameStateProvider gameState,
            ICombatConfigProvider combatConfig,
            ProgressBattleUseCase progressBattle,
            GrantBattleRewardUseCase grantReward,
            AddItemToInventoryUseCase addItem,
            DamageNumberPool damagePool,
            IPublisher<BattleStartedDTO> battleStartedPub,
            IPublisher<BattleCompletedDTO> battleCompletedPub,
            IPublisher<WaveStartedDTO> waveStartedPub,
            IPublisher<DamageDealtDTO> damageDealtPub,
            IPublisher<EnemyKilledDTO> enemyKilledPub,
            IPublisher<LootDroppedDTO> lootDroppedPub,
            IPublisher<InventoryChangedDTO> inventoryChangedPub,
            ISubscriber<HeroStatsChangedDTO> heroStatsChangedSub)
        {
            _gameState = gameState;
            _combatConfig = combatConfig;
            _progressBattle = progressBattle;
            _grantReward = grantReward;
            _addItem = addItem;
            _damagePool = damagePool;
            _battleStartedPub = battleStartedPub;
            _battleCompletedPub = battleCompletedPub;
            _waveStartedPub = waveStartedPub;
            _damageDealtPub = damageDealtPub;
            _enemyKilledPub = enemyKilledPub;
            _lootDroppedPub = lootDroppedPub;
            _inventoryChangedPub = inventoryChangedPub;
            _heroStatsChangedSub = heroStatsChangedSub;

            _statsSubscription = _heroStatsChangedSub.Subscribe(OnHeroStatsChanged);
        }

        private void Update()
        {
            if (!TryInitialize()) return;

            switch (_phase)
            {
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

        private void LateUpdate()
        {
            if (!_initialized) return;
            ProcessDamageEvents();
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;
            if (_gameState?.Hero == null) return false;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            _entityManager = world.EntityManager;

            _aliveEnemyQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<DeadTag>()
            );

            _damageBufferSystem = world.GetExistingSystemManaged<DamageEventBufferSystem>();

            SpawnHeroEntity();

            var progress = _gameState.Progress;
            _currentBattle = _combatConfig.GetBattle(progress.CurrentTier, progress.CurrentMap, progress.CurrentBattle);

            if (_currentBattle == null)
            {
                Debug.LogError("[CombatBridge] No battle found for current progress!");
                return false;
            }

            _initialized = true;
            StartBattle();
            return true;
        }

        private void SpawnHeroEntity()
        {
            var hero = _gameState.Hero;
            _heroEntity = _entityManager.CreateEntity(
                typeof(HeroTag),
                typeof(Position2D),
                typeof(CombatStats),
                typeof(AttackCooldown),
                typeof(ActorId)
            );

            float attackSpeed = hero.Stats.GetFinal(StatType.AttackSpeed);
            float cooldown = attackSpeed > 0 ? 1f / attackSpeed : 1f;

            _entityManager.SetComponentData(_heroEntity, new Position2D { Value = new float2(0f, -1.7f) });
            _entityManager.SetComponentData(_heroEntity, new CombatStats
            {
                MaxHealth = hero.Stats.GetFinal(StatType.MaxHealth),
                CurrentHealth = hero.Stats.GetFinal(StatType.CurrentHealth),
                PhysicalDamage = hero.Stats.GetFinal(StatType.PhysicalDamage),
                AttackSpeed = attackSpeed,
                Armor = hero.Stats.GetFinal(StatType.Armor),
                MoveSpeed = hero.Stats.GetFinal(StatType.MovementSpeed)
            });
            _entityManager.SetComponentData(_heroEntity, new AttackCooldown
            {
                Cooldown = cooldown,
                Timer = cooldown
            });
            _entityManager.SetComponentData(_heroEntity, new ActorId { Value = _nextActorId++ });

            Debug.Log($"[CombatBridge] Hero entity created. Damage: {hero.Stats.GetFinal(StatType.PhysicalDamage)}, AS: {attackSpeed}");
        }

        private void OnHeroStatsChanged(HeroStatsChangedDTO dto)
        {
            if (!_initialized || !_entityManager.Exists(_heroEntity)) return;

            float physDmg = dto.FinalStats.TryGetValue(StatType.PhysicalDamage, out var d) ? d : 10f;
            float maxHp = dto.FinalStats.TryGetValue(StatType.MaxHealth, out var h) ? h : 100f;
            float armor = dto.FinalStats.TryGetValue(StatType.Armor, out var a) ? a : 5f;
            float atkSpd = dto.FinalStats.TryGetValue(StatType.AttackSpeed, out var s) ? s : 1f;
            float moveSpd = dto.FinalStats.TryGetValue(StatType.MovementSpeed, out var m) ? m : 3f;

            var stats = _entityManager.GetComponentData<CombatStats>(_heroEntity);
            stats.MaxHealth = maxHp;
            stats.PhysicalDamage = physDmg;
            stats.Armor = armor;
            stats.AttackSpeed = atkSpd;
            stats.MoveSpeed = moveSpd;
            _entityManager.SetComponentData(_heroEntity, stats);

            float cooldown = atkSpd > 0 ? 1f / atkSpd : 1f;
            var cd = _entityManager.GetComponentData<AttackCooldown>(_heroEntity);
            cd.Cooldown = cooldown;
            _entityManager.SetComponentData(_heroEntity, cd);

            Debug.Log($"[CombatBridge] Hero stats updated. Damage: {physDmg}, AS: {atkSpd}");
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

            Debug.Log($"[CombatBridge] Battle started: {_currentBattle.Id} (waves: {_currentBattle.Waves.Count})");
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

            SpawnWave(_currentBattle.Waves[_currentWaveIndex]);
            _phase = BattlePhase.WaveActive;
        }

        private void UpdateWaveActive()
        {
            int alive = _aliveEnemyQuery.CalculateEntityCount();
            if (alive > 0) return;

            _currentWaveIndex++;
            StartWaveDelay();
        }

        private void SpawnWave(WaveDefinition wave)
        {
            float tierScaling = _combatConfig.GetTierScaling(_gameState.Progress.CurrentTier);

            foreach (var spawn in wave.Spawns)
            {
                var enemyDef = _combatConfig.GetEnemy(spawn.EnemyDefinitionId);
                if (enemyDef == null)
                {
                    Debug.LogWarning($"[CombatBridge] Enemy definition not found: {spawn.EnemyDefinitionId}");
                    continue;
                }

                for (int i = 0; i < spawn.Count; i++)
                {
                    float xSpread = (i - (spawn.Count - 1) / 2f) * 1.0f;
                    xSpread += UnityEngine.Random.Range(-0.3f, 0.3f);
                    float ySpawn = UnityEngine.Random.Range(5.5f, 7f);

                    var entity = _entityManager.CreateEntity(
                        typeof(EnemyTag),
                        typeof(Position2D),
                        typeof(CombatStats),
                        typeof(ActorId)
                    );

                    _entityManager.SetComponentData(entity, new Position2D
                    {
                        Value = new float2(xSpread, ySpawn)
                    });

                    _entityManager.SetComponentData(entity, new CombatStats
                    {
                        MaxHealth = enemyDef.BaseHealth * tierScaling,
                        CurrentHealth = enemyDef.BaseHealth * tierScaling,
                        PhysicalDamage = enemyDef.BaseDamage * tierScaling,
                        AttackSpeed = 0.8f,
                        Armor = enemyDef.BaseArmor * tierScaling,
                        MoveSpeed = enemyDef.BaseSpeed
                    });

                    _entityManager.SetComponentData(entity, new ActorId { Value = _nextActorId++ });
                }
            }

            Debug.Log($"[CombatBridge] Wave {_currentWaveIndex} spawned.");
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

            Debug.Log($"[CombatBridge] Battle {_currentBattle.Id} completed!");

            GrantRewards(progress.CurrentBattle, progress.CurrentTier);

            var result = _progressBattle.Execute(progress);
            _currentBattle = result.NextBattle;

            if (result.TierChanged)
                Debug.Log($"[CombatBridge] Tier advanced to {progress.CurrentTier}!");
            if (result.MapChanged)
                Debug.Log($"[CombatBridge] Map advanced to {progress.CurrentMap}!");

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
                    Debug.Log($"[CombatBridge] Loot: {item.Definition.Name} ({item.Definition.Rarity})");
                }
                else
                {
                    Debug.Log($"[CombatBridge] Inventory full, discarded: {item.Definition.Name}");
                }
            }

            if (changed)
                _inventoryChangedPub.Publish(new InventoryChangedDTO());
        }

        private void ProcessDamageEvents()
        {
            if (_damageBufferSystem == null) return;

            foreach (var evt in _damageBufferSystem.FrameEvents)
            {
                _damagePool.Show(
                    new Vector3(evt.WorldX, evt.WorldY, 0f),
                    evt.Amount,
                    evt.IsCritical
                );

                _damageDealtPub.Publish(new DamageDealtDTO(
                    new DamageResult(evt.Amount, evt.Amount, evt.IsCritical, DamageType.Physical),
                    true,
                    evt.WorldX,
                    evt.WorldY
                ));
            }
        }

        private void OnDestroy()
        {
            _statsSubscription?.Dispose();
            if (_initialized)
                _aliveEnemyQuery.Dispose();
        }
    }
}
