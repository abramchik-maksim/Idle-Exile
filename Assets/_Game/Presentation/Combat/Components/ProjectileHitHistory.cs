using Unity.Entities;

namespace Game.Presentation.Combat.Components
{
    /// <summary>
    /// Stores targets already hit by this projectile lineage.
    /// Used to prevent re-hitting the same target during chain/fork/pierce routing.
    /// </summary>
    public struct ProjectileHitHistory : IBufferElementData
    {
        public Entity Target;
    }
}
