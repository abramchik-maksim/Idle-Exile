using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HeroMovementSystem))]
    public partial class HeroAttackSystem : SystemBase
    {
        private EntityQuery _enemyQuery;
        private DamageEventBufferSystem _damageBuffer;
        private uint _rngState;

        private struct PendingMeleeHit
        {
            public Entity Target;
            public float TotalDamage;
            public bool IsCritical;
            public float2 TargetPos;
            public float2 HeroPos;
            public float Distance;
            public float AttackRange;
        }

        private struct PendingProjectile
        {
            public float2 HeroPos;
            public Entity Target;
            public float TotalDamage;
            public bool IsCritical;
            public int VisualId;
        }

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
            _rngState = (uint)System.Environment.TickCount;
            RequireForUpdate<HeroTag>();
        }

        protected override void OnUpdate()
        {
            if (_enemyQuery.IsEmpty) return;

            float dt = SystemAPI.Time.DeltaTime;

            var enemies = _enemyQuery.ToEntityArray(Allocator.Temp);
            var enemyPositions = _enemyQuery.ToComponentDataArray<Position2D>(Allocator.Temp);

            bool isMelee = false;
            float attackRange = 50f;

            foreach (var range in SystemAPI.Query<RefRO<HeroAttackRange>>().WithAll<HeroTag>())
            {
                isMelee = range.ValueRO.IsMelee != 0;
                attackRange = range.ValueRO.Value;
                break;
            }

            HeroSkillAffixData affixData = default;
            bool hasAffixData = false;
            foreach (var affix in SystemAPI.Query<RefRO<HeroSkillAffixData>>().WithAll<HeroTag>())
            {
                affixData = affix.ValueRO;
                hasAffixData = true;
                break;
            }

            int heroProjectileVisualId = 0;
            foreach (var projVis in SystemAPI.Query<RefRO<ProjectileVisualId>>().WithAll<HeroTag>())
            {
                heroProjectileVisualId = projVis.ValueRO.Value;
                break;
            }

            var meleeHits = new NativeList<PendingMeleeHit>(4, Allocator.Temp);
            var projectiles = new NativeList<PendingProjectile>(4, Allocator.Temp);

            foreach (var (cooldown, heroPos, stats)
                in SystemAPI.Query<RefRW<AttackCooldown>, RefRO<Position2D>, RefRO<CombatStats>>()
                    .WithAll<HeroTag, AttackEnabled>())
            {
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRW.Timer > 0f) continue;

                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;
                int nearestIdx = -1;

                for (int i = 0; i < enemies.Length; i++)
                {
                    float distSq = math.distancesq(heroPos.ValueRO.Value, enemyPositions[i].Value);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = enemies[i];
                        nearestIdx = i;
                    }
                }

                if (nearest == Entity.Null) continue;

                float dist = math.sqrt(nearestDistSq);
                float totalDamage = stats.ValueRO.PhysicalDamage + stats.ValueRO.FireDamage
                    + stats.ValueRO.ColdDamage + stats.ValueRO.LightningDamage;
                float critChance = math.clamp(stats.ValueRO.CriticalChance, 0f, 1f);
                bool isCritical = NextRandom() < critChance;
                if (isCritical)
                    totalDamage *= math.max(1f, stats.ValueRO.CriticalMultiplier);

                if (isMelee)
                {
                    if (dist > attackRange) continue;

                    cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                    meleeHits.Add(new PendingMeleeHit
                    {
                        Target = nearest,
                        TotalDamage = totalDamage,
                        IsCritical = isCritical,
                        TargetPos = enemyPositions[nearestIdx].Value,
                        HeroPos = heroPos.ValueRO.Value,
                        Distance = dist,
                        AttackRange = attackRange
                    });
                }
                else
                {
                    cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                    projectiles.Add(new PendingProjectile
                    {
                        HeroPos = heroPos.ValueRO.Value,
                        Target = nearest,
                        TotalDamage = totalDamage,
                        IsCritical = isCritical,
                        VisualId = heroProjectileVisualId
                    });
                }
            }

            enemies.Dispose();
            enemyPositions.Dispose();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < meleeHits.Length; i++)
            {
                var hit = meleeHits[i];

                var targetStats = EntityManager.GetComponentData<CombatStats>(hit.Target);
                float finalDmg = DamageCalculator.ApplyArmorReduction(hit.TotalDamage, targetStats.Armor);

                targetStats.CurrentHealth -= finalDmg;
                EntityManager.SetComponentData(hit.Target, targetStats);

                if (targetStats.CurrentHealth <= 0f && !EntityManager.HasComponent<DeadTag>(hit.Target))
                    ecb.AddComponent<DeadTag>(hit.Target);

                if (hasAffixData)
                    ApplyAilmentsOnHit(hit.Target, hit.TotalDamage, affixData);

                int actorId = EntityManager.HasComponent<ActorId>(hit.Target)
                    ? EntityManager.GetComponentData<ActorId>(hit.Target).Value
                    : -1;

                _damageBuffer.EventQueue.Enqueue(new DamageEvent
                {
                    Amount = finalDmg,
                    WorldX = hit.TargetPos.x,
                    WorldY = hit.TargetPos.y,
                    IsCritical = hit.IsCritical,
                    TargetActorId = actorId,
                    IsFromHero = true,
                    DamageCategory = 0
                });

                float2 attackDir = math.normalizesafe(hit.TargetPos - hit.HeroPos, new float2(0, 1));

                var fx = ecb.CreateEntity();
                ecb.AddComponent(fx, new HeroSlashFX
                {
                    Origin = hit.HeroPos,
                    Direction = attackDir,
                    Length = math.min(hit.Distance, hit.AttackRange),
                    Timer = 0f,
                    Duration = 0.15f
                });
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                var p = projectiles[i];

                var proj = ecb.CreateEntity();
                ecb.AddComponent(proj, new ProjectileTag());
                ecb.AddComponent(proj, new Position2D { Value = p.HeroPos });
                ecb.AddComponent(proj, new ProjectileData
                {
                    Target = p.Target,
                    Speed = 12f,
                    Damage = p.TotalDamage,
                    IsCritical = p.IsCritical,
                    VisualId = p.VisualId
                });
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
            meleeHits.Dispose();
            projectiles.Dispose();
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
