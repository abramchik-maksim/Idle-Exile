using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    public struct ProjectileData : IComponentData
    {
        public Entity Target;
        public float Speed;
        public float Damage;
        public bool IsCritical;
    }
}
