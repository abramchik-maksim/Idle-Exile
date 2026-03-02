using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Presentation.UI.Combat
{
    public sealed class RadialFillElement : VisualElement
    {
        private float _fillAmount;

        public float FillAmount
        {
            get => _fillAmount;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(_fillAmount, clamped)) return;
                _fillAmount = clamped;
                MarkDirtyRepaint();
            }
        }

        public RadialFillElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_fillAmount <= 0.001f) return;

            var painter = mgc.painter2D;
            var rect = contentRect;
            var center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

            float sweepDeg = _fillAmount * 360f;
            float startAngle = -90f;
            float endAngle = startAngle + sweepDeg;

            painter.fillColor = new Color(0f, 0f, 0f, 0.55f);
            painter.BeginPath();
            painter.MoveTo(center);
            painter.Arc(center, radius, startAngle, endAngle, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
