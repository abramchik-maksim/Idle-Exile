using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyProjectileMovementSystem))]
    public partial class EnemyProjectileHitSystem : SystemBase
    {
        private const float HitRadius = 0.3f;
        private DamageEventBufferSystem _damageBuffer;
        private uint _rngState;

        protected override void OnCreate()
        {
            RequireForUpdate<EnemyProjectileTag>();
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
            _rngState = (uint)System.Environment.TickCount + 31u;
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<ProjectileData>>()
                    .WithAll<EnemyProjectileTag>()
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

                bool blocked = targetStats.BlockChance > 0f && NextRandom() < targetStats.BlockChance;
                if (blocked)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float rawDmg = proj.ValueRO.Damage;
                float finalDmg = DamageCalculator.ApplyArmorReduction(rawDmg, targetStats.Armor);

                targetStats.CurrentHealth -= finalDmg;
                EntityManager.SetComponentData(target, targetStats);

                bool isHero = EntityManager.HasComponent<HeroTag>(target);
                if (targetStats.CurrentHealth <= 0f && !isHero
                    && !EntityManager.HasComponent<DeadTag>(target))
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
                    TargetActorId = actorId,
                    IsFromHero = false,
                    DamageCategory = 0
                });

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private float NextRandom()
        {
            _rngState = _rngState * 747796405u + 2891336453u;
            uint result = ((_rngState >> (int)((_rngState >> 28) + 4u)) ^ _rngState) * 277803737u;
            result = (result >> 22) ^ result;
            return result / (float)uint.MaxValue;
        }
    }
}
