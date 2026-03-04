using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HeroMovementSystem))]
    public partial class HeroAttackSystem : SystemBase
    {
        private EntityQuery _enemyQuery;
        private DamageEventBufferSystem _damageBuffer;

        private struct PendingMeleeHit
        {
            public Entity Target;
            public float Damage;
            public float2 TargetPos;
            public float2 HeroPos;
            public float Distance;
            public float AttackRange;
        }

        private struct PendingProjectile
        {
            public float2 HeroPos;
            public Entity Target;
            public float Damage;
        }

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
            RequireForUpdate<HeroTag>();
        }

        protected override void OnUpdate()
        {
            if (_enemyQuery.IsEmpty) return;

            float dt = SystemAPI.Time.DeltaTime;

            var enemies = _enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyPositions = _enemyQuery.ToComponentDataArray<Position2D>(Allocator.Temp);

            bool isMelee = false;
            float attackRange = 50f;

            foreach (var range in SystemAPI.Query<RefRO<HeroAttackRange>>().WithAll<HeroTag>())
            {
                isMelee = range.ValueRO.IsMelee != 0;
                attackRange = range.ValueRO.Value;
                break;
            }

            var meleeHits = new NativeList<PendingMeleeHit>(4, Allocator.Temp);
            var projectiles = new NativeList<PendingProjectile>(4, Allocator.Temp);

            foreach (var (cooldown, heroPos, stats)
                in SystemAPI.Query<RefRW<AttackCooldown>, RefRO<Position2D>, RefRO<CombatStats>>()
                    .WithAll<HeroTag, AttackEnabled>())
            {
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRW.Timer > 0f) continue;

                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;
                int nearestIdx = -1;

                for (int i = 0; i < enemies.Length; i++)
                {
                    float distSq = math.distancesq(heroPos.ValueRO.Value, enemyPositions[i].Value);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = enemies[i];
                        nearestIdx = i;
                    }
                }

                if (nearest == Entity.Null) continue;

                float dist = math.sqrt(nearestDistSq);

                if (isMelee)
                {
                    if (dist > attackRange) continue;

                    cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                    meleeHits.Add(new PendingMeleeHit
                    {
                        Target = nearest,
                        Damage = stats.ValueRO.PhysicalDamage,
                        TargetPos = enemyPositions[nearestIdx].Value,
                        HeroPos = heroPos.ValueRO.Value,
                        Distance = dist,
                        AttackRange = attackRange
                    });
                }
                else
                {
                    cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                    projectiles.Add(new PendingProjectile
                    {
                        HeroPos = heroPos.ValueRO.Value,
                        Target = nearest,
                        Damage = stats.ValueRO.PhysicalDamage
                    });
                }
            }

            enemies.Dispose();
            enemyPositions.Dispose();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < meleeHits.Length; i++)
            {
                var hit = meleeHits[i];

                var targetStats = EntityManager.GetComponentData<CombatStats>(hit.Target);
                float armor = targetStats.Armor;
                float reduction = armor / (armor + 10f * hit.Damage);
                float finalDmg = hit.Damage * (1f - reduction);
                finalDmg = math.max(finalDmg, 0f);

                targetStats.CurrentHealth -= finalDmg;
                EntityManager.SetComponentData(hit.Target, targetStats);

                if (targetStats.CurrentHealth <= 0f && !EntityManager.HasComponent<DeadTag>(hit.Target))
                    ecb.AddComponent<DeadTag>(hit.Target);

                int actorId = EntityManager.HasComponent<ActorId>(hit.Target)
                    ? EntityManager.GetComponentData<ActorId>(hit.Target).Value
                    : -1;

                _damageBuffer.EventQueue.Enqueue(new DamageEvent
                {
                    Amount = finalDmg,
                    WorldX = hit.TargetPos.x,
                    WorldY = hit.TargetPos.y,
                    IsCritical = false,
                    TargetActorId = actorId
                });

                float2 attackDir = math.normalizesafe(hit.TargetPos - hit.HeroPos, new float2(0, 1));

                var fx = ecb.CreateEntity();
                ecb.AddComponent(fx, new HeroSlashFX
                {
                    Origin = hit.HeroPos,
                    Direction = attackDir,
                    Length = math.min(hit.Distance, hit.AttackRange),
                    Timer = 0f,
                    Duration = 0.15f
                });
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                var p = projectiles[i];

                var proj = ecb.CreateEntity();
                ecb.AddComponent(proj, new ProjectileTag());
                ecb.AddComponent(proj, new Position2D { Value = p.HeroPos });
                ecb.AddComponent(proj, new ProjectileData
                {
                    Target = p.Target,
                    Speed = 12f,
                    Damage = p.Damage,
                    IsCritical = false
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            meleeHits.Dispose();
            projectiles.Dispose();
        }
    }
}
