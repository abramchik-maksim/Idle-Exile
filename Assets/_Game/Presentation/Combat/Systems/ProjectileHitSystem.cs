using Game.Domain.Combat;
using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProjectileMovementSystem))]
    public partial class ProjectileHitSystem : SystemBase
    {
        private const float HitRadius = 0.3f;
        private DamageEventBufferSystem _damageBuffer;
        private uint _rngState;

        protected override void OnCreate()
        {
            RequireForUpdate<ProjectileTag>();
            _damageBuffer = World.GetExistingSystemManaged<DamageEventBufferSystem>();
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

                float rawDmg = proj.ValueRO.Damage;
                float finalDmg = DamageCalculator.ApplyArmorReduction(rawDmg, targetStats.Armor);

                targetStats.CurrentHealth -= finalDmg;
                EntityManager.SetComponentData(target, targetStats);

                if (targetStats.CurrentHealth <= 0f && !EntityManager.HasComponent<DeadTag>(target))
                    ecb.AddComponent<DeadTag>(target);

                if (hasAffixData)
                    ApplyAilmentsOnHit(target, rawDmg, affixData);

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
                    IsFromHero = true,
                    DamageCategory = 0
                });

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
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
