using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using Unity.Entities;
using Unity.Mathematics;
using Game.Application.Ports;
using Game.Domain.Combat;
using Game.Domain.Combat.Progression;
using Game.Domain.DTOs.Combat;
using Game.Domain.DTOs.Stats;
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
        private DamageNumberPool _damagePool;
        private IPublisher<DamageDealtDTO> _damageDealtPub;
        private ISubscriber<HeroStatsChangedDTO> _heroStatsChangedSub;

        private EntityManager _entityManager;
        private EntityQuery _aliveEnemyQuery;
        private DamageEventBufferSystem _damageBufferSystem;
        private Entity _heroEntity;
        private IDisposable _statsSubscription;
        private int _nextActorId;

        public bool IsReady { get; private set; }

        [Inject]
        public void Construct(
            IGameStateProvider gameState,
            ICombatConfigProvider combatConfig,
            DamageNumberPool damagePool,
            IPublisher<DamageDealtDTO> damageDealtPub,
            ISubscriber<HeroStatsChangedDTO> heroStatsChangedSub)
        {
            _gameState = gameState;
            _combatConfig = combatConfig;
            _damagePool = damagePool;
            _damageDealtPub = damageDealtPub;
            _heroStatsChangedSub = heroStatsChangedSub;

            _statsSubscription = _heroStatsChangedSub.Subscribe(OnHeroStatsChanged);
        }

        private void Update()
        {
            TryInitialize();
        }

        private void LateUpdate()
        {
            if (!IsReady) return;
            ProcessDamageEvents();
        }

        private void TryInitialize()
        {
            if (IsReady) return;
            if (_gameState?.Hero == null) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return;

            _entityManager = world.EntityManager;

            _aliveEnemyQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<DeadTag>()
            );

            _damageBufferSystem = world.GetExistingSystemManaged<DamageEventBufferSystem>();

            SpawnHeroEntity();
            IsReady = true;

            Debug.Log("[CombatBridge] ECS bridge initialized.");
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

        public void SpawnWave(WaveDefinition wave, float tierScaling)
        {
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
                    xSpread += Random.Range(-0.3f, 0.3f);
                    float ySpawn = Random.Range(5.5f, 7f);

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

            Debug.Log($"[CombatBridge] Wave spawned ({wave.Spawns.Count} groups).");
        }

        public int GetAliveEnemyCount() => _aliveEnemyQuery.CalculateEntityCount();

        private void OnHeroStatsChanged(HeroStatsChangedDTO dto)
        {
            if (!IsReady || !_entityManager.Exists(_heroEntity)) return;

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
            if (IsReady)
                _aliveEnemyQuery.Dispose();
        }
    }
}
