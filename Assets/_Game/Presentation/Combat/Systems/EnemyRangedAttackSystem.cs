using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial class EnemyRangedAttackSystem : SystemBase
    {
        private struct PendingProjectile
        {
            public float2 Origin;
            public Entity Target;
            public float Damage;
            public int VisualId;
        }

        protected override void OnCreate()
        {
            RequireForUpdate<EnemyTag>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var pending = new NativeList<PendingProjectile>(4, Allocator.Temp);

            foreach (var (pos, behavior, target, cooldown, stats, status, projVisId)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<EnemyBehavior>, RefRO<TargetEntity>,
                    RefRW<AttackCooldown>, RefRO<CombatStats>, RefRO<StatusEffects>, RefRO<ProjectileVisualId>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (behavior.ValueRO.Archetype != EnemyArchetype.Ranged)
                    continue;

                if (status.ValueRO.IsIncapacitated)
                    continue;

                var targetEntity = target.ValueRO.Value;
                if (targetEntity == Entity.Null || !EntityManager.Exists(targetEntity))
                    continue;

                var targetPos = EntityManager.GetComponentData<Position2D>(targetEntity).Value;
                float dist = math.distance(pos.ValueRO.Value, targetPos);

                if (dist > behavior.ValueRO.AttackRange)
                    continue;

                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRW.Timer > 0f)
                    continue;

                cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                pending.Add(new PendingProjectile
                {
                    Origin = pos.ValueRO.Value,
                    Target = targetEntity,
                    Damage = stats.ValueRO.PhysicalDamage,
                    VisualId = projVisId.ValueRO.Value
                });
            }

            if (pending.Length > 0)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                for (int i = 0; i < pending.Length; i++)
                {
                    var p = pending[i];
                    var proj = ecb.CreateEntity();
                    ecb.AddComponent(proj, new EnemyProjectileTag());
                    ecb.AddComponent(proj, new Position2D { Value = p.Origin });
                    float2 targetPos = EntityManager.GetComponentData<Position2D>(p.Target).Value;
                    float2 dir = math.normalizesafe(targetPos - p.Origin, new float2(0f, 1f));
                    float speed = ProjectileConstants.DefaultEnemyProjectileSpeed;
                    ecb.AddComponent(proj, new ProjectileData
                    {
                        Velocity = dir * speed,
                        PrevPosition = p.Origin,
                        MotionMode = ProjectileMotionMode.GuidedHoming,
                        MotionTarget = p.Target,
                        TimeToLiveSeconds = ProjectileConstants.EnemyProjectileMaxLifetimeSeconds,
                        SpawnOrigin = p.Origin,
                        AgeSeconds = 0f,
                        Damage = p.Damage,
                        IsCritical = false,
                        VisualId = p.VisualId
                    });
                }

                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            pending.Dispose();
        }
    }
}
