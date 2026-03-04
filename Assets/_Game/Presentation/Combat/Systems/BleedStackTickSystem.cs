using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(AilmentTickSystem))]
    public partial class BleedStackTickSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (ailment, entity)
                in SystemAPI.Query<RefRW<AilmentState>>()
                    .WithAll<BleedStack>()
                    .WithNone<DeadTag>()
                    .WithEntityAccess())
            {
                var buffer = EntityManager.GetBuffer<BleedStack>(entity);
                float totalDps = 0f;

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var stack = buffer[i];
                    stack.RemainingDuration -= dt;

                    if (stack.RemainingDuration <= 0f)
                    {
                        buffer.RemoveAt(i);
                        continue;
                    }

                    buffer[i] = stack;
                    totalDps += stack.DamagePerTick / AilmentCalculator.AilmentTickInterval;
                }

                ailment.ValueRW.BleedTotalDps = totalDps;
            }
        }
    }
}
