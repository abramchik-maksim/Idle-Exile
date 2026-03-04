using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial class EnemyRangedAttackSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnemyTag>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (pos, behavior, target, cooldown, stats, status)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<EnemyBehavior>, RefRO<TargetEntity>,
                    RefRW<AttackCooldown>, RefRO<CombatStats>, RefRO<StatusEffects>>()
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

                var proj = EntityManager.CreateEntity(
                    typeof(EnemyProjectileTag),
                    typeof(Position2D),
                    typeof(ProjectileData)
                );

                EntityManager.SetComponentData(proj, new Position2D { Value = pos.ValueRO.Value });
                EntityManager.SetComponentData(proj, new ProjectileData
                {
                    Target = targetEntity,
                    Speed = 8f,
                    Damage = stats.ValueRO.PhysicalDamage,
                    IsCritical = false
                });
            }
        }
    }
}
