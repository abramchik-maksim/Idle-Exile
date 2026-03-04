using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    public struct HeroSlashFX : IComponentData
    {
        public float2 Origin;
        public float2 Direction;
        public float Length;
        public float Timer;
        public float Duration;
    }
}
