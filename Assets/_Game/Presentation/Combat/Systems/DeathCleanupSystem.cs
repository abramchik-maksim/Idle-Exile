using Game.Presentation.Combat.Components;
using Unity.Entities;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class DeathCleanupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<DeadTag>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (_, entity)
                in SystemAPI.Query<RefRO<DeadTag>>()
                    .WithAll<EnemyTag>()
                    .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
