using UnityEngine;

namespace Game.Presentation.Combat.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Animator _animator;

        public SpriteRenderer Renderer => _renderer;
        public Animator Animator => _animator;
        public int VisualId { get; set; }

        public void SetPosition(float x, float y)
        {
            transform.position = new Vector3(x, y, 0f);
        }

        public void SetFlipX(bool flip)
        {
            _renderer.flipX = flip;
        }

        public void SetSortingOrder(int order)
        {
            _renderer.sortingOrder = order;
        }

        public void SetRotation(float angleDeg)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        public void PlayTrigger(string trigger)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.SetTrigger(trigger);
        }

        public void SetBool(string param, bool value)
        {
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.SetBool(param, value);
        }

        public void Activate()
        {
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
        }
    }
}
