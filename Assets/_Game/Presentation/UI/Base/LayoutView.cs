using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Presentation.UI.Base
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class LayoutView : MonoBehaviour, IDisposable
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private bool _visibleOnStart = true;
        [SerializeField] private int _sortOrder;

        protected UIDocument UIDocument => _uiDocument;
        protected VisualElement Root { get; private set; }
        public bool IsVisible => Root?.style.display != DisplayStyle.None;

        protected virtual void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();

            _uiDocument.sortingOrder = _sortOrder;
        }

        protected virtual void Start()
        {
            Root = _uiDocument.rootVisualElement;
            OnBind();

            if (!_visibleOnStart)
                Hide();
        }

        /// <summary>
        /// Called once after Root is ready. Query UXML elements and set up callbacks here.
        /// </summary>
        protected abstract void OnBind();

        public virtual void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            OnShow();
        }

        public virtual void Hide()
        {
            Root.style.display = DisplayStyle.None;
            OnHide();
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        protected T Q<T>(string name = null, string className = null) where T : VisualElement =>
            Root.Q<T>(name, className);

        protected VisualElement Q(string name = null, string className = null) =>
            Root.Q(name, className);

        public virtual void Dispose() { }

        protected virtual void OnDestroy() => Dispose();
    }
}
