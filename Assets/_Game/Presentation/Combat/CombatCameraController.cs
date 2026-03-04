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

        [Header("Vertical Offset")]
        [Tooltip("Camera Y offset so the hero (at y=-1.7) sits at ~25% screen height")]
        [SerializeField] private float _cameraYOffset = 1.5f;

        [Header("Viewport")]
        [SerializeField] private float _viewportWidth = 1f / 3f;
        [SerializeField] private float _nearClip = 0.1f;
        [SerializeField] private float _farClip = 100f;
        [SerializeField] private float _defaultZ = -10f;

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

            var pos = _cam.transform.position;
            if (Mathf.Abs(pos.y - _cameraYOffset) > 0.001f)
            {
                pos.y = Mathf.Lerp(pos.y, _cameraYOffset, Time.deltaTime * _smoothSpeed);
                _cam.transform.position = pos;
            }
        }

        private bool EnsureCamera()
        {
            if (_ready) return true;

            _cam = Camera.main;
            if (_cam == null || !_cam.orthographic) return false;

            _cam.transform.position = new Vector3(0f, _cameraYOffset, _defaultZ);
            _cam.rect = new Rect(0f, 0f, _viewportWidth, 1f);
            _cam.nearClipPlane = _nearClip;
            _cam.farClipPlane = _farClip;

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
