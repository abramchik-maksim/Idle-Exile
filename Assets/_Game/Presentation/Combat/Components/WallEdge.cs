using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    /// <summary>Axis-aligned obstacle edge segment in world space (start/end).</summary>
    public struct WallEdge : IComponentData
    {
        public float2 A;
        public float2 B;
    }
}
