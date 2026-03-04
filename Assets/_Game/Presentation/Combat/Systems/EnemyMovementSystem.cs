using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(HeroAttackSystem))]
    public partial class EnemyMovementSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnemyTag>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (pos, stats, behavior, target, status)
                in SystemAPI.Query<RefRW<Position2D>, RefRO<CombatStats>, RefRO<EnemyBehavior>,
                    RefRO<TargetEntity>, RefRO<StatusEffects>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (status.ValueRO.IsIncapacitated)
                    continue;

                var targetEntity = target.ValueRO.Value;
                if (targetEntity == Entity.Null || !EntityManager.Exists(targetEntity))
                    continue;

                var targetPos = EntityManager.GetComponentData<Position2D>(targetEntity).Value;
                float2 diff = targetPos - pos.ValueRO.Value;
                float dist = math.length(diff);

                if (dist <= behavior.ValueRO.AttackRange)
                    continue;

                float2 direction = math.normalize(diff);

                float slowFactor = status.ValueRO.Has(StatusEffectType.Slow)
                    ? math.max(status.ValueRO.SlowFactor, 0.1f)
                    : 1f;

                pos.ValueRW.Value += direction * stats.ValueRO.MoveSpeed * slowFactor * dt;
            }
        }
    }
}
