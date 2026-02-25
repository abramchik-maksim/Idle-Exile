using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    public struct Position2D : IComponentData
    {
        public float2 Value;
    }
}
