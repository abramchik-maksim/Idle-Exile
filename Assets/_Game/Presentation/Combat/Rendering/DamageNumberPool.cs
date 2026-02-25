using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class DamageNumberPool : MonoBehaviour
    {
        [SerializeField] private int _poolSize = 50;
        [SerializeField] private float _duration = 0.8f;

        private readonly Queue<DamageNumber> _available = new();
        private readonly List<DamageNumber> _active = new();

        private void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var go = new GameObject($"DamageNumber_{i}");
                go.transform.SetParent(transform);
                var dn = go.AddComponent<DamageNumber>();
                dn.Initialize();
                _available.Enqueue(dn);
            }
        }

        public void Show(Vector3 worldPosition, float amount, bool isCritical)
        {
            DamageNumber dn;

            if (_available.Count > 0)
            {
                dn = _available.Dequeue();
            }
            else if (_active.Count > 0)
            {
                dn = _active[0];
                _active.RemoveAt(0);
                dn.gameObject.SetActive(false);
            }
            else
            {
                return;
            }

            _active.Add(dn);

            var offset = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.2f, 0.5f),
                0f
            );

            dn.Show(worldPosition + offset, amount, isCritical, _duration, ReturnToPool);
        }

        private void ReturnToPool(DamageNumber dn)
        {
            _active.Remove(dn);
            _available.Enqueue(dn);
        }
    }
}
