using UnityEngine;

namespace Game.Presentation.Combat
{
    public sealed class CombatCameraController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minOrthoSize = 2f;
        [SerializeField] private float _maxOrthoSize = 12f;
        [SerializeField] private float _smoothSpeed = 10f;

        private Camera _cam;
        private float _targetOrthoSize;
        private bool _ready;

        private void Update()
        {
            if (!EnsureCamera()) return;

            float scroll = Input.mouseScrollDelta.y;

            if (scroll == 0f) scroll = Input.GetAxis("Mouse ScrollWheel") * 10f;

            if (Mathf.Abs(scroll) > 0.001f && IsMouseOverCombatArea())
            {
                _targetOrthoSize -= scroll * _zoomSpeed;
                _targetOrthoSize = Mathf.Clamp(_targetOrthoSize, _minOrthoSize, _maxOrthoSize);
            }

            if (Mathf.Abs(_cam.orthographicSize - _targetOrthoSize) > 0.001f)
            {
                _cam.orthographicSize = Mathf.Lerp(
                    _cam.orthographicSize,
                    _targetOrthoSize,
                    Time.deltaTime * _smoothSpeed
                );
            }
        }

        private bool EnsureCamera()
        {
            if (_ready) return true;

            _cam = Camera.main;
            if (_cam == null || !_cam.orthographic) return false;

            _targetOrthoSize = _cam.orthographicSize;
            _ready = true;
            return true;
        }

        private bool IsMouseOverCombatArea()
        {
            float normalizedX = Input.mousePosition.x / Screen.width;
            return normalizedX <= _cam.rect.xMax + 0.01f;
        }
    }
}
