using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct TargetEntity : IComponentData
    {
        public Entity Value;
    }
}
