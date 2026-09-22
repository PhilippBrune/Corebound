using Corebound.Gameplay.Shared;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.MetaProgression
{
    // SkillTree.md, Ascension: "a visible prestige marker (a rank badge in the main menu)" - owner
    // decision 2026-09-22: yes. Shown only once the first rank is bought, so it marks the players
    // who have finished the tree rather than being an empty "Rank 0" everyone sees.
    //
    // Its own overlay canvas in the top-right corner rather than an element of MainMenu.unity, so
    // the scene's authored layout is untouched. Asset 88 (ascension_rank_badge.png) is picked up by
    // path when it exists; until then the badge is a procedural medallion.
    public sealed class AscensionRankBadgePresenter : MonoBehaviour
    {
        private const string BadgeTexturePath = "Assets/Art/UI/SkillTree/ascension_rank_badge.png";
        private const float BadgeSize = 96f;

        [SerializeField] private MetaProgressionService _service;

        private CanvasGroup _group;
        private Text _rankText;
        private Text _labelText;
        private int _renderedRank = -1;

        public void Bind(MetaProgressionService service)
        {
            _service = service;
        }

        private void Start()
        {
            if (_service == null)
            {
                _service = GetComponent<MetaProgressionService>();
            }

            Build();
        }

        private void Update()
        {
            if (_service == null || _group == null)
            {
                return;
            }

            var rank = _service.AscensionRank;

            if (rank == _renderedRank)
            {
                return;
            }

            _renderedRank = rank;
            _group.alpha = rank > 0 ? 1f : 0f;
            _rankText.text = rank.ToString();
            _labelText.text = LocalizationService.Resolve("Ascension", "Aufstieg");
        }

        private void Build()
        {
            var canvasObject = new GameObject("AscensionBadgeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var root = canvasObject.GetComponent<RectTransform>();
            var holder = new GameObject("AscensionBadge", typeof(RectTransform), typeof(CanvasGroup));
            var holderRect = holder.GetComponent<RectTransform>();
            holderRect.SetParent(root, false);
            // Top-right: the top-left corner is the COREBOUND logo (first autopilot screenshot).
            holderRect.anchorMin = new Vector2(1f, 1f);
            holderRect.anchorMax = new Vector2(1f, 1f);
            holderRect.pivot = new Vector2(1f, 1f);
            holderRect.anchoredPosition = new Vector2(-32f, -32f);
            holderRect.sizeDelta = new Vector2(BadgeSize + 180f, BadgeSize);
            _group = holder.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var art = DebugSpriteFactory.LoadProjectSprite(BadgeTexturePath);
            var medallion = ProceduralUIFactory.CreateImage("Medallion", holderRect, art != null ? art : CreateDiscSprite(), art != null ? Color.white : new Color(0.95f, 0.72f, 0.28f, 1f), Image.Type.Simple);
            medallion.preserveAspect = true;
            medallion.raycastTarget = false;
            var medallionRect = medallion.rectTransform;
            medallionRect.anchorMin = new Vector2(0f, 0.5f);
            medallionRect.anchorMax = new Vector2(0f, 0.5f);
            medallionRect.pivot = new Vector2(0f, 0.5f);
            medallionRect.sizeDelta = new Vector2(BadgeSize, BadgeSize);

            var font = UIFontResolver.Resolve();
            _rankText = ProceduralUIFactory.CreateText("Rank", medallionRect, font, 38, FontStyle.Bold, new Color(0.12f, 0.08f, 0.04f, 1f), TextAnchor.MiddleCenter);
            _rankText.rectTransform.anchorMin = Vector2.zero;
            _rankText.rectTransform.anchorMax = Vector2.one;
            _rankText.rectTransform.offsetMin = Vector2.zero;
            _rankText.rectTransform.offsetMax = Vector2.zero;

            _labelText = ProceduralUIFactory.CreateText("Label", holderRect, font, 24, FontStyle.Bold, new Color(1f, 0.85f, 0.5f, 1f), TextAnchor.MiddleLeft);
            _labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _labelText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _labelText.rectTransform.offsetMin = new Vector2(BadgeSize + 14f, 0f);
            _labelText.rectTransform.offsetMax = Vector2.zero;
            var shadow = _labelText.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        }

        private static Sprite CreateDiscSprite()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    var alpha = Mathf.Clamp01(center - distance);
                    var rim = distance > center - 7f ? 0.7f : 1f;
                    texture.SetPixel(x, y, new Color(rim, rim, rim, alpha));
                }
            }

            texture.Apply();
            texture.hideFlags = HideFlags.DontSave;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
