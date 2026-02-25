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

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRW<Position2D>, RefRO<ProjectileData>>()
                    .WithAll<ProjectileTag>()
                    .WithEntityAccess())
            {
                if (!EntityManager.Exists(proj.ValueRO.Target)
                    || EntityManager.HasComponent<DeadTag>(proj.ValueRO.Target))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var targetPos = EntityManager.GetComponentData<Position2D>(proj.ValueRO.Target).Value;
                float2 direction = targetPos - pos.ValueRO.Value;
                float dist = math.length(direction);

                if (dist < 0.2f) continue;

                float2 move = math.normalize(direction) * proj.ValueRO.Speed * dt;

                if (math.length(move) >= dist)
                    pos.ValueRW.Value = targetPos;
                else
                    pos.ValueRW.Value += move;
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
