namespace Game.Domain.Combat
{
    /// <summary>Shared radii and tuning for hero/enemy projectiles and hit tests (ECS uses these; boss override is a future component).</summary>
    public static class ProjectileConstants
    {
        public const float ProjectileHitRadius = 0.2f;
        public const float EnemyHitRadius = 0.1f;

        public const float DefaultHeroProjectileSpeed = 12f;
        public const float DefaultEnemyProjectileSpeed = 8f;

        /// <summary>Upper bound on hero projectile lifetime if nothing destroys it sooner (hits, TTL in movement).</summary>
        public const float HeroProjectileMaxLifetimeSeconds = 2f;

        /// <summary>Upper bound on enemy projectile lifetime (separate from hero balance).</summary>
        public const float EnemyProjectileMaxLifetimeSeconds = 30f;

        /// <summary>Spawn position offset along aim so swept tests do not register t=0 wall hits at the hero feet / tile seam.</summary>
        public const float ProjectileSpawnForwardOffset = 0.15f;

        /// <summary>
        /// Hero primary projectile wall hits closer than this (world units) to the shot SpawnOrigin are ignored so wall
        /// colliders right at the hero's body do not immediately eat the shot. Only applied when
        /// <see cref="Game.Presentation.Combat.Components.ProjectileData.WallHitNearSpawnIgnoreEnabled"/> is non-zero
        /// (primary shot only — fork children and chain hops collide normally).
        /// Keep small (≤ 1 hero radius + spawn offset) so real arena walls are not silently passed through.
        /// </summary>
        public const float HeroProjectileWallHitIgnoreRadiusFromSpawn = 0.6f;

        /// <summary>
        /// Ignore wall hits at the start of an almost-zero-length swept segment (SegmentVsCapsule <c>t=0</c> glitches when the projectile spawns essentially on top of a wall).
        /// Must be strictly smaller than a normal per-frame travel distance squared (e.g. speed 12 at 60 fps → 0.2 units → 0.04). Set to <c>1e-4</c> = segment length below 0.01 units.
        /// </summary>
        public const float HeroProjectileWallHitMinSegmentLengthSq = 1e-4f;

        public const float HeroProjectileWallHitIgnoreTIfSegmentShort = 1e-4f;

        public const float ChainTurnRateRadPerSec = 6f;
        public const float RicochetAngleToleranceDeg = 10f;
        public const float ForkAngleDeg = 60f;

        public const float WallBakeRdpEpsilon = 0.05f;

        /// <summary>
        /// After a successful wall ricochet chain, offset position along aim toward the chain target so the projectile
        /// center clears the inflated wall swept capsule; otherwise the next frame reports another wall hit at t≈0 and the shot sticks.
        /// </summary>
        public const float WallChainExitAlongAimPadding = 0.12f;

        /// <summary>
        /// When a projectile stops in a wall, let it travel a tiny extra distance along incoming velocity
        /// before freezing, so the sprite visually enters the terrain.
        /// </summary>
        public const float WallStickEmbedSeconds = 0.08f;
        public const float WallStickEmbedMaxDistance = 0.22f;
    }
}
