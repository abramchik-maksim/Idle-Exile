using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HeroAttackSystem))]
    public partial class ProjectileMovementSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<ProjectileTag>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            float maxTurn = ProjectileConstants.ChainTurnRateRadPerSec * dt;

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRW<Position2D>, RefRW<ProjectileData>>()
                    .WithAll<ProjectileTag>()
                    .WithEntityAccess())
            {
                if (proj.ValueRO.MotionMode == ProjectileMotionMode.StuckInWall)
                {
                    proj.ValueRW.TimeToLiveSeconds -= dt;
                    proj.ValueRW.AgeSeconds += dt;

                    if (proj.ValueRO.TimeToLiveSeconds <= 0f)
                    {
                        ProjectileLifetimeDebug.LogHeroDespawn(
                            entity,
                            "TimeToLiveExpired_StuckInWall",
                            pos.ValueRO.Value,
                            float2.zero,
                            proj.ValueRO.TimeToLiveSeconds,
                            proj.ValueRO.MotionMode,
                            $"dt={dt:F4}");
                        ecb.DestroyEntity(entity);
                    }

                    continue;
                }

                proj.ValueRW.PrevPosition = pos.ValueRO.Value;

                float speed = math.length(proj.ValueRO.Velocity);

                if (proj.ValueRO.MotionMode == ProjectileMotionMode.HomingChain)
                {
                    Entity mt = proj.ValueRO.MotionTarget;
                    if (mt == Entity.Null || !EntityManager.Exists(mt) || EntityManager.HasComponent<DeadTag>(mt))
                    {
                        proj.ValueRW.MotionMode = ProjectileMotionMode.Straight;
                        proj.ValueRW.MotionTarget = Entity.Null;
                    }
                    else
                    {
                        float2 targetPos = EntityManager.GetComponentData<Position2D>(mt).Value;
                        float2 desired = math.normalizesafe(targetPos - pos.ValueRO.Value, new float2(0f, 1f));
                        float2 curDir = math.normalizesafe(proj.ValueRO.Velocity, desired);
                        float2 newDir = ProjectileHitMath.SlerpDir2D(curDir, desired, maxTurn);
                        proj.ValueRW.Velocity = newDir * speed;
                    }
                }

                pos.ValueRW.Value += proj.ValueRO.Velocity * dt;
                proj.ValueRW.TimeToLiveSeconds -= dt;
                proj.ValueRW.AgeSeconds += dt;

                if (proj.ValueRO.TimeToLiveSeconds <= 0f)
                {
                    ProjectileLifetimeDebug.LogHeroDespawn(
                        entity,
                        "TimeToLiveExpired",
                        pos.ValueRO.Value,
                        proj.ValueRO.Velocity,
                        proj.ValueRO.TimeToLiveSeconds,
                        proj.ValueRO.MotionMode,
                        $"dt={dt:F4}");
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
