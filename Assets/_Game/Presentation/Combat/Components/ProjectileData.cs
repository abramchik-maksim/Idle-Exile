using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Components
{
    public struct ProjectileData : IComponentData
    {
        /// <summary>World-space velocity (units per second). Magnitude is shot speed.</summary>
        public float2 Velocity;

        /// <summary>Position at start of current frame (before integration); used for swept hits.</summary>
        public float2 PrevPosition;

        public ProjectileMotionMode MotionMode;

        /// <summary>
        /// <see cref="ProjectileMotionMode.HomingChain"/>: enemy to steer toward.
        /// <see cref="ProjectileMotionMode.GuidedHoming"/>: target to steer toward (e.g. hero).
        /// Otherwise <see cref="Entity.Null"/>.
        /// </summary>
        public Entity MotionTarget;

        public float TimeToLiveSeconds;

        /// <summary>First spawn position (hero shot or fork child); used to ignore baked walls still inside the grace sphere around the shot.</summary>
        public float2 SpawnOrigin;

        /// <summary>
        /// 1 = ignore wall hits whose hit-point is within
        /// <see cref="Game.Domain.Combat.ProjectileConstants.HeroProjectileWallHitIgnoreRadiusFromSpawn"/> of <see cref="SpawnOrigin"/>.
        /// Enabled only for the hero's primary shot (avoids self-eating walls baked at the hero's feet).
        /// Disabled for fork children and chain hops since those originate mid-arena and must collide normally.
        /// </summary>
        public byte WallHitNearSpawnIgnoreEnabled;

        /// <summary>Seconds since spawn; incremented for hero projectiles in ProjectileMovementSystem.</summary>
        public float AgeSeconds;

        public float Damage;
        public bool IsCritical;
        public int VisualId;
        public float IgnoreArmorChance;
        public float LifeLeech;
        public int ProjectileLineageId;
        public Entity LastHitTarget;

        public int ForkGuaranteedLeft;
        public float ForkRemainderChance;
        public int PierceGuaranteedLeft;
        public float PierceRemainderChance;
        public int ChainGuaranteedLeft;
        public float ChainRemainderChance;
    }
}
