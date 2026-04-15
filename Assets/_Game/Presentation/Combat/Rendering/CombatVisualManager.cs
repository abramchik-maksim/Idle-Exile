using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VContainer;
using Game.Presentation.Combat.Components;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class CombatVisualManager : MonoBehaviour
    {
        private const int SortOrderBase = 10000;
        private SpriteViewPool _pool;
        private readonly Dictionary<int, SpriteView> _activeViews = new();
        private readonly Dictionary<int, float> _previousX = new();

        private EntityQuery _visualQuery;
        private bool _initialized;

        [Inject]
        public void Construct(ICombatVisualProvider visualProvider)
        {
            _pool = new SpriteViewPool(visualProvider);
        }

        public void OnEntitySpawned(int actorId, int visualId)
        {
            if (_activeViews.ContainsKey(actorId))
            {
                Debug.LogWarning($"[CombatVisualManager] Actor {actorId} already has a view, replacing.");
                OnEntityDespawned(actorId);
            }

            var view = _pool.Get(visualId);
            if (view != null)
            {
                _activeViews[actorId] = view;
                _previousX[actorId] = 0f;
            }
        }

        public void OnEntityDespawned(int actorId)
        {
            if (_activeViews.TryGetValue(actorId, out var view))
            {
                _pool.Return(view);
                _activeViews.Remove(actorId);
                _previousX.Remove(actorId);
            }
        }

        public void PlayAnimation(int actorId, string trigger)
        {
            if (_activeViews.TryGetValue(actorId, out var view))
                view.PlayTrigger(trigger);
        }

        public SpriteView GetView(int actorId)
        {
            _activeViews.TryGetValue(actorId, out var view);
            return view;
        }

        private void LateUpdate()
        {
            if (!TryInitialize()) return;

            SyncPositions();
        }

        private bool TryInitialize()
        {
            if (_initialized) return true;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated) return false;

            var em = world.EntityManager;
            _visualQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Position2D>(),
                ComponentType.ReadOnly<ActorId>()
            );

            _initialized = true;
            return true;
        }

        private readonly HashSet<int> _seenThisFrame = new();

        private void SyncPositions()
        {
            if (_activeViews.Count == 0) return;

            var positions = _visualQuery.ToComponentDataArray<Position2D>(Allocator.Temp);
            var actorIds = _visualQuery.ToComponentDataArray<ActorId>(Allocator.Temp);

            _seenThisFrame.Clear();

            for (int i = 0; i < actorIds.Length; i++)
            {
                int id = actorIds[i].Value;
                _seenThisFrame.Add(id);

                if (!_activeViews.TryGetValue(id, out var view)) continue;

                float x = positions[i].Value.x;
                float y = positions[i].Value.y;

                view.SetPosition(x, y);

                if (_previousX.TryGetValue(id, out float prevX))
                {
                    float dx = x - prevX;
                    if (Mathf.Abs(dx) > 0.001f)
                    {
                        view.SetFlipX(dx < 0f);
                        view.SetBool("IsMoving", true);
                    }
                    else
                    {
                        view.SetBool("IsMoving", false);
                    }
                }
                _previousX[id] = x;

                int sortOrder = SortOrderBase + Mathf.RoundToInt(-y * 100f);
                view.SetSortingOrder(sortOrder);
            }

            positions.Dispose();
            actorIds.Dispose();

            CleanupDestroyedEntities();
        }

        private readonly List<int> _toRemove = new();

        private void CleanupDestroyedEntities()
        {
            _toRemove.Clear();
            foreach (var id in _activeViews.Keys)
            {
                if (!_seenThisFrame.Contains(id))
                    _toRemove.Add(id);
            }

            for (int i = 0; i < _toRemove.Count; i++)
                OnEntityDespawned(_toRemove[i]);
        }

        public void DespawnAll()
        {
            var ids = new List<int>(_activeViews.Keys);
            foreach (int id in ids)
                OnEntityDespawned(id);
        }

        private void OnDestroy()
        {
            DespawnAll();
            _pool?.Dispose();

            if (_initialized)
                _visualQuery.Dispose();
        }
    }
}
