using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AilmentTickSystem))]
    [UpdateBefore(typeof(EnemyMovementSystem))]
    public partial class EnemyTargetSelectionSystem : SystemBase
    {
        private EntityQuery _targetableQuery;

        protected override void OnCreate()
        {
            _targetableQuery = GetEntityQuery(
                ComponentType.ReadOnly<Targetable>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            RequireForUpdate<EnemyTag>();
            RequireForUpdate<Targetable>();
        }

        protected override void OnUpdate()
        {
            var targetEntities = _targetableQuery.ToEntityArray(Allocator.Temp);
            var targetPositions = _targetableQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var targetWeights = _targetableQuery.ToComponentDataArray<Targetable>(Allocator.Temp);

            int targetCount = targetEntities.Length;
            if (targetCount == 0)
            {
                targetEntities.Dispose();
                targetPositions.Dispose();
                targetWeights.Dispose();
                return;
            }

            foreach (var (pos, target)
                in SystemAPI.Query<RefRO<Position2D>, RefRW<TargetEntity>>()
                    .WithAll<EnemyTag>()
                    .WithNone<DeadTag>())
            {
                Entity best = Entity.Null;
                float bestScore = -1f;

                for (int i = 0; i < targetCount; i++)
                {
                    float dist = math.distance(pos.ValueRO.Value, targetPositions[i].Value);
                    float score = targetWeights[i].AggroWeight / math.max(dist, 0.01f);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = targetEntities[i];
                    }
                }

                target.ValueRW.Value = best;
            }

            targetEntities.Dispose();
            targetPositions.Dispose();
            targetWeights.Dispose();
        }
    }
}
