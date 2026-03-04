using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Application.Ports;
using Game.Domain.Combat;
using Game.Domain.Combat.Progression;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat
{
    public sealed class WaveSpawner
    {
        private readonly ICombatConfigProvider _combatConfig;
        private EntityManager _entityManager;
        private EntityQuery _aliveEnemyQuery;
        private int _nextActorId;
        private bool _initialized;

        public WaveSpawner(ICombatConfigProvider combatConfig)
        {
            _combatConfig = combatConfig;
        }

        public void Initialize(EntityManager entityManager, int startingActorId)
        {
            _entityManager = entityManager;
            _nextActorId = startingActorId;

            _aliveEnemyQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.Exclude<DeadTag>()
            );

            _initialized = true;
        }

        public int NextActorId => _nextActorId;

        public void SpawnWave(WaveDefinition wave, float tierScaling)
        {
            if (!_initialized) return;

            foreach (var spawn in wave.Spawns)
            {
                var enemyDef = _combatConfig.GetEnemy(spawn.EnemyDefinitionId);
                if (enemyDef == null)
                {
                    Debug.LogWarning($"[WaveSpawner] Enemy definition not found: {spawn.EnemyDefinitionId}");
                    continue;
                }

                for (int i = 0; i < spawn.Count; i++)
                {
                    float xSpread = (i - (spawn.Count - 1) / 2f) * 1.0f;
                    xSpread += UnityEngine.Random.Range(-0.3f, 0.3f);
                    float ySpawn = UnityEngine.Random.Range(5.5f, 7f);

                    var archetype = enemyDef.Archetype;
                    bool isCaster = archetype == EnemyArchetype.Caster;

                    var entity = _entityManager.CreateEntity(
                        typeof(EnemyTag),
                        typeof(Position2D),
                        typeof(CombatStats),
                        typeof(AttackCooldown),
                        typeof(ActorId),
                        typeof(EnemyBehavior),
                        typeof(TargetEntity),
                        typeof(StatusEffects),
                        typeof(AilmentState)
                    );

                    _entityManager.AddBuffer<BleedStack>(entity);

                    if (isCaster && enemyDef.Spell != null)
                        _entityManager.AddComponentData(entity, new CastState
                        {
                            CastDuration = enemyDef.Spell.CastDuration,
                            DamageMultiplier = enemyDef.Spell.DamageMultiplier,
                            AoERadius = enemyDef.Spell.AoERadius,
                            DetonationDelay = enemyDef.Spell.DetonationDelay
                        });

                    _entityManager.SetComponentData(entity, new Position2D
                    {
                        Value = new float2(xSpread, ySpawn)
                    });

                    float atkSpeed = enemyDef.AttackSpeed;
                    float atkCooldown = atkSpeed > 0f ? 1f / atkSpeed : 1f;

                    _entityManager.SetComponentData(entity, new CombatStats
                    {
                        MaxHealth = enemyDef.BaseHealth * tierScaling,
                        CurrentHealth = enemyDef.BaseHealth * tierScaling,
                        PhysicalDamage = enemyDef.BaseDamage * tierScaling,
                        AttackSpeed = atkSpeed,
                        Armor = enemyDef.BaseArmor * tierScaling,
                        MoveSpeed = enemyDef.BaseSpeed
                    });

                    _entityManager.SetComponentData(entity, new AttackCooldown
                    {
                        Cooldown = atkCooldown,
                        Timer = atkCooldown
                    });

                    _entityManager.SetComponentData(entity, new ActorId { Value = _nextActorId++ });

                    _entityManager.SetComponentData(entity, new EnemyBehavior
                    {
                        Archetype = archetype,
                        AttackRange = enemyDef.AttackRange
                    });
                }
            }

            Debug.Log($"[WaveSpawner] Wave spawned ({wave.Spawns.Count} groups).");
        }

        public void SpawnClone(Entity heroEntity, float damagePercent, float duration)
        {
            if (!_initialized || !_entityManager.Exists(heroEntity)) return;

            var heroPos = _entityManager.GetComponentData<Position2D>(heroEntity).Value;
            var heroStats = _entityManager.GetComponentData<CombatStats>(heroEntity);

            float2 offset = new float2(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(0.5f, 1.5f)
            );

            var clone = _entityManager.CreateEntity(
                typeof(CloneTag),
                typeof(Position2D),
                typeof(CombatStats),
                typeof(Targetable),
                typeof(ActorId),
                typeof(StatusEffects),
                typeof(AilmentState)
            );

            _entityManager.AddBuffer<BleedStack>(clone);

            _entityManager.SetComponentData(clone, new Position2D { Value = heroPos + offset });
            _entityManager.SetComponentData(clone, new CombatStats
            {
                MaxHealth = heroStats.MaxHealth * 0.3f,
                CurrentHealth = heroStats.MaxHealth * 0.3f,
                PhysicalDamage = heroStats.PhysicalDamage * (damagePercent / 100f),
                AttackSpeed = heroStats.AttackSpeed,
                Armor = 0f,
                MoveSpeed = heroStats.MoveSpeed
            });
            _entityManager.SetComponentData(clone, new Targetable { AggroWeight = 15f });
            _entityManager.SetComponentData(clone, new ActorId { Value = _nextActorId++ });

            Debug.Log($"[WaveSpawner] Clone spawned (Dmg: {damagePercent}%, Duration: {duration}s).");
        }

        public int GetAliveEnemyCount() => _initialized ? _aliveEnemyQuery.CalculateEntityCount() : 0;

        public void DespawnAllEnemies()
        {
            if (!_initialized) return;
            var enemies = _aliveEnemyQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!_entityManager.HasComponent<DeadTag>(enemies[i]))
                    _entityManager.AddComponent<DeadTag>(enemies[i]);
            }
            enemies.Dispose();
        }

        public void Dispose()
        {
            if (_initialized)
                _aliveEnemyQuery.Dispose();
        }
    }
}
