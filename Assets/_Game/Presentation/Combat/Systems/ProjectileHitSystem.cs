using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial class ProjectileHitSystem : SystemBase
    {
        private DamageEventBufferSystem _damageBuffer;
        private EntityQuery _enemyQuery;
        private uint _rngState;

        private enum PathHitKind : byte
        {
            Enemy = 0,
            Wall = 1
        }

        private enum WallShape : byte
        {
            None = 0,
            Edge = 1,
            Aabb = 2,
            Circle = 3
        }

        private struct PathHit
        {
            public float T;
            public PathHitKind Kind;
            public Entity Enemy;
            public float2 HitPos;
            public float2 WallNormal;
            public Entity WallEntity;
            public WallShape WallShape;
            public float2 WallA;
            public float2 WallB;
        }

        protected override void OnCreate()
        {
            RequireForUpdate<ProjectileTag>();
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>());
            _rngState = (uint)System.Environment.TickCount + 7u;
        }

        protected override void OnUpdate()
        {
            HeroSkillAffixData affixData = default;
            bool hasAffixData = false;
            foreach (var affix in SystemAPI.Query<RefRO<HeroSkillAffixData>>().WithAll<HeroTag>())
            {
                affixData = affix.ValueRO;
                hasAffixData = true;
                break;
            }

            Entity heroEntity = Entity.Null;
            foreach (var (_, e) in SystemAPI.Query<RefRO<CombatStats>>().WithAll<HeroTag>().WithEntityAccess())
            {
                heroEntity = e;
                break;
            }

            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyPositions = _enemyQuery.ToComponentDataArray<Position2D>(Allocator.Temp);

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            float totalLeech = 0f;

            float combinedRadius = ProjectileConstants.ProjectileHitRadius + ProjectileConstants.EnemyHitRadius;
            float pr = ProjectileConstants.ProjectileHitRadius;

            foreach (var (pos, proj, entity)
                in SystemAPI.Query<RefRW<Position2D>, RefRW<ProjectileData>>()
                    .WithAll<ProjectileTag>()
                    .WithEntityAccess())
            {
                if (proj.ValueRO.MotionMode == ProjectileMotionMode.StuckInWall)
                    continue;

                float2 prev = proj.ValueRO.PrevPosition;
                float2 curr = pos.ValueRO.Value;

                var hits = new NativeList<PathHit>(32, Allocator.Temp);
                CollectWallHits(prev, curr, pr, ref hits);
                CollectEnemyHits(prev, curr, combinedRadius, entity, ref hits, enemyEntities, enemyPositions);

                SortPathHits(ref hits);

                var historyBuf = EntityManager.GetBuffer<ProjectileHitHistory>(entity);

                for (int h = 0; h < hits.Length; h++)
                {
                    var ev = hits[h];

                    if (ev.Kind == PathHitKind.Wall)
                    {
                        float segLenSq = math.lengthsq(curr - prev);
                        if (ev.T <= ProjectileConstants.HeroProjectileWallHitIgnoreTIfSegmentShort &&
                            segLenSq < ProjectileConstants.HeroProjectileWallHitMinSegmentLengthSq)
                            continue;

                        if (proj.ValueRO.WallHitNearSpawnIgnoreEnabled != 0)
                        {
                            float ignoreR = ProjectileConstants.HeroProjectileWallHitIgnoreRadiusFromSpawn;
                            if (math.distancesq(ev.HitPos, proj.ValueRO.SpawnOrigin) < ignoreR * ignoreR)
                                continue;
                        }

                        float2 reflectDir = ProjectileHitMath.Reflect(proj.ValueRO.Velocity, ev.WallNormal);

                        bool canPayChain = proj.ValueRO.ChainGuaranteedLeft > 0 ||
                                           proj.ValueRO.ChainRemainderChance > 0f;
                        if (!canPayChain)
                        {
                            StickInWall(ref pos.ValueRW, ref proj.ValueRW, entity, ev,
                                "WallHit_NoChainBudget_StuckInWall",
                                $"t={ev.T:F4} hit=({ev.HitPos.x:F3},{ev.HitPos.y:F3}) n=({ev.WallNormal.x:F2},{ev.WallNormal.y:F2}) hits={hits.Length} wall={FormatWall(ev)} prev=({prev.x:F3},{prev.y:F3}) curr=({curr.x:F3},{curr.y:F3})");
                            break;
                        }

                        Entity ric = FindRicochetChainTarget(ev.HitPos, reflectDir, enemyEntities, enemyPositions);
                        if (ric == Entity.Null)
                        {
                            StickInWall(ref pos.ValueRW, ref proj.ValueRW, entity, ev,
                                "WallHit_ChainAvailableButNoRicochetTarget_StuckInWall",
                                $"t={ev.T:F4} hit=({ev.HitPos.x:F3},{ev.HitPos.y:F3}) reflect=({reflectDir.x:F2},{reflectDir.y:F2}) wall={FormatWall(ev)} prev=({prev.x:F3},{prev.y:F3}) curr=({curr.x:F3},{curr.y:F3})");
                            break;
                        }

                        if (!TryConsumeProc(ref proj.ValueRW.ChainGuaranteedLeft,
                                ref proj.ValueRW.ChainRemainderChance))
                        {
                            StickInWall(ref pos.ValueRW, ref proj.ValueRW, entity, ev,
                                "WallHit_ChainRollFailedAfterRicochetTarget_StuckInWall",
                                $"ric={ric} wall={FormatWall(ev)}");
                            break;
                        }

                        {
                            float speed = math.length(proj.ValueRO.Velocity);
                            float2 tp = EntityManager.GetComponentData<Position2D>(ric).Value;
                            float2 dir = math.normalizesafe(tp - ev.HitPos, new float2(0f, 1f));
                            float exitAlong = ProjectileConstants.ProjectileHitRadius +
                                              ProjectileConstants.WallChainExitAlongAimPadding;
                            float2 exitPos = ev.HitPos + dir * exitAlong;
                            pos.ValueRW.Value = exitPos;
                            proj.ValueRW.Velocity = dir * speed;
                            proj.ValueRW.PrevPosition = exitPos;
                            proj.ValueRW.MotionMode = ProjectileMotionMode.HomingChain;
                            proj.ValueRW.MotionTarget = ric;
                            proj.ValueRW.WallHitNearSpawnIgnoreEnabled = 0;
                        }

                        break;
                    }

                    Entity target = ev.Enemy;
                    float2 hitPos = ev.HitPos;

                    if (!EntityManager.Exists(target) || EntityManager.HasComponent<DeadTag>(target))
                        continue;

                    if (IsInHistory(historyBuf, target))
                        continue;

                    var targetStats = EntityManager.GetComponentData<CombatStats>(target);

                    float rawDmg = proj.ValueRO.Damage;
                    bool ignoreArmor = proj.ValueRO.IgnoreArmorChance > 0f &&
                                       NextRandom() < proj.ValueRO.IgnoreArmorChance;
                    float effectiveArmor = ignoreArmor ? 0f : targetStats.Armor;
                    float finalDmg = DamageCalculator.ApplyArmorReduction(rawDmg, effectiveArmor);

                    targetStats.CurrentHealth -= finalDmg;
                    EntityManager.SetComponentData(target, targetStats);

                    if (targetStats.CurrentHealth <= 0f && !EntityManager.HasComponent<DeadTag>(target))
                        ecb.AddComponent<DeadTag>(target);

                    if (hasAffixData)
                        ApplyAilmentsOnHit(target, rawDmg, affixData);

                    if (proj.ValueRO.LifeLeech > 0f)
                        totalLeech += finalDmg * proj.ValueRO.LifeLeech;

                    int actorId = EntityManager.HasComponent<ActorId>(target)
                        ? EntityManager.GetComponentData<ActorId>(target).Value
                        : -1;

                    _damageBuffer.EventQueue.Enqueue(new DamageEvent
                    {
                        Amount = finalDmg,
                        WorldX = hitPos.x,
                        WorldY = hitPos.y,
                        IsCritical = proj.ValueRO.IsCritical,
                        TargetActorId = actorId,
                        IsFromHero = true,
                        DamageCategory = 0
                    });

                    AddHitToHistory(historyBuf, target);
                    proj.ValueRW.LastHitTarget = target;

                    bool forked = TryConsumeProc(ref proj.ValueRW.ForkGuaranteedLeft,
                        ref proj.ValueRW.ForkRemainderChance);
                    if (forked)
                    {
                        SpawnForkChildren(hitPos, proj.ValueRO.Velocity, in proj.ValueRO, historyBuf, target, ecb);
                        ProjectileLifetimeDebug.LogHeroDespawn(
                            entity,
                            "EnemyHit_ForkSpent_DestroyParent",
                            pos.ValueRO.Value,
                            proj.ValueRO.Velocity,
                            proj.ValueRO.TimeToLiveSeconds,
                            proj.ValueRO.MotionMode,
                            $"target={target} hit=({hitPos.x:F3},{hitPos.y:F3}) t={ev.T:F4}");
                        ecb.DestroyEntity(entity);
                        break;
                    }

                    bool pierced = TryConsumeProc(ref proj.ValueRW.PierceGuaranteedLeft,
                        ref proj.ValueRW.PierceRemainderChance);
                    if (pierced)
                        continue;

                    bool chained = TryConsumeProc(ref proj.ValueRW.ChainGuaranteedLeft,
                        ref proj.ValueRW.ChainRemainderChance);
                    if (chained)
                    {
                        Entity chainTarget = FindNearestTarget(hitPos, historyBuf, target, enemyEntities,
                            enemyPositions, Entity.Null);
                        if (chainTarget != Entity.Null)
                        {
                            float speed = math.length(proj.ValueRO.Velocity);
                            float2 chainPos = EntityManager.GetComponentData<Position2D>(chainTarget).Value;
                            float2 dir = math.normalizesafe(chainPos - hitPos, new float2(0f, 1f));
                            pos.ValueRW.Value = hitPos;
                            proj.ValueRW.Velocity = dir * speed;
                            proj.ValueRW.PrevPosition = hitPos;
                            proj.ValueRW.MotionMode = ProjectileMotionMode.HomingChain;
                            proj.ValueRW.MotionTarget = chainTarget;
                        }
                        else
                        {
                            ProjectileLifetimeDebug.LogHeroDespawn(
                                entity,
                                "EnemyHit_ChainBudgetButNoTarget",
                                pos.ValueRO.Value,
                                proj.ValueRO.Velocity,
                                proj.ValueRO.TimeToLiveSeconds,
                                proj.ValueRO.MotionMode,
                                $"fromTarget={target} hit=({hitPos.x:F3},{hitPos.y:F3}) t={ev.T:F4}");
                            ecb.DestroyEntity(entity);
                        }

                        break;
                    }

                    ProjectileLifetimeDebug.LogHeroDespawn(
                        entity,
                        "EnemyHit_NoForkPierceChainProc",
                        pos.ValueRO.Value,
                        proj.ValueRO.Velocity,
                        proj.ValueRO.TimeToLiveSeconds,
                        proj.ValueRO.MotionMode,
                        $"target={target} hit=({hitPos.x:F3},{hitPos.y:F3}) t={ev.T:F4} forkL={proj.ValueRO.ForkGuaranteedLeft} pierceL={proj.ValueRO.PierceGuaranteedLeft} chainL={proj.ValueRO.ChainGuaranteedLeft}");
                    ecb.DestroyEntity(entity);
                    break;
                }

                hits.Dispose();
            }

            if (totalLeech > 0f && heroEntity != Entity.Null && EntityManager.Exists(heroEntity))
            {
                var hs = EntityManager.GetComponentData<CombatStats>(heroEntity);
                hs.CurrentHealth = math.min(hs.CurrentHealth + totalLeech, hs.MaxHealth);
                EntityManager.SetComponentData(heroEntity, hs);
            }

            enemyEntities.Dispose();
            enemyPositions.Dispose();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void CollectWallHits(float2 prev, float2 curr, float projectileRadius, ref NativeList<PathHit> hits)
        {
            foreach (var (pos, box, wallEntity)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<WallAABB>>().WithAll<WallTag>().WithEntityAccess())
            {
                if (!ProjectileHitMath.SegmentVsAABB(prev, curr, pos.ValueRO.Value, box.ValueRO.HalfExtents,
                        projectileRadius, out float t, out float2 n, out float2 hp))
                    continue;
                hits.Add(new PathHit
                {
                    T = t,
                    Kind = PathHitKind.Wall,
                    Enemy = Entity.Null,
                    HitPos = hp,
                    WallNormal = n,
                    WallEntity = wallEntity,
                    WallShape = WallShape.Aabb,
                    WallA = pos.ValueRO.Value - box.ValueRO.HalfExtents,
                    WallB = pos.ValueRO.Value + box.ValueRO.HalfExtents
                });
            }

            foreach (var (pos, circ, wallEntity)
                in SystemAPI.Query<RefRO<Position2D>, RefRO<WallCircle>>().WithAll<WallTag>().WithEntityAccess())
            {
                float r = circ.ValueRO.Radius + projectileRadius;
                if (!ProjectileHitMath.SegmentVsCircle(prev, curr, pos.ValueRO.Value, r, out float t, out float2 hp))
                    continue;
                float2 n = math.normalizesafe(hp - pos.ValueRO.Value, new float2(0f, 1f));
                hits.Add(new PathHit
                {
                    T = t,
                    Kind = PathHitKind.Wall,
                    HitPos = hp,
                    WallNormal = n,
                    WallEntity = wallEntity,
                    WallShape = WallShape.Circle,
                    WallA = pos.ValueRO.Value,
                    WallB = new float2(circ.ValueRO.Radius, 0f)
                });
            }

            foreach (var (edge, wallEntity)
                in SystemAPI.Query<RefRO<WallEdge>>().WithAll<WallTag>().WithEntityAccess())
            {
                float2 p1 = edge.ValueRO.A;
                float2 p2 = edge.ValueRO.B;
                if (!ProjectileHitMath.SegmentVsCapsule(prev, curr, p1, p2, projectileRadius, out float t,
                        out float2 n, out float2 hp))
                    continue;
                hits.Add(new PathHit
                {
                    T = t,
                    Kind = PathHitKind.Wall,
                    HitPos = hp,
                    WallNormal = n,
                    WallEntity = wallEntity,
                    WallShape = WallShape.Edge,
                    WallA = p1,
                    WallB = p2
                });
            }
        }

        private static string FormatWall(in PathHit hit)
        {
            return hit.WallShape switch
            {
                WallShape.Edge => $"EDGE entity={hit.WallEntity} A=({hit.WallA.x:F3},{hit.WallA.y:F3}) B=({hit.WallB.x:F3},{hit.WallB.y:F3})",
                WallShape.Aabb => $"AABB entity={hit.WallEntity} min=({hit.WallA.x:F3},{hit.WallA.y:F3}) max=({hit.WallB.x:F3},{hit.WallB.y:F3})",
                WallShape.Circle => $"CIRCLE entity={hit.WallEntity} center=({hit.WallA.x:F3},{hit.WallA.y:F3}) r={hit.WallB.x:F3}",
                _ => $"UNKNOWN entity={hit.WallEntity}"
            };
        }

        private void CollectEnemyHits(
            float2 prev,
            float2 curr,
            float combinedRadius,
            Entity projectileEntity,
            ref NativeList<PathHit> hits,
            NativeArray<Entity> enemyEntities,
            NativeArray<Position2D> enemyPositions)
        {
            var history = EntityManager.GetBuffer<ProjectileHitHistory>(projectileEntity);

            for (int i = 0; i < enemyEntities.Length; i++)
            {
                var candidate = enemyEntities[i];
                if (IsInHistory(history, candidate))
                    continue;

                float2 enemyPos = enemyPositions[i].Value;
                if (!ProjectileHitMath.SegmentVsCircle(prev, curr, enemyPos, combinedRadius, out float t,
                        out float2 hitPos))
                    continue;

                hits.Add(new PathHit
                {
                    T = t,
                    Kind = PathHitKind.Enemy,
                    Enemy = candidate,
                    HitPos = hitPos,
                    WallNormal = float2.zero
                });
            }
        }

        private static void SortPathHits(ref NativeList<PathHit> hits)
        {
            for (int i = 1; i < hits.Length; i++)
            {
                var key = hits[i];
                int j = i - 1;
                while (j >= 0 && PathHitOrdering(hits[j], key) > 0)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }

                hits[j + 1] = key;
            }
        }

        /// <summary>Negative if <paramref name="a"/> should be processed before <paramref name="b"/> (earlier along path).</summary>
        private static int PathHitOrdering(PathHit a, PathHit b)
        {
            const float tEps = 1e-4f;
            float dt = a.T - b.T;
            if (dt < -tEps) return -1;
            if (dt > tEps) return 1;
            // Same t: walls are collected before enemies; prefer enemy first so a wall seam at the hero does not eat the shot.
            int pa = a.Kind == PathHitKind.Enemy ? 0 : 1;
            int pb = b.Kind == PathHitKind.Enemy ? 0 : 1;
            return pa - pb;
        }

        private Entity FindRicochetChainTarget(
            float2 bouncePos,
            float2 reflectDir,
            NativeArray<Entity> enemies,
            NativeArray<Position2D> enemyPositions)
        {
            float tol = ProjectileConstants.RicochetAngleToleranceDeg * (math.PI / 180f);
            float2 rd = math.normalizesafe(reflectDir, new float2(0f, 1f));
            Entity best = Entity.Null;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (!EntityManager.Exists(e) || EntityManager.HasComponent<DeadTag>(e))
                    continue;

                float2 w = enemyPositions[i].Value;
                float2 to = w - bouncePos;
                float dsq = math.lengthsq(to);
                if (dsq < 1e-8f)
                    continue;

                float2 dirTo = math.normalizesafe(to, new float2(0f, 1f));
                float ang = math.acos(math.clamp(math.dot(rd, dirTo), -1f, 1f));
                if (ang > tol + 1e-5f)
                    continue;

                if (dsq < bestDistSq)
                {
                    bestDistSq = dsq;
                    best = e;
                }
            }

            return best;
        }

        private static void AddHitToHistory(DynamicBuffer<ProjectileHitHistory> history, Entity target)
        {
            for (int i = 0; i < history.Length; i++)
            {
                if (history[i].Target == target)
                    return;
            }

            history.Add(new ProjectileHitHistory { Target = target });
        }

        private static bool IsInHistory(DynamicBuffer<ProjectileHitHistory> history, Entity target)
        {
            for (int i = 0; i < history.Length; i++)
            {
                if (history[i].Target == target)
                    return true;
            }

            return false;
        }

        private static Entity FindNearestTarget(
            float2 origin,
            DynamicBuffer<ProjectileHitHistory> history,
            Entity currentTarget,
            NativeArray<Entity> enemies,
            NativeArray<Position2D> enemyPositions,
            Entity extraExcluded)
        {
            Entity best = Entity.Null;
            float bestDistSq = float.MaxValue;

            for (int i = 0; i < enemies.Length; i++)
            {
                var candidate = enemies[i];
                if (candidate == currentTarget || candidate == extraExcluded)
                    continue;
                if (IsInHistory(history, candidate))
                    continue;

                float distSq = math.distancesq(origin, enemyPositions[i].Value);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = candidate;
                }
            }

            return best;
        }

        private static void StickInWall(
            ref Position2D position,
            ref ProjectileData projectile,
            Entity entity,
            PathHit hit,
            string reason,
            string detail)
        {
            float2 incomingDir = math.normalizesafe(projectile.Velocity, -hit.WallNormal);
            float speed = math.length(projectile.Velocity);
            float embedDistance = math.min(
                speed * ProjectileConstants.WallStickEmbedSeconds,
                ProjectileConstants.WallStickEmbedMaxDistance);
            float2 stuckPos = hit.HitPos + incomingDir * embedDistance;

            position.Value = stuckPos;
            projectile.PrevPosition = stuckPos;
            projectile.Velocity = float2.zero;
            projectile.MotionMode = ProjectileMotionMode.StuckInWall;
            projectile.MotionTarget = Entity.Null;

            ProjectileLifetimeDebug.LogHeroDespawn(
                entity,
                reason,
                stuckPos,
                float2.zero,
                projectile.TimeToLiveSeconds,
                projectile.MotionMode,
                $"{detail} embed={embedDistance:F3}");
        }

        private bool TryConsumeProc(ref int guaranteedLeft, ref float remainderChance)
        {
            if (guaranteedLeft > 0)
            {
                guaranteedLeft--;
                return true;
            }

            if (remainderChance <= 0f) return false;
            float rollChance = remainderChance;
            remainderChance = 0f;
            return NextRandom() < rollChance;
        }

        private void SpawnForkChildren(
            float2 spawnPos,
            float2 parentVelocity,
            in ProjectileData parentData,
            DynamicBuffer<ProjectileHitHistory> parentHistory,
            Entity currentHitTarget,
            EntityCommandBuffer ecb)
        {
            float speed = math.length(parentVelocity);
            float2 baseDir = math.normalizesafe(parentVelocity, new float2(0f, 1f));
            float2 vA = ProjectileHitMath.RotateDeg(baseDir, ProjectileConstants.ForkAngleDeg) * speed;
            float2 vB = ProjectileHitMath.RotateDeg(baseDir, -ProjectileConstants.ForkAngleDeg) * speed;

            SpawnOneForkChild(spawnPos, vA, in parentData, parentHistory, currentHitTarget, ecb);
            SpawnOneForkChild(spawnPos, vB, in parentData, parentHistory, currentHitTarget, ecb);
        }

        private static void SpawnOneForkChild(
            float2 spawnPos,
            float2 velocity,
            in ProjectileData parentData,
            DynamicBuffer<ProjectileHitHistory> parentHistory,
            Entity currentHitTarget,
            EntityCommandBuffer ecb)
        {
            var child = ecb.CreateEntity();
            ecb.AddComponent(child, new ProjectileTag());
            ecb.AddComponent(child, new Position2D { Value = spawnPos });

            var childData = parentData;
            childData.Velocity = velocity;
            childData.PrevPosition = spawnPos;
            childData.MotionMode = ProjectileMotionMode.Straight;
            childData.MotionTarget = Entity.Null;
            childData.TimeToLiveSeconds = ProjectileConstants.HeroProjectileMaxLifetimeSeconds;
            childData.SpawnOrigin = spawnPos;
            childData.WallHitNearSpawnIgnoreEnabled = 0;
            childData.AgeSeconds = 0f;
            childData.ForkGuaranteedLeft = 0;
            childData.ForkRemainderChance = 0f;
            childData.LastHitTarget = currentHitTarget;

            ecb.AddComponent(child, childData);

            ProjectileLifetimeDebug.LogHeroForkSpawn(
                child,
                spawnPos,
                velocity,
                childData.TimeToLiveSeconds,
                parentData.ProjectileLineageId);

            var childHistory = ecb.AddBuffer<ProjectileHitHistory>(child);
            for (int i = 0; i < parentHistory.Length; i++)
                childHistory.Add(parentHistory[i]);
        }

        private void ApplyAilmentsOnHit(Entity target, float hitDamage, HeroSkillAffixData affixData)
        {
            if (!EntityManager.HasComponent<AilmentState>(target)) return;

            var ailment = EntityManager.GetComponentData<AilmentState>(target);
            bool changed = false;

            if (affixData.IgniteChance > 0f && NextRandom() < affixData.IgniteChance)
            {
                ailment.IgniteDamagePerTick = AilmentCalculator.GetIgniteDamagePerTick(hitDamage);
                ailment.IgniteTimer = AilmentCalculator.IgniteDuration;
                changed = true;
            }

            if (affixData.ChillChance > 0f && NextRandom() < affixData.ChillChance)
            {
                ailment.ChillStacks = math.min(ailment.ChillStacks + 1, AilmentCalculator.MaxChillStacks);
                changed = true;
            }

            if (affixData.ShockChance > 0f && NextRandom() < affixData.ShockChance)
            {
                ailment.ShockStacks = math.min(ailment.ShockStacks + 1, AilmentCalculator.MaxShockStacks);
                changed = true;
            }

            if (affixData.BleedChance > 0f && NextRandom() < affixData.BleedChance)
            {
                float dpt = AilmentCalculator.GetBleedDamagePerTick(hitDamage);
                var buffer = EntityManager.GetBuffer<BleedStack>(target);
                buffer.Add(new BleedStack
                {
                    DamagePerTick = dpt,
                    RemainingDuration = AilmentCalculator.BleedDuration
                });
                changed = true;
            }

            if (changed)
                EntityManager.SetComponentData(target, ailment);
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
