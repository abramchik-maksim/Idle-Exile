using Game.Domain.Combat;
using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct StatusEffects : IComponentData
    {
        public StatusEffectType Flags;
        public float SilenceTimer;
        public float StunTimer;
        public float SlowFactor;
        public float SlowTimer;
        public float FreezeTimer;

        public bool Has(StatusEffectType flag) => (Flags & flag) != 0;
        public bool IsIncapacitated => Has(StatusEffectType.Stun) || Has(StatusEffectType.Frozen);
    }
}
