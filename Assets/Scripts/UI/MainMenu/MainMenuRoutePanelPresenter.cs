using System.Collections.Generic;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.MainMenu
{
    public sealed class MainMenuRoutePanelPresenter : MonoBehaviour
    {
        private const float PanelWidth = 980f;
        // button_primary_frame.png has a 220px/140px (horiz/vert) 9-slice border baked into the
        // art itself; 650 left too little safe vertical space once title+subtitle+summary+detail
        // lines+footer are all accounted for, so title through footer are laid out below with
        // insets that clear that border instead of rendering across it.
        private const float PanelHeight = 750f;
        private const int TitleFontSize = 42;
        private const int SubtitleFontSize = 24;
        private const int SummaryFontSize = 24;
        private const int DetailFontSize = 22;
        private const int FooterFontSize = 20;

        private readonly List<Text> _detailTexts = new List<Text>();

        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private UnityEngine.Object _routeTargetControllerSource;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private RectTransform _overlayRect;
        private RectTransform _panelRect;
        private Text _titleText;
        private Text _subtitleText;
        private Text _summaryText;
        private Text _footerText;
        private VerticalLayoutGroup _detailLayout;
        private Font _font;
        private Sprite _frameSprite;
        private UIManagedPanel _managedPanel;

        private IMainMenuRouteTargetController RouteTargetController => _routeTargetControllerSource as IMainMenuRouteTargetController;

        private void Awake()
        {
            _font = ResolveFont();
            _frameSprite = CreateRuntimeSprite(_skinData != null ? _skinData.ButtonFrameTexture : null, _skinData != null ? _skinData.ButtonFrameBorder : Vector4.zero);
            BuildPanel();
            RegisterWithUIManager();
            Refresh();
        }

        private void OnEnable()
        {
            if (RouteTargetController != null)
            {
                RouteTargetController.RouteTargetChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (RouteTargetController != null)
            {
                RouteTargetController.RouteTargetChanged -= Refresh;
            }
        }

        private void OnDestroy()
        {
            if (_frameSprite == null)
            {
                UIManager.UnregisterPanel(_managedPanel);
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_frameSprite);
            }
            else
            {
                DestroyImmediate(_frameSprite);
            }

            UIManager.UnregisterPanel(_managedPanel);
        }

        private void BuildPanel()
        {
            _presenterRoot = EnsurePresenterRoot("RoutePanelPresenterRoot");
            _canvasGroup = _presenterRoot.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = _presenterRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var overlay = CreateImage("RouteOverlay", _presenterRoot, null, new Color(0.02f, 0.04f, 0.06f, 0.78f), Image.Type.Simple);
            _overlayRect = overlay.rectTransform;
            StretchToParent(_overlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage("RoutePanel", _presenterRoot, _frameSprite, _skinData != null ? _skinData.SelectedFrameTint : Color.white, ResolvePanelImageType(_frameSprite));
            _panelRect = panel.rectTransform;
            SetAnchoredBox(_panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, PanelHeight));

            _titleText = CreateText("Title", _panelRect, TitleFontSize, FontStyle.Bold, _skinData != null ? _skinData.SelectedLabelColor : Color.white, TextAnchor.UpperLeft);
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -235f), new Vector2(-230f, -165f));

            _subtitleText = CreateText("Subtitle", _panelRect, SubtitleFontSize, FontStyle.Normal, _skinData != null ? _skinData.PromptLabelColor : Color.white, TextAnchor.UpperLeft);
            StretchToParent(_subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -280f), new Vector2(-230f, -245f));

            _summaryText = CreateText("Summary", _panelRect, SummaryFontSize, FontStyle.Normal, _skinData != null ? _skinData.StatusLabelColor : Color.white, TextAnchor.UpperLeft);
            // Summary strings here run long enough to wrap 2-3 lines (e.g. the New Run body) -
            // the previous 55px band was sized for one line and crowded straight into the detail
            // list below it with no visible gap.
            StretchToParent(_summaryText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(230f, -380f), new Vector2(-230f, -295f));

            var detailContainer = new GameObject("DetailContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            detailContainer.transform.SetParent(_panelRect, false);
            var detailRect = detailContainer.GetComponent<RectTransform>();
            StretchToParent(detailRect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(230f, 205f), new Vector2(-230f, -395f));

            _detailLayout = detailContainer.GetComponent<VerticalLayoutGroup>();
            _detailLayout.spacing = 14f;
            _detailLayout.childAlignment = TextAnchor.UpperLeft;
            _detailLayout.childControlHeight = true;
            _detailLayout.childControlWidth = true;
            _detailLayout.childForceExpandHeight = false;
            _detailLayout.childForceExpandWidth = true;

            _footerText = CreateText("Footer", _panelRect, FooterFontSize, FontStyle.Italic, _skinData != null ? _skinData.IdleLabelColor : Color.white, TextAnchor.LowerRight);
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(230f, 140f), new Vector2(-230f, 195f));
        }

        private void Refresh()
        {
            var panelData = RouteTargetController != null ? RouteTargetController.ActivePanelData : null;
            var isVisible = panelData != null;

            EnsureForegroundOrder();

            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;

            if (!isVisible)
            {
                return;
            }

            _titleText.text = panelData.Title;
            _subtitleText.text = panelData.Subtitle;
            _summaryText.text = panelData.Summary;
            _footerText.text = panelData.FooterHint;
            RebuildDetailLines(panelData.DetailLines);
            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private void EnsureForegroundOrder()
        {
            if (_overlayRect != null)
            {
                _overlayRect.SetAsLastSibling();
            }

            if (_panelRect != null)
            {
                _panelRect.SetAsLastSibling();
            }
        }

        private void RebuildDetailLines(string[] detailLines)
        {
            _detailTexts.Clear();

            if (_detailLayout == null)
            {
                return;
            }

            for (var index = _detailLayout.transform.childCount - 1; index >= 0; index--)
            {
                var child = _detailLayout.transform.GetChild(index);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            if (detailLines == null)
            {
                return;
            }

            for (var index = 0; index < detailLines.Length; index++)
            {
                var detailText = CreateText("Detail" + index, _detailLayout.transform as RectTransform, DetailFontSize, FontStyle.Normal, _skinData != null ? _skinData.IdleLabelColor : Color.white, TextAnchor.UpperLeft);
                detailText.text = "- " + detailLines[index];
                var layoutElement = detailText.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 44f;
                _detailTexts.Add(detailText);
            }
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, fontStyle, color, anchor);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color, imageType);
        }

        private static Image.Type ResolvePanelImageType(Sprite sprite)
        {
            return sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        private RectTransform EnsurePresenterRoot(string childName)
        {
            var childTransform = transform.Find(childName);

            if (childTransform == null)
            {
                var childObject = new GameObject(childName, typeof(RectTransform));
                childObject.transform.SetParent(transform, false);
                childTransform = childObject.transform;
            }

            var rectTransform = childTransform as RectTransform;

            if (rectTransform == null)
            {
                rectTransform = childTransform.gameObject.AddComponent<RectTransform>();
            }

            StretchToParent(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rectTransform;
        }

        private static Sprite CreateRuntimeSprite(Texture2D texture, Vector4 border)
        {
            if (texture == null)
            {
                return null;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect,
                border);
            sprite.hideFlags = HideFlags.DontSaveInEditor;
            return sprite;
        }

        private static Font ResolveFont()
        {
            return UIFontResolver.Resolve();
        }

        private static void StretchToParent(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static void SetAnchoredBox(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(anchor.x, anchor.y);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

        private void RegisterWithUIManager()
        {
            _managedPanel = UIManager.RegisterPanel(_presenterRoot, "main_menu_route_panel", UIPanelLayer.BlockingOverlay, true, true, _canvasGroup);
        }
    }
}