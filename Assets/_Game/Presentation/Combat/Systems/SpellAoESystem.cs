using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyCastSystem))]
    public partial class SpellAoESystem : SystemBase
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
            RequireForUpdate<SpellAoE>();
        }

        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var tEntities = _targetableQuery.ToEntityArray(Allocator.Temp);
            var tPositions = _targetableQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var tStats = _targetableQuery.ToComponentDataArray<CombatStats>(Allocator.Temp);

            foreach (var (spell, entity)
                in SystemAPI.Query<RefRW<SpellAoE>>()
                    .WithEntityAccess())
            {
                spell.ValueRW.Timer += dt;

                if (spell.ValueRO.Timer < spell.ValueRO.Delay)
                    continue;

                if (spell.ValueRO.HasDetonated != 0)
                {
                    if (spell.ValueRO.Timer > spell.ValueRO.Delay + 0.3f)
                        ecb.DestroyEntity(entity);
                    continue;
                }

                spell.ValueRW.HasDetonated = 1;

                float2 center = spell.ValueRO.Center;
                float radius = spell.ValueRO.Radius;
                float damage = spell.ValueRO.Damage;

                for (int i = 0; i < tEntities.Length; i++)
                {
                    float dist = math.distance(center, tPositions[i].Value);
                    if (dist > radius)
                        continue;

                    var stats = tStats[i];
                    float finalDmg = DamageCalculator.ApplyArmorReduction(damage, stats.Armor);

                    stats.CurrentHealth -= finalDmg;
                    EntityManager.SetComponentData(tEntities[i], stats);

                    bool isHero = EntityManager.HasComponent<HeroTag>(tEntities[i]);
                    if (stats.CurrentHealth <= 0f && !isHero
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
                        TargetActorId = actorId,
                        IsFromHero = false,
                        DamageCategory = 0
                    });
                }
            }

            tEntities.Dispose();
            tPositions.Dispose();
            tStats.Dispose();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
