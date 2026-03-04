using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(HeroAttackSystem))]
    public partial class HeroMovementSystem : SystemBase
    {
        private EntityQuery _enemyQuery;

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            RequireForUpdate<HeroTag>();
            RequireForUpdate<HeroAttackRange>();
        }

        protected override void OnUpdate()
        {
            if (_enemyQuery.IsEmpty) return;

            float dt = SystemAPI.Time.DeltaTime;
            var enemyPositions = _enemyQuery.ToComponentDataArray<Position2D>(Allocator.Temp);

            foreach (var (pos, stats, range, status)
                in SystemAPI.Query<RefRW<Position2D>, RefRO<CombatStats>, RefRO<HeroAttackRange>, RefRO<StatusEffects>>()
                    .WithAll<HeroTag, AttackEnabled>())
            {
                if (status.ValueRO.IsIncapacitated)
                    continue;

                if (range.ValueRO.IsMelee == 0)
                    continue;

                float nearestDistSq = float.MaxValue;
                int nearestIdx = -1;

                for (int i = 0; i < enemyPositions.Length; i++)
                {
                    float distSq = math.distancesq(pos.ValueRO.Value, enemyPositions[i].Value);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestIdx = i;
                    }
                }

                if (nearestIdx < 0) continue;

                float dist = math.sqrt(nearestDistSq);
                if (dist <= range.ValueRO.Value)
                    continue;

                float2 dir = math.normalize(enemyPositions[nearestIdx].Value - pos.ValueRO.Value);

                float slowFactor = status.ValueRO.Has(Game.Domain.Combat.StatusEffectType.Slow)
                    ? math.max(status.ValueRO.SlowFactor, 0.1f)
                    : 1f;

                pos.ValueRW.Value += dir * stats.ValueRO.MoveSpeed * slowFactor * dt;
            }

            enemyPositions.Dispose();
        }
    }
}
