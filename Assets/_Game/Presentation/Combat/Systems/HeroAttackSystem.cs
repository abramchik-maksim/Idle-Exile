using Game.Presentation.Combat.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Presentation.Combat.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class HeroAttackSystem : SystemBase
    {
        private EntityQuery _enemyQuery;

        protected override void OnCreate()
        {
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.Exclude<DeadTag>()
            );
            RequireForUpdate<HeroTag>();
        }

        protected override void OnUpdate()
        {
            if (_enemyQuery.IsEmpty) return;

            float dt = SystemAPI.Time.DeltaTime;

            var enemies = _enemyQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var enemyPositions = _enemyQuery.ToComponentDataArray<Position2D>(Unity.Collections.Allocator.Temp);

            foreach (var (cooldown, heroPos, stats)
                in SystemAPI.Query<RefRW<AttackCooldown>, RefRO<Position2D>, RefRO<CombatStats>>()
                    .WithAll<HeroTag>())
            {
                cooldown.ValueRW.Timer -= dt;
                if (cooldown.ValueRW.Timer > 0f) continue;

                cooldown.ValueRW.Timer = cooldown.ValueRO.Cooldown;

                Entity nearest = Entity.Null;
                float nearestDistSq = float.MaxValue;

                for (int i = 0; i < enemies.Length; i++)
                {
                    float distSq = math.distancesq(heroPos.ValueRO.Value, enemyPositions[i].Value);
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearest = enemies[i];
                    }
                }

                if (nearest == Entity.Null) continue;

                // TODO: Use Domain DamageCalculator for crit rolls.
                // Currently Burst-incompatible due to managed types (option A).
                // Will migrate to ECS-native calculation when formulas stabilize.
                var proj = EntityManager.CreateEntity(
                    typeof(ProjectileTag),
                    typeof(Position2D),
                    typeof(ProjectileData)
                );

                EntityManager.SetComponentData(proj, new Position2D { Value = heroPos.ValueRO.Value });
                EntityManager.SetComponentData(proj, new ProjectileData
                {
                    Target = nearest,
                    Speed = 12f,
                    Damage = stats.ValueRO.PhysicalDamage,
                    IsCritical = false
                });
            }

            enemies.Dispose();
            enemyPositions.Dispose();
        }
    }
}
