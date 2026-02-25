using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(HeroAttackSystem))]
    public partial class EnemyMovementSystem : SystemBase
    {
        private EntityQuery _heroQuery;

        protected override void OnCreate()
        {
            _heroQuery = GetEntityQuery(
                ComponentType.ReadOnly<HeroTag>(),
                ComponentType.ReadOnly<Position2D>()
            );
            RequireForUpdate<EnemyTag>();
        }

        protected override void OnUpdate()
        {
            if (_heroQuery.IsEmpty) return;

            var heroPositions = _heroQuery.ToComponentDataArray<Position2D>(Unity.Collections.Allocator.Temp);
            float2 heroPos = heroPositions[0].Value;
            heroPositions.Dispose();

            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (pos, stats)
                in SystemAPI.Query<RefRW<Position2D>, RefRO<CombatStats>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                float2 diff = heroPos - pos.ValueRO.Value;
                float dist = math.length(diff);

                // Stop when close to hero (melee range)
                if (dist < 1.0f) continue;

                float2 direction = math.normalize(diff);
                pos.ValueRW.Value += direction * stats.ValueRO.MoveSpeed * dt;
            }
        }
    }
}
