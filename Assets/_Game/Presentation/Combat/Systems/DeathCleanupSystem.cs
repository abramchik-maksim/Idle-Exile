using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class DeathCleanupSystem : SystemBase
    {
        private EntityQuery _aliveEnemyQuery;
        private uint _rngState;

        protected override void OnCreate()
        {
            RequireForUpdate<DeadTag>();
            _aliveEnemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.ReadWrite<AilmentState>(),
                ComponentType.Exclude<DeadTag>()
            );
            _rngState = (uint)System.Environment.TickCount + 13u;
        }

        protected override void OnUpdate()
        {
            HeroSkillAffixData affixData = default;
            bool hasAoE = false;
            foreach (var affix in SystemAPI.Query<RefRO<HeroSkillAffixData>>().WithAll<HeroTag>())
            {
                affixData = affix.ValueRO;
                hasAoE = affixData.AoEAilmentChance > 0f && affixData.AoEAilmentRadius > 0f;
                break;
            }

            var deadPositions = new NativeList<float2>(4, Allocator.Temp);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (pos, entity)
                in SystemAPI.Query<RefRO<Position2D>>()
                    .WithAll<DeadTag, EnemyTag>()
                    .WithEntityAccess())
            {
                if (hasAoE)
                    deadPositions.Add(pos.ValueRO.Value);

                ecb.DestroyEntity(entity);
            }

            foreach (var (_, entity)
                in SystemAPI.Query<RefRO<DeadTag>>()
                    .WithAll<CloneTag>()
                    .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();

            if (hasAoE && deadPositions.Length > 0)
                ApplyAoEAilmentsOnKill(deadPositions, affixData);

            deadPositions.Dispose();
        }

        private void ApplyAoEAilmentsOnKill(NativeList<float2> deathPositions, HeroSkillAffixData affixData)
        {
            var aliveEntities = _aliveEnemyQuery.ToEntityArray(Allocator.Temp);
            var alivePositions = _aliveEnemyQuery.ToComponentDataArray<Position2D>(Allocator.Temp);

            float radiusSq = affixData.AoEAilmentRadius * affixData.AoEAilmentRadius;

            for (int d = 0; d < deathPositions.Length; d++)
            {
                if (NextRandom() > affixData.AoEAilmentChance) continue;

                for (int e = 0; e < aliveEntities.Length; e++)
                {
                    if (!EntityManager.Exists(aliveEntities[e])) continue;
                    float distSq = math.distancesq(deathPositions[d], alivePositions[e].Value);
                    if (distSq > radiusSq) continue;

                    if (!EntityManager.HasComponent<AilmentState>(aliveEntities[e])) continue;

                    var ailment = EntityManager.GetComponentData<AilmentState>(aliveEntities[e]);

                    switch (affixData.AoEAilmentType)
                    {
                        case AilmentType.Ignite:
                            ailment.IgniteDamagePerTick = AilmentCalculator.GetIgniteDamagePerTick(10f);
                            ailment.IgniteTimer = AilmentCalculator.IgniteDuration;
                            break;
                        case AilmentType.Chill:
                            ailment.ChillStacks = math.min(ailment.ChillStacks + 1, AilmentCalculator.MaxChillStacks);
                            break;
                        case AilmentType.Shock:
                            ailment.ShockStacks = math.min(ailment.ShockStacks + 1, AilmentCalculator.MaxShockStacks);
                            break;
                        case AilmentType.Bleed:
                            float dpt = AilmentCalculator.GetBleedDamagePerTick(10f);
                            var buffer = EntityManager.GetBuffer<BleedStack>(aliveEntities[e]);
                            buffer.Add(new BleedStack { DamagePerTick = dpt, RemainingDuration = AilmentCalculator.BleedDuration });
                            break;
                    }

                    EntityManager.SetComponentData(aliveEntities[e], ailment);
                }
            }

            aliveEntities.Dispose();
            alivePositions.Dispose();
        }

        private float NextRandom()
        {
            _rngState = _rngState * 747796405u + 2891336453u;
            uint result = ((_rngState >> (int)((_rngState >> 28) + 4u)) ^ _rngState) * 277803737u;
            result = (result >> 22) ^ result;
            return result / (float)uint.MaxValue;
        }
    }
}
