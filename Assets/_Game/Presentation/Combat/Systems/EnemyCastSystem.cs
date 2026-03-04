using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial class EnemyCastSystem : SystemBase
    {
        private struct PendingSpell
        {
            public float2 TargetPos;
            public float Radius;
            public float Damage;
            public float Delay;
        }

        protected override void OnCreate()
        {
            RequireForUpdate<CastState>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var pending = new NativeList<PendingSpell>(4, Allocator.Temp);

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

                pending.Add(new PendingSpell
                {
                    TargetPos = targetPos,
                    Radius = castState.ValueRO.AoERadius,
                    Damage = stats.ValueRO.PhysicalDamage * castState.ValueRO.DamageMultiplier,
                    Delay = castState.ValueRO.DetonationDelay
                });
            }

            if (pending.Length > 0)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                for (int i = 0; i < pending.Length; i++)
                {
                    var s = pending[i];
                    var spellEntity = ecb.CreateEntity();
                    ecb.AddComponent(spellEntity, new Position2D { Value = s.TargetPos });
                    ecb.AddComponent(spellEntity, new SpellAoE
                    {
                        Center = s.TargetPos,
                        Radius = s.Radius,
                        Damage = s.Damage,
                        Delay = s.Delay,
                        Timer = 0f,
                        HasDetonated = 0
                    });
                }

                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            pending.Dispose();
        }
    }
}
