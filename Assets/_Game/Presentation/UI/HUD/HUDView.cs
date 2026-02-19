using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.HUD
{
    public sealed class HUDView : LayoutView
    {
        private Label _waveLabel;
        private VisualElement _healthFill;
        private Button _btnInventory;
        private Button _btnCharacter;
        private Button _btnCheats;

        public event System.Action OnInventoryClicked;
        public event System.Action OnCharacterClicked;
        public event System.Action OnCheatsClicked;

        protected override void OnBind()
        {
            _waveLabel = Q<Label>("wave-label");
            _healthFill = Q("hero-health-fill");
            _btnInventory = Q<Button>("btn-inventory");
            _btnCharacter = Q<Button>("btn-character");
            _btnCheats = Q<Button>("btn-cheats");

            _btnInventory.clicked += () => OnInventoryClicked?.Invoke();
            _btnCharacter.clicked += () => OnCharacterClicked?.Invoke();
            _btnCheats.clicked += () => OnCheatsClicked?.Invoke();
        }

        public void SetWave(int wave) =>
            _waveLabel.text = $"Wave {wave}";

        public void SetHealthPercent(float normalized)
        {
            float pct = UnityEngine.Mathf.Clamp01(normalized) * 100f;
            _healthFill.style.width = new Length(pct, LengthUnit.Percent);
        }
    }
}
