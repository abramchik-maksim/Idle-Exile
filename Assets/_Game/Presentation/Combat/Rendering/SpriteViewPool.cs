using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class SpriteViewPool : IDisposable
    {
        private readonly ICombatVisualProvider _visualProvider;
        private readonly Dictionary<int, Queue<SpriteView>> _pools = new();
        private readonly Transform _poolRoot;

        public SpriteViewPool(ICombatVisualProvider visualProvider)
        {
            _visualProvider = visualProvider;
            var rootGo = new GameObject("[SpriteViewPool]");
            rootGo.SetActive(false);
            _poolRoot = rootGo.transform;
        }

        public SpriteView Get(int visualId)
        {
            if (_pools.TryGetValue(visualId, out var queue) && queue.Count > 0)
            {
                var view = queue.Dequeue();
                view.VisualId = visualId;
                view.transform.SetParent(null);
                view.Activate();
                return view;
            }

            var prefab = _visualProvider.GetPrefab(visualId);
            if (prefab == null)
            {
                Debug.LogWarning($"[SpriteViewPool] No prefab for visualId {visualId}, creating fallback.");
                return CreateFallback(visualId);
            }

            var instance = UnityEngine.Object.Instantiate(prefab);
            var spriteView = instance.GetComponent<SpriteView>();
            if (spriteView == null)
                spriteView = instance.AddComponent<SpriteView>();

            float scale = _visualProvider.GetScale(visualId);
            if (scale > 0f && Mathf.Abs(scale - 1f) > 0.001f)
                instance.transform.localScale = new Vector3(scale, scale, 1f);

            spriteView.VisualId = visualId;
            return spriteView;
        }

        public void Return(SpriteView view)
        {
            if (view == null) return;

            view.Deactivate();
            view.transform.SetParent(_poolRoot);

            if (!_pools.TryGetValue(view.VisualId, out var queue))
            {
                queue = new Queue<SpriteView>();
                _pools[view.VisualId] = queue;
            }
            queue.Enqueue(view);
        }

        public void Dispose()
        {
            foreach (var kvp in _pools)
            {
                foreach (var view in kvp.Value)
                {
                    if (view != null)
                        UnityEngine.Object.Destroy(view.gameObject);
                }
            }
            _pools.Clear();

            if (_poolRoot != null)
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
        }

        private SpriteView CreateFallback(int visualId)
        {
            var go = new GameObject($"SpriteView_Fallback_{visualId}");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.magenta;
            var view = go.AddComponent<SpriteView>();
            view.VisualId = visualId;
            return view;
        }
    }
}
