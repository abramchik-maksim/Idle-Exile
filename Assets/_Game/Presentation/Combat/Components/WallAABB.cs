using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    public struct WallAABB : IComponentData
    {
        public float2 HalfExtents;
    }
}
