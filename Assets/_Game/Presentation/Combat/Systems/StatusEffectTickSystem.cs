using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BleedStackTickSystem))]
    public partial class StatusEffectTickSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var status
                in SystemAPI.Query<RefRW<StatusEffects>>()
                    .WithNone<DeadTag>())
            {
                var flags = status.ValueRO.Flags;

                if ((flags & StatusEffectType.Silence) != 0)
                {
                    status.ValueRW.SilenceTimer -= dt;
                    if (status.ValueRO.SilenceTimer <= 0f)
                    {
                        status.ValueRW.SilenceTimer = 0f;
                        status.ValueRW.Flags &= ~StatusEffectType.Silence;
                    }
                }

                if ((flags & StatusEffectType.Stun) != 0)
                {
                    status.ValueRW.StunTimer -= dt;
                    if (status.ValueRO.StunTimer <= 0f)
                    {
                        status.ValueRW.StunTimer = 0f;
                        status.ValueRW.Flags &= ~StatusEffectType.Stun;
                    }
                }

                if ((flags & StatusEffectType.Slow) != 0)
                {
                    status.ValueRW.SlowTimer -= dt;
                    if (status.ValueRO.SlowTimer <= 0f)
                    {
                        status.ValueRW.SlowTimer = 0f;
                        status.ValueRW.SlowFactor = 0f;
                        status.ValueRW.Flags &= ~StatusEffectType.Slow;
                    }
                }

                if ((flags & StatusEffectType.Frozen) != 0)
                {
                    status.ValueRW.FreezeTimer -= dt;
                    if (status.ValueRO.FreezeTimer <= 0f)
                    {
                        status.ValueRW.FreezeTimer = 0f;
                        status.ValueRW.Flags &= ~StatusEffectType.Frozen;
                    }
                }

                if ((flags & StatusEffectType.KnockedBack) != 0)
                {
                    status.ValueRW.StunTimer -= dt;
                    if (status.ValueRO.StunTimer <= 0f)
                    {
                        status.ValueRW.StunTimer = 0f;
                        status.ValueRW.Flags &= ~StatusEffectType.KnockedBack;
                    }
                }
            }
        }
    }
}
