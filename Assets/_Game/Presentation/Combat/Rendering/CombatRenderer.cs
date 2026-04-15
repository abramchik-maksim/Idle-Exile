using UnityEngine;
using Game.Application.Ports;

namespace Game.Presentation.Combat.Rendering
{
    /// <summary>
    /// Legacy shim kept for scene hierarchy and DI compatibility.
    /// Entity rendering is handled by CombatVisualManager (SpriteRenderer GameObjects).
    /// Overlay rendering (HP bars, AoE, status icons) is handled by CombatOverlayRenderer.
    /// This component delegates ICombatDisplaySettings to the sibling CombatOverlayRenderer.
    /// </summary>
    public sealed class CombatRenderer : MonoBehaviour, ICombatDisplaySettings
    {
        private CombatOverlayRenderer _overlay;

        public bool ShowDamageNumbers
        {
            get => GetOverlay().ShowDamageNumbers;
            set => GetOverlay().ShowDamageNumbers = value;
        }

        public bool ShowHpBars
        {
            get => GetOverlay().ShowHpBars;
            set => GetOverlay().ShowHpBars = value;
        }

        public bool ShowEffectIndicators
        {
            get => GetOverlay().ShowEffectIndicators;
            set => GetOverlay().ShowEffectIndicators = value;
        }

        private CombatOverlayRenderer GetOverlay()
        {
            if (_overlay == null)
                _overlay = GetComponent<CombatOverlayRenderer>();
            return _overlay;
        }
    }
}
