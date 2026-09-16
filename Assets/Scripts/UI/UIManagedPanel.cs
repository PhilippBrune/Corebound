using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI
{
    public sealed class UIManagedPanel : MonoBehaviour
    {
        [SerializeField] private string _panelId = "panel";
        [SerializeField] private UIPanelLayer _layer = UIPanelLayer.Overlay;
        [SerializeField] private bool _blocksInput;
        [SerializeField] private bool _isModal;

        private RectTransform _root;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private int _registrationOrder;

        public string PanelId => _panelId;

        public UIPanelLayer Layer => _layer;

        public bool BlocksInput => _blocksInput;

        public bool IsModal => _isModal;

        public int RegistrationOrder => _registrationOrder;

        public bool IsVisible
        {
            get
            {
                if (_canvasGroup != null)
                {
                    return _canvasGroup.gameObject.activeInHierarchy && _canvasGroup.alpha > 0.001f;
                }

                return _root != null ? _root.gameObject.activeInHierarchy : gameObject.activeInHierarchy;
            }
        }

        public void Configure(string panelId, UIPanelLayer layer, bool blocksInput, bool isModal, RectTransform root, CanvasGroup canvasGroup, int registrationOrder)
        {
            _panelId = string.IsNullOrWhiteSpace(panelId) ? _panelId : panelId;
            _layer = layer;
            _blocksInput = blocksInput;
            _isModal = isModal;
            _root = root != null ? root : transform as RectTransform;
            _canvasGroup = canvasGroup;
            _registrationOrder = registrationOrder;
            EnsureCanvas();
        }

        public void ApplySortingOrder(int sortingOrder)
        {
            EnsureCanvas();

            if (_canvas == null)
            {
                return;
            }

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
        }

        private void EnsureCanvas()
        {
            var targetGameObject = _root != null ? _root.gameObject : gameObject;
            _canvas = targetGameObject.GetComponent<Canvas>();

            if (_canvas == null)
            {
                _canvas = targetGameObject.AddComponent<Canvas>();
            }

            var parentCanvas = targetGameObject.transform.parent != null ? targetGameObject.transform.parent.GetComponentInParent<Canvas>() : null;

            if (parentCanvas != null && parentCanvas != _canvas)
            {
                _canvas.renderMode = parentCanvas.renderMode;
                _canvas.worldCamera = parentCanvas.worldCamera;
                _canvas.planeDistance = parentCanvas.planeDistance;
            }
            else
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            _canvas.overrideSorting = true;

            if (_blocksInput && targetGameObject.GetComponent<GraphicRaycaster>() == null)
            {
                targetGameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
}