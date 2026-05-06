using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyProjectileMovementSystem))]
    public partial class EnemyProjectileHitSystem : SystemBase
    {
        private DamageEventBufferSystem _damageBuffer;
        private uint _rngState;

        private enum EpHitKind : byte
        {
            Wall = 0,
            Hero = 1
        }

        private struct EpHit
        {
            public float T;
            public EpHitKind Kind;
            public float2 HitPos;
        }

        protected override void OnCreate()
        {
            RequireForUpdate<EnemyProjectileTag>();
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
            _rngState = (uint)System.Environment.TickCount + 31u;
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            float combinedRadius = ProjectileConstants.ProjectileHitRadius + ProjectileConstants.EnemyHitRadius;
            float pr = ProjectileConstants.ProjectileHitRadius;

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<ProjectileData>>()
                    .WithAll<EnemyProjectileTag>()
                    .WithEntityAccess())
            {
                Entity mt = proj.ValueRO.MotionTarget;
                if (mt == Entity.Null || !EntityManager.Exists(mt) || EntityManager.HasComponent<DeadTag>(mt))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float2 prev = proj.ValueRO.PrevPosition;
                float2 curr = pos.ValueRO.Value;
                float2 heroPos = EntityManager.GetComponentData<Position2D>(mt).Value;

                var hits = new NativeList<EpHit>(16, Allocator.Temp);
                CollectWallHits(prev, curr, pr, ref hits);

                if (ProjectileHitMath.SegmentVsCircle(prev, curr, heroPos, combinedRadius, out float th,
                        out float2 heroHitPos))
                {
                    hits.Add(new EpHit { T = th, Kind = EpHitKind.Hero, HitPos = heroHitPos });
                }

                SortEpHits(ref hits);

                if (hits.Length == 0)
                {
                    hits.Dispose();
                    continue;
                }

                var first = hits[0];
                hits.Dispose();

                if (first.Kind == EpHitKind.Wall)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var targetStats = EntityManager.GetComponentData<CombatStats>(mt);

                bool blocked = targetStats.BlockChance > 0f && NextRandom() < targetStats.BlockChance;
                if (blocked)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                float rawDmg = proj.ValueRO.Damage;
                float finalDmg = DamageCalculator.ApplyArmorReduction(rawDmg, targetStats.Armor);

                targetStats.CurrentHealth -= finalDmg;
                EntityManager.SetComponentData(mt, targetStats);

                bool isHero = EntityManager.HasComponent<HeroTag>(mt);
                if (targetStats.CurrentHealth <= 0f && !isHero
                    && !EntityManager.HasComponent<DeadTag>(mt))
                    ecb.AddComponent<DeadTag>(mt);

                int actorId = EntityManager.HasComponent<ActorId>(mt)
                    ? EntityManager.GetComponentData<ActorId>(mt).Value
                    : -1;

                _damageBuffer.EventQueue.Enqueue(new DamageEvent
                {
                    Amount = finalDmg,
                    WorldX = first.HitPos.x,
                    WorldY = first.HitPos.y,
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

        private void CollectWallHits(float2 prev, float2 curr, float projectileRadius, ref NativeList<EpHit> hits)
        {
            foreach (var (pos, box)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<WallAABB>>().WithAll<WallTag>())
            {
                if (!ProjectileHitMath.SegmentVsAABB(prev, curr, pos.ValueRO.Value, box.ValueRO.HalfExtents,
                        projectileRadius, out float t, out _, out float2 hp))
                    continue;
                hits.Add(new EpHit { T = t, Kind = EpHitKind.Wall, HitPos = hp });
            }

            foreach (var (pos, circ)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<WallCircle>>().WithAll<WallTag>())
            {
                float r = circ.ValueRO.Radius + projectileRadius;
                if (!ProjectileHitMath.SegmentVsCircle(prev, curr, pos.ValueRO.Value, r, out float t, out float2 hp))
                    continue;
                hits.Add(new EpHit { T = t, Kind = EpHitKind.Wall, HitPos = hp });
            }

            foreach (var edge in SystemAPI.Query<RefRO<WallEdge>>().WithAll<WallTag>())
            {
                float2 p1 = edge.ValueRO.A;
                float2 p2 = edge.ValueRO.B;
                if (!ProjectileHitMath.SegmentVsCapsule(prev, curr, p1, p2, projectileRadius, out float t,
                        out _, out float2 hp))
                    continue;
                hits.Add(new EpHit { T = t, Kind = EpHitKind.Wall, HitPos = hp });
            }
        }

        private static void SortEpHits(ref NativeList<EpHit> hits)
        {
            for (int i = 1; i < hits.Length; i++)
            {
                var key = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].T > key.T)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }

                hits[j + 1] = key;
            }
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
