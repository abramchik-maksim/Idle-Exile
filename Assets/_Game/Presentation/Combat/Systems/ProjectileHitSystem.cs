using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    /// <summary>
    /// Detects projectile-enemy collision, applies damage, enqueues DamageEvent.
    /// TODO: Currently uses simple distance check O(projectiles).
    /// Migrate to spatial hash when entity counts justify it.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial class ProjectileHitSystem : SystemBase
    {
        private const float HitRadius = 0.3f;
        private DamageEventBufferSystem _damageBuffer;

        protected override void OnCreate()
        {
            RequireForUpdate<ProjectileTag>();
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<ProjectileData>>()
                    .WithAll<ProjectileTag>()
                    .WithEntityAccess())
            {
                var target = proj.ValueRO.Target;

                if (!EntityManager.Exists(target) || EntityManager.HasComponent<DeadTag>(target))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var targetPos = EntityManager.GetComponentData<Position2D>(target).Value;
                float dist = math.distance(pos.ValueRO.Value, targetPos);

                if (dist > HitRadius) continue;

                var targetStats = EntityManager.GetComponentData<CombatStats>(target);

                float armor = targetStats.Armor;
                float rawDmg = proj.ValueRO.Damage;
                float reduction = armor / (armor + 10f * rawDmg);
                float finalDmg = rawDmg * (1f - reduction);
                finalDmg = math.max(finalDmg, 0f);

                targetStats.CurrentHealth -= finalDmg;
                EntityManager.SetComponentData(target, targetStats);

                if (targetStats.CurrentHealth <= 0f && !EntityManager.HasComponent<DeadTag>(target))
                    ecb.AddComponent<DeadTag>(target);

                int actorId = EntityManager.HasComponent<ActorId>(target)
                    ? EntityManager.GetComponentData<ActorId>(target).Value
                    : -1;

                _damageBuffer.EventQueue.Enqueue(new DamageEvent
                {
                    Amount = finalDmg,
                    WorldX = targetPos.x,
                    WorldY = targetPos.y,
                    IsCritical = proj.ValueRO.IsCritical,
                    TargetActorId = actorId
                });

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
