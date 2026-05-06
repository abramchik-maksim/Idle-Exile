using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyRangedAttackSystem))]
    public partial class EnemyProjectileMovementSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnemyProjectileTag>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            float maxTurn = ProjectileConstants.ChainTurnRateRadPerSec * dt;

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRW<Position2D>, RefRW<ProjectileData>>()
                    .WithAll<EnemyProjectileTag>()
                    .WithEntityAccess())
            {
                proj.ValueRW.PrevPosition = pos.ValueRO.Value;

                Entity mt = proj.ValueRO.MotionTarget;
                if (mt == Entity.Null || !EntityManager.Exists(mt) || EntityManager.HasComponent<DeadTag>(mt))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float speed = math.length(proj.ValueRO.Velocity);
                float2 targetPos = EntityManager.GetComponentData<Position2D>(mt).Value;
                float2 desired = math.normalizesafe(targetPos - pos.ValueRO.Value, new float2(0f, 1f));
                float2 curDir = math.normalizesafe(proj.ValueRO.Velocity, desired);
                float2 newDir = ProjectileHitMath.SlerpDir2D(curDir, desired, maxTurn);
                proj.ValueRW.Velocity = newDir * speed;

                pos.ValueRW.Value += proj.ValueRO.Velocity * dt;
                proj.ValueRW.TimeToLiveSeconds -= dt;

                if (proj.ValueRO.TimeToLiveSeconds <= 0f)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
