using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct Targetable : IComponentData
    {
        public float AggroWeight;
    }
}
