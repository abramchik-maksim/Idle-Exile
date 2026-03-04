using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial class EnemyCastSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<CastState>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (pos, behavior, target, castState, cooldown, stats, status)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<EnemyBehavior>, RefRO<TargetEntity>,
                    RefRW<CastState>, RefRW<AttackCooldown>, RefRO<CombatStats>, RefRO<StatusEffects>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                if (behavior.ValueRO.Archetype != EnemyArchetype.Caster)
                    continue;

                if (status.ValueRO.IsIncapacitated || status.ValueRO.Has(StatusEffectType.Silence))
                {
                    if (castState.ValueRO.IsCasting != 0)
                    {
                        castState.ValueRW.IsCasting = 0;
                        castState.ValueRW.CastTimer = 0f;
                    }
                    continue;
                }

                var targetEntity = target.ValueRO.Value;
                if (targetEntity == Entity.Null || !EntityManager.Exists(targetEntity))
                {
                    castState.ValueRW.IsCasting = 0;
                    castState.ValueRW.CastTimer = 0f;
                    continue;
                }

                var targetPos = EntityManager.GetComponentData<Position2D>(targetEntity).Value;
                float dist = math.distance(pos.ValueRO.Value, targetPos);

                if (dist > behavior.ValueRO.AttackRange)
                {
                    castState.ValueRW.IsCasting = 0;
                    castState.ValueRW.CastTimer = 0f;
                    continue;
                }

                if (castState.ValueRO.IsCasting == 0)
                {
                    cooldown.ValueRW.Timer -= dt;
                    if (cooldown.ValueRW.Timer > 0f)
                        continue;

                    castState.ValueRW.IsCasting = 1;
                    castState.ValueRW.CastTimer = 0f;
                    continue;
                }

                castState.ValueRW.CastTimer += dt;

                if (castState.ValueRW.CastTimer < castState.ValueRO.CastDuration)
                    continue;

                castState.ValueRW.IsCasting = 0;
                castState.ValueRW.CastTimer = 0f;
                cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                var spellEntity = EntityManager.CreateEntity(
                    typeof(SpellAoE),
                    typeof(Position2D)
                );

                EntityManager.SetComponentData(spellEntity, new Position2D { Value = targetPos });
                EntityManager.SetComponentData(spellEntity, new SpellAoE
                {
                    Center = targetPos,
                    Radius = castState.ValueRO.AoERadius,
                    Damage = stats.ValueRO.PhysicalDamage * castState.ValueRO.DamageMultiplier,
                    Delay = castState.ValueRO.DetonationDelay,
                    Timer = 0f,
                    HasDetonated = 0
                });
            }
        }
    }
}
