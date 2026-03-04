using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    public struct SpellAoE : IComponentData
    {
        public float2 Center;
        public float Radius;
        public float Damage;
        public float Delay;
        public float Timer;
        public byte HasDetonated;
    }
}
