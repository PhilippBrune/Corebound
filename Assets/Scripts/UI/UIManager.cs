using System.Collections.Generic;
using UnityEngine;

namespace Corebound.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        [SerializeField] private PanelDefinitionData[] _panelDefinitions;
        [SerializeField] private NotificationStyleData _defaultNotificationStyle;
        [SerializeField] private UIThemeData _defaultTheme;

        private readonly List<UIManagedPanel> _registeredPanels = new List<UIManagedPanel>();
        private int _registrationSeed;

        public static UIManager Instance => ResolveInstance();

        public NotificationStyleData DefaultNotificationStyle => _defaultNotificationStyle;

        public UIThemeData DefaultTheme => _defaultTheme;

        public bool HasBlockingPanelVisible
        {
            get
            {
                for (var index = 0; index < _registeredPanels.Count; index++)
                {
                    var panel = _registeredPanels[index];

                    if (panel != null && panel.BlocksInput && panel.IsVisible)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        public static UIManagedPanel RegisterPanel(RectTransform root, string panelId, UIPanelLayer layer, bool blocksInput, bool isModal, CanvasGroup canvasGroup = null)
        {
            return Instance.RegisterPanelInternal(root, panelId, layer, blocksInput, isModal, canvasGroup);
        }

        public static void NotifyPanelChanged(UIManagedPanel panel)
        {
            if (_instance == null || panel == null)
            {
                return;
            }

            _instance.ApplyPanelOrdering();
        }

        public static void UnregisterPanel(UIManagedPanel panel)
        {
            if (_instance == null || panel == null)
            {
                return;
            }

            _instance._registeredPanels.Remove(panel);
            _instance.ApplyPanelOrdering();
        }

        private UIManagedPanel RegisterPanelInternal(RectTransform root, string panelId, UIPanelLayer layer, bool blocksInput, bool isModal, CanvasGroup canvasGroup)
        {
            if (root == null)
            {
                return null;
            }

            var resolvedLayer = layer;
            var resolvedBlocksInput = blocksInput;
            var resolvedModal = isModal;
            var definition = ResolveDefinition(panelId);

            if (definition != null)
            {
                resolvedLayer = definition.DefaultLayer;
                resolvedBlocksInput = definition.BlocksInput;
                resolvedModal = definition.IsModal;
            }

            var panel = root.GetComponent<UIManagedPanel>();

            if (panel == null)
            {
                panel = root.gameObject.AddComponent<UIManagedPanel>();
            }

            if (!_registeredPanels.Contains(panel))
            {
                _registeredPanels.Add(panel);
            }

            panel.Configure(panelId, resolvedLayer, resolvedBlocksInput, resolvedModal, root, canvasGroup, _registrationSeed++);
            ApplyPanelOrdering();
            return panel;
        }

        private void ApplyPanelOrdering()
        {
            _registeredPanels.Sort(ComparePanels);

            for (var index = 0; index < _registeredPanels.Count; index++)
            {
                var panel = _registeredPanels[index];

                if (panel == null)
                {
                    continue;
                }

                panel.ApplySortingOrder(ResolveLayerBaseOrder(panel.Layer) + index);
            }
        }

        private PanelDefinitionData ResolveDefinition(string panelId)
        {
            if (_panelDefinitions == null || string.IsNullOrWhiteSpace(panelId))
            {
                return null;
            }

            for (var index = 0; index < _panelDefinitions.Length; index++)
            {
                var definition = _panelDefinitions[index];

                if (definition != null && definition.PanelId == panelId)
                {
                    return definition;
                }
            }

            return null;
        }

        private static int ComparePanels(UIManagedPanel left, UIManagedPanel right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            var layerComparison = left.Layer.CompareTo(right.Layer);

            if (layerComparison != 0)
            {
                return layerComparison;
            }

            return left.RegistrationOrder.CompareTo(right.RegistrationOrder);
        }

        private static int ResolveLayerBaseOrder(UIPanelLayer layer)
        {
            switch (layer)
            {
                case UIPanelLayer.Background:
                    return 0;
                case UIPanelLayer.HUD:
                    return 100;
                case UIPanelLayer.Overlay:
                    return 200;
                case UIPanelLayer.BlockingOverlay:
                    return 300;
                case UIPanelLayer.Menu:
                    return 400;
                case UIPanelLayer.System:
                    return 500;
                default:
                    return 1000;
            }
        }

        private static UIManager ResolveInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            _instance = FindFirstObjectByType<UIManager>();

            if (_instance != null)
            {
                return _instance;
            }

            var managerObject = new GameObject("UIManager");
            _instance = managerObject.AddComponent<UIManager>();
            return _instance;
        }
    }
}