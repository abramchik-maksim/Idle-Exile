using System;

namespace Game.Domain.Combat
{
    [Flags]
    public enum StatusEffectType : byte
    {
        None = 0,
        Silence = 1,
        Stun = 2,
        Slow = 4,
        KnockedBack = 8,
        Frozen = 16
    }
}
