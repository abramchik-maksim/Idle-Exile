using System;
using TMPro;
using UnityEngine;

namespace Game.Presentation.Combat.Rendering
{
    public sealed class DamageNumber : MonoBehaviour
    {
        private TextMeshPro _text;
        private float _timer;
        private float _duration;
        private Vector3 _startPos;
        private Color _startColor;
        private Action<DamageNumber> _onComplete;

        private const float FloatHeight = 1.5f;

        public void Initialize()
        {
            _text = gameObject.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.sortingOrder = 100;
            _text.fontSize = 5f;
            _text.fontStyle = FontStyles.Bold;
            gameObject.SetActive(false);
        }

        public void Show(Vector3 position, float amount, bool isCrit, float duration, Action<DamageNumber> onComplete)
        {
            _startPos = position;
            _duration = duration;
            _timer = 0f;
            _onComplete = onComplete;

            transform.position = position;

            _text.text = Mathf.RoundToInt(amount).ToString();
            _text.fontSize = isCrit ? 7f : 5f;
            _startColor = isCrit ? new Color(1f, 0.85f, 0.1f) : Color.white;
            _text.color = _startColor;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float t = _timer / _duration;

            transform.position = _startPos + Vector3.up * (t * FloatHeight);

            var c = _startColor;
            c.a = 1f - t;
            _text.color = c;

            if (t >= 1f)
            {
                gameObject.SetActive(false);
                _onComplete?.Invoke(this);
            }
        }
    }
}
