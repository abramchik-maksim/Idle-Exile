using System;
using Game.Domain.Items;
using UnityEngine.UIElements;
using Game.Presentation.UI.Base;

namespace Game.Presentation.UI.MainMenu
{
    public sealed class CharacterSelectView : LayoutView
    {
        private VisualElement _root;
        private Button _btnWarrior;
        private Button _btnRogue;
        private Button _btnCaster;
        private Label _descriptionLabel;
        private Button _btnStart;
        private Button _btnBack;

        public event Action<HeroItemClass> OnClassSelected;
        public event Action OnStartClicked;
        public event Action OnBackClicked;

        protected override void OnBind()
        {
            _root = Q("character-select-screen");
            _btnWarrior = Q<Button>("btn-char-warrior");
            _btnRogue = Q<Button>("btn-char-rogue");
            _btnCaster = Q<Button>("btn-char-caster");
            _descriptionLabel = Q<Label>("character-description");
            _btnStart = Q<Button>("btn-char-start");
            _btnBack = Q<Button>("btn-char-back");

            _btnWarrior.clicked += () => OnClassSelected?.Invoke(HeroItemClass.Warrior);
            _btnRogue.clicked += () => OnClassSelected?.Invoke(HeroItemClass.Rogue);
            _btnCaster.clicked += () => OnClassSelected?.Invoke(HeroItemClass.Caster);
            _btnStart.clicked += () => OnStartClicked?.Invoke();
            _btnBack.clicked += () => OnBackClicked?.Invoke();

            SetStartEnabled(false);
        }

        public void ShowScreen() => _root.style.display = DisplayStyle.Flex;
        public void HideScreen() => _root.style.display = DisplayStyle.None;

        public void SetStartEnabled(bool enabled) => _btnStart.SetEnabled(enabled);

        public void SetSelection(HeroItemClass selectedClass, string description)
        {
            _btnWarrior.EnableInClassList("tab-btn--active", selectedClass == HeroItemClass.Warrior);
            _btnRogue.EnableInClassList("tab-btn--active", selectedClass == HeroItemClass.Rogue);
            _btnCaster.EnableInClassList("tab-btn--active", selectedClass == HeroItemClass.Caster);
            _descriptionLabel.text = description ?? string.Empty;
        }
    }
}
