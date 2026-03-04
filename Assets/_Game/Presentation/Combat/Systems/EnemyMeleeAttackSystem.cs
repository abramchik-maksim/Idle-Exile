using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial class EnemyMeleeAttackSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnemyTag>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (pos, behavior, target, cooldown, status, entity)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<EnemyBehavior>, RefRO<TargetEntity>,
                    RefRW<AttackCooldown>, RefRO<StatusEffects>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag, MeleeWindUp>()
                    .WithEntityAccess())
            {
                if (behavior.ValueRO.Archetype != EnemyArchetype.Melee)
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

                float2 dir = math.normalizesafe(targetPos - pos.ValueRO.Value, new float2(0, -1));

                ecb.AddComponent(entity, new MeleeWindUp
                {
                    Duration = 0.5f,
                    Timer = 0f,
                    AoERadius = behavior.ValueRO.AttackRange,
                    AoEDirection = dir,
                    ConeHalfAngle = 37.5f * math.PI / 180f
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
