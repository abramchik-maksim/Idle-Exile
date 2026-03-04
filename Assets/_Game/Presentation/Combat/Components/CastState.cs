using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct CastState : IComponentData
    {
        public float CastDuration;
        public float CastTimer;
        public float DamageMultiplier;
        public float AoERadius;
        public float DetonationDelay;
        public byte IsCasting;
    }
}
