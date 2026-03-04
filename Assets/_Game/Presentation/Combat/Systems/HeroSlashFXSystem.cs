using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SlashFXSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (fx, entity) in SystemAPI.Query<RefRW<HeroSlashFX>>().WithEntityAccess())
            {
                fx.ValueRW.Timer += dt;
                if (fx.ValueRO.Timer >= fx.ValueRO.Duration)
                    ecb.DestroyEntity(entity);
            }

            foreach (var (fx, entity) in SystemAPI.Query<RefRW<EnemySlashFX>>().WithEntityAccess())
            {
                fx.ValueRW.Timer += dt;
                if (fx.ValueRO.Timer >= fx.ValueRO.Duration)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
