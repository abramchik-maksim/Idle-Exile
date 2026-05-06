using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Presentation.Combat.Systems
{
    /// <summary>
    /// Temporary diagnostics for hero projectile spawn/despawn. Toggle in inspector via static field (or set from cheat later).
    /// </summary>
    public static class ProjectileLifetimeDebug
    {
        /// <summary>When false, no logs (avoid spam in production builds after tuning).</summary>
        public static bool HeroProjectileLogsEnabled = true;

        public static void LogHeroSpawn(
            Entity entity,
            float2 spawnPos,
            float2 velocity,
            float ttlSeconds,
            int lineageId,
            int forkG,
            float forkR,
            int pierceG,
            float pierceR,
            int chainG,
            float chainR,
            float damage)
        {
            if (!HeroProjectileLogsEnabled) return;

            float sp = math.length(velocity);
            Debug.Log(
                $"[HeroProjectile] Spawn entity={entity} pos=({spawnPos.x:F3},{spawnPos.y:F3}) " +
                $"vel=({velocity.x:F2},{velocity.y:F2}) speed={sp:F2} ttl={ttlSeconds:F3}s lineage={lineageId} " +
                $"dmg={damage:F1} fork={forkG}+{forkR:F2} pierce={pierceG}+{pierceR:F2} chain={chainG}+{chainR:F2}");
        }

        public static void LogHeroDespawn(
            Entity entity,
            string reason,
            float2 position,
            float2 velocity,
            float ttlRemaining,
            ProjectileMotionMode motionMode,
            string detail = null)
        {
            if (!HeroProjectileLogsEnabled) return;

            string tail = string.IsNullOrEmpty(detail) ? string.Empty : $" | {detail}";
            Debug.Log(
                $"[HeroProjectile] Despawn entity={entity} reason={reason} pos=({position.x:F3},{position.y:F3}) " +
                $"vel=({velocity.x:F2},{velocity.y:F2}) ttlLeft={ttlRemaining:F4}s mode={motionMode}{tail}");
        }

        public static void LogHeroForkSpawn(Entity entity, float2 spawnPos, float2 velocity, float ttlSeconds, int lineageId)
        {
            if (!HeroProjectileLogsEnabled) return;

            Debug.Log(
                $"[HeroProjectile] SpawnFork entity={entity} pos=({spawnPos.x:F3},{spawnPos.y:F3}) " +
                $"vel=({velocity.x:F2},{velocity.y:F2}) ttl={ttlSeconds:F3}s lineage={lineageId}");
        }
    }
}
