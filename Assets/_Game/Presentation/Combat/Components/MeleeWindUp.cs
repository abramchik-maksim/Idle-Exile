using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    public struct MeleeWindUp : IComponentData
    {
        public float Duration;
        public float Timer;
        public float AoERadius;
        public float2 AoEDirection;
        public float ConeHalfAngle;
    }
}
