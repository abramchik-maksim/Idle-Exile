using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(EnemyTargetSelectionSystem))]
    public partial class AilmentTickSystem : SystemBase
    {
        private DamageEventBufferSystem _damageBuffer;

        protected override void OnCreate()
        {
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (ailment, stats, status, pos, entity)
                in SystemAPI.Query<RefRW<AilmentState>, RefRW<CombatStats>, RefRW<StatusEffects>, RefRO<Position2D>>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                float totalDot = 0f;

                if (ailment.ValueRO.IgniteTimer > 0f)
                {
                    ailment.ValueRW.IgniteTimer -= dt;
                    totalDot += ailment.ValueRO.IgniteDamagePerTick * dt / AilmentCalculator.AilmentTickInterval;

                    if (ailment.ValueRO.IgniteTimer <= 0f)
                    {
                        ailment.ValueRW.IgniteTimer = 0f;
                        ailment.ValueRW.IgniteDamagePerTick = 0f;
                    }
                }

                totalDot += ailment.ValueRO.BleedTotalDps * dt;

                if (totalDot > 0f)
                {
                    stats.ValueRW.CurrentHealth -= totalDot;

                    bool isHero = EntityManager.HasComponent<HeroTag>(entity);
                    if (stats.ValueRO.CurrentHealth <= 0f && !isHero
                        && !EntityManager.HasComponent<DeadTag>(entity))
                        ecb.AddComponent<DeadTag>(entity);

                    int actorId = EntityManager.HasComponent<ActorId>(entity)
                        ? EntityManager.GetComponentData<ActorId>(entity).Value
                        : -1;

                    _damageBuffer.EventQueue.Enqueue(new DamageEvent
                    {
                        Amount = totalDot,
                        WorldX = pos.ValueRO.Value.x,
                        WorldY = pos.ValueRO.Value.y,
                        IsCritical = false,
                        TargetActorId = actorId
                    });
                }

                if (AilmentCalculator.ShouldFreeze(ailment.ValueRO.ChillStacks))
                {
                    ailment.ValueRW.ChillStacks = 0;

                    var st = status.ValueRO;
                    st.Flags |= StatusEffectType.Frozen;
                    st.FreezeTimer = AilmentCalculator.FreezeDuration;
                    status.ValueRW = st;
                }

                if (ailment.ValueRO.ChillStacks > 0)
                {
                    float slowFactor = AilmentCalculator.GetChillSlowFactor(ailment.ValueRO.ChillStacks);
                    var st = status.ValueRO;
                    st.Flags |= StatusEffectType.Slow;
                    st.SlowFactor = math.min(st.SlowFactor > 0f ? st.SlowFactor : 1f, slowFactor);
                    status.ValueRW = st;
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
