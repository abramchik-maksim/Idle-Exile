namespace Game.Presentation.Combat.Components
{
    public enum ProjectileMotionMode : byte
    {
        /// <summary>Constant <see cref="ProjectileData.Velocity"/>.</summary>
        Straight = 0,

        /// <summary>After chain proc: steer toward <see cref="ProjectileData.MotionTarget"/> (hero projectiles).</summary>
        HomingChain = 1,

        /// <summary>Enemy projectile: steer toward <see cref="ProjectileData.MotionTarget"/> (usually hero).</summary>
        GuidedHoming = 2,

        /// <summary>
        /// Hero projectile that hit a wall but did not chain off it. The projectile freezes in place
        /// (clamped to the wall hit point) and remains visible until <see cref="ProjectileData.TimeToLiveSeconds"/> expires.
        /// In this mode collisions are no longer evaluated and position is not integrated.
        /// </summary>
        StuckInWall = 3
    }
}
