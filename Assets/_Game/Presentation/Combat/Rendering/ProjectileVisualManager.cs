using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VContainer;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class ProjectileVisualManager : MonoBehaviour
    {
        private const int SortOrderBase = 10000;
        private const int ProjectileSortOffset = 50;
        private SpriteViewPool _pool;
        private readonly Dictionary<Entity, SpriteView> _activeViews = new();

        private EntityQuery _heroProjectileQuery;
        private EntityQuery _enemyProjectileQuery;
        private bool _initialized;

        [Inject]
        public void Construct(ICombatVisualProvider visualProvider)
        {
            _pool = new SpriteViewPool(visualProvider);
        }

        private void LateUpdate()
        {
            if (!TryInitialize()) return;

            SyncProjectiles(_heroProjectileQuery);
            SyncProjectiles(_enemyProjectileQuery);
            CleanupDestroyedEntities();
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            var em = world.EntityManager;

            _heroProjectileQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<ProjectileTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.ReadOnly<ProjectileData>()
            );

            _enemyProjectileQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyProjectileTag>(),
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.ReadOnly<ProjectileData>()
            );

            _initialized = true;
            return true;
        }

        private readonly HashSet<Entity> _seenThisFrame = new();
        private readonly List<Entity> _toRemove = new();

        private void SyncProjectiles(EntityQuery query)
        {
            int count = query.CalculateEntityCount();
            if (count == 0) return;

            var entities = query.ToEntityArray(Allocator.Temp);
            var positions = query.ToComponentDataArray<Position2D>(Allocator.Temp);
            var projDatas = query.ToComponentDataArray<ProjectileData>(Allocator.Temp);

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                _seenThisFrame.Add(entity);

                if (!_activeViews.TryGetValue(entity, out var view))
                {
                    view = _pool.Get(projDatas[i].VisualId);
                    if (view == null) continue;
                    _activeViews[entity] = view;
                }

                float px = positions[i].Value.x;
                float py = positions[i].Value.y;
                view.SetPosition(px, py);

                var target = projDatas[i].Target;
                if (em.Exists(target) && em.HasComponent<Position2D>(target))
                {
                    var targetPos = em.GetComponentData<Position2D>(target).Value;
                    float2 dir = targetPos - positions[i].Value;
                    if (math.lengthsq(dir) > 0.0001f)
                    {
                        float angle = math.atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        view.SetRotation(angle);
                    }
                }

                view.SetSortingOrder(SortOrderBase + Mathf.RoundToInt(-py * 100f) + ProjectileSortOffset);
            }

            entities.Dispose();
            positions.Dispose();
            projDatas.Dispose();
        }

        private void CleanupDestroyedEntities()
        {
            _toRemove.Clear();
            foreach (var entity in _activeViews.Keys)
            {
                if (!_seenThisFrame.Contains(entity))
                    _toRemove.Add(entity);
            }

            for (int i = 0; i < _toRemove.Count; i++)
            {
                if (_activeViews.TryGetValue(_toRemove[i], out var view))
                {
                    view.SetRotation(0f);
                    _pool.Return(view);
                    _activeViews.Remove(_toRemove[i]);
                }
            }

            _seenThisFrame.Clear();
        }

        public void DespawnAll()
        {
            foreach (var kvp in _activeViews)
            {
                kvp.Value.SetRotation(0f);
                _pool.Return(kvp.Value);
            }
            _activeViews.Clear();
        }

        private void OnDestroy()
        {
            DespawnAll();
            _pool?.Dispose();

            if (_initialized)
            {
                _heroProjectileQuery.Dispose();
                _enemyProjectileQuery.Dispose();
            }
        }
    }
}
