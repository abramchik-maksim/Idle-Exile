using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMeleeAttackSystem))]
    public partial class MeleeWindUpSystem : SystemBase
    {
        private EntityQuery _targetableQuery;
        private DamageEventBufferSystem _damageBuffer;

        protected override void OnCreate()
        {
            _targetableQuery = GetEntityQuery(
                ComponentType.ReadOnly<Targetable>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.ReadOnly<CombatStats>()
            );
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
            RequireForUpdate<MeleeWindUp>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var tEntities = _targetableQuery.ToEntityArray(Allocator.Temp);
            var tPositions = _targetableQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var tStats = _targetableQuery.ToComponentDataArray<CombatStats>(Allocator.Temp);

            foreach (var (windUp, pos, stats, entity)
                in SystemAPI.Query<RefRW<MeleeWindUp>, RefRO<Position2D>, RefRO<CombatStats>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                windUp.ValueRW.Timer += dt;

                if (windUp.ValueRW.Timer < windUp.ValueRO.Duration)
                    continue;

                float2 attackerPos = pos.ValueRO.Value;
                float aoERadius = windUp.ValueRO.AoERadius;
                float damage = stats.ValueRO.PhysicalDamage;
                float2 attackDir = windUp.ValueRO.AoEDirection;
                float coneHalfAngle = windUp.ValueRO.ConeHalfAngle;

                bool hitAnyone = false;

                for (int i = 0; i < tEntities.Length; i++)
                {
                    float dist = math.distance(attackerPos, tPositions[i].Value);
                    if (dist > aoERadius)
                        continue;

                    if (dist > 0.01f)
                    {
                        float2 toTarget = math.normalize(tPositions[i].Value - attackerPos);
                        float angle = math.acos(math.clamp(math.dot(attackDir, toTarget), -1f, 1f));
                        if (angle > coneHalfAngle)
                            continue;
                    }

                    var targetStats = tStats[i];
                    float armor = targetStats.Armor;
                    float rawDmg = damage;
                    float reduction = armor / (armor + 10f * rawDmg);
                    float finalDmg = rawDmg * (1f - reduction);
                    finalDmg = math.max(finalDmg, 0f);

                    targetStats.CurrentHealth -= finalDmg;

                    bool isHero = EntityManager.HasComponent<HeroTag>(tEntities[i]);
                    EntityManager.SetComponentData(tEntities[i], targetStats);

                    if (targetStats.CurrentHealth <= 0f && !isHero
                        && !EntityManager.HasComponent<DeadTag>(tEntities[i]))
                        ecb.AddComponent<DeadTag>(tEntities[i]);

                    int actorId = EntityManager.HasComponent<ActorId>(tEntities[i])
                        ? EntityManager.GetComponentData<ActorId>(tEntities[i]).Value
                        : -1;

                    _damageBuffer.EventQueue.Enqueue(new DamageEvent
                    {
                        Amount = finalDmg,
                        WorldX = tPositions[i].Value.x,
                        WorldY = tPositions[i].Value.y,
                        IsCritical = false,
                        TargetActorId = actorId
                    });

                    if (!hitAnyone)
                    {
                        hitAnyone = true;
                        float slashLen = math.max(dist, 0.3f);
                        float2 slashDir = dist > 0.01f
                            ? math.normalize(tPositions[i].Value - attackerPos)
                            : attackDir;
                        var fx = ecb.CreateEntity();
                        ecb.AddComponent(fx, new EnemySlashFX
                        {
                            Origin = attackerPos,
                            Direction = slashDir,
                            Length = slashLen,
                            Timer = 0f,
                            Duration = 0.15f
                        });
                    }
                }

                ecb.RemoveComponent<MeleeWindUp>(entity);
            }

            tEntities.Dispose();
            tPositions.Dispose();
            tStats.Dispose();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
