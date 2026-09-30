using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Shared;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Corebound.Gameplay.Shop
{
    [ExecuteAlways]
    public sealed class ShopPresenter : MonoBehaviour
    {
        private const string PanelTexturePath = "Assets/Art/UI/HUD/shop_offer_panel.png";
        private const string PanelTextureFallbackPath = "Assets/Art/UI/HUD/shop_offer_panel.png";
        private const string PriceChipTexturePath = "Assets/Art/UI/HUD/shop_price_chip.png";
        private const string CommonFramePath = "Assets/Art/Cards/Frames/card_frame_common.png";
        private const string RareFramePath = "Assets/Art/Cards/Frames/card_frame_rare.png";
        private const string EpicFramePath = "Assets/Art/Cards/Frames/card_frame_epic.png";
        private const string LegendaryFramePath = "Assets/Art/Cards/Frames/card_frame_legendary.png";
        private const string TowerUpgradeIconPath = "Assets/Art/UI/HUD/shop_tower_upgrade_icon.png";
        // UI proposal 1 (playtest 2026-09-21: "make it similar to the Reward screen"): the reward
        // screen's slot geometry exactly (CardRewardPresenter / RewardCanvas.unity), so a player
        // does not re-learn a layout between the two panels. ShopCanvas.unity carries the same
        // values; these only matter for the procedural fallback and the editor bake below.
        private const float SlotWidth = 264f;
        private const float SlotHeight = 336f;
        private const float SlotSpacing = 291f;
        private const float SlotCenterY = -17f;
        private static readonly Vector2 SlotTitlePosition = new Vector2(0f, 114f);
        private static readonly Vector2 SlotTitleSize = new Vector2(165f, 44f);
        private static readonly Vector2 SlotDescriptionPosition = new Vector2(0f, -26f);
        private static readonly Vector2 SlotDescriptionSize = new Vector2(158f, 212f);

        // The reward screen's bottom band only holds a hidden rarity line, so the price chip sits
        // there: the one place in the shared geometry with room for information the reward screen
        // has no equivalent of.
        private static readonly Vector2 SlotCostPosition = new Vector2(0f, -148f);
        private static readonly Vector2 SlotCostChipSize = new Vector2(126f, 34f);
        private static readonly Vector2 SlotCostTextSize = new Vector2(110f, 30f);
        private const float TypeBadgeSize = 40f;

        private readonly RectTransform[] _slotRoots = new RectTransform[3];
        private readonly Image[] _slotFrameImages = new Image[3];
        private readonly Image[] _slotCostChipImages = new Image[3];
        private readonly Image[] _slotHighlights = new Image[3];
        private readonly Image[] _slotTypeBadgeImages = new Image[3];
        private readonly Text[] _slotTitleTexts = new Text[3];
        private readonly Text[] _slotCostTexts = new Text[3];
        private readonly Text[] _slotDescriptionTexts = new Text[3];

        [SerializeField] private ShopService _service;
        // Deliberately NOT [SerializeField] - see CardRewardPresenter's identical fields for why:
        // cross-scene references (ShopCanvas.unity is a separate scene), always re-resolved fresh
        // via GameObject.Find() in TryBindAuthoredLayout(), and [SerializeField] made Unity log
        // "Cross scene references are not supported" and null the field out on domain reload/exit
        // Play Mode.
        private RectTransform _authoredPresenterRoot;
        private CanvasGroup _authoredCanvasGroup;
        private Text _authoredTitleText;
        private Text _authoredSummaryText;
        private Text _authoredResourceText;
        private Text _authoredFooterText;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private Text _titleText;
        private Text _summaryText;
        private Text _resourceText;
        private Text _footerText;
        private Font _font;
        private Texture2D _runtimeLoadedPanelTexture;
        private Texture2D _runtimeLoadedTowerUpgradeIconTexture;
        private Sprite _panelSprite;
        private Sprite _priceChipSprite;
        private Sprite _commonFrameSprite;
        private Sprite _rareFrameSprite;
        private Sprite _epicFrameSprite;
        private Sprite _legendaryFrameSprite;
        private Sprite _towerUpgradeIconSprite;
        private UIManagedPanel _managedPanel;

        private static readonly Color ShopOverlayColor = new Color(0.07f, 0.045f, 0.02f, 0.9f);

        private const string ShopCanvasName = "ShopCanvas";
        private const string ShopRootName = "ShopScreen";
        private const string ShopPanelName = "ShopOfferPanel";

        // Entry point for Assets/Editor/AuthoredCanvasRepairTool.cs - see CardRewardPresenter's
        // identical method for why this exists.
        public bool RepairAuthoredLayoutForEditorTool()
        {
            return TryBindAuthoredLayout();
        }

        // See CardRewardPresenter.Awake() for the full root-cause writeup (2026-09-16 playtest:
        // reward/shop screens broken via MainMenu -> New Run, fine when opening SampleScene
        // directly). Same bug applied here: calling the fallback-capable EnsureInitialized() from
        // Awake() let ResolveOrBuildPanel()'s local-build fallback run before ShopCanvas.unity's
        // additive load completes, permanently satisfying EnsureInitialized()'s guard and
        // defeating Start()'s later retry. Awake() now only attempts a bind (safe no-op if the
        // canvas is not yet loaded).
        private void Awake()
        {
            if (_service == null)
            {
                _service = GetComponent<ShopService>();
            }

            TryBindAuthoredLayout();
            Refresh();
        }

        // The one place guaranteed to run after ShopCanvas.unity's additive load has completed -
        // see the Awake() comment above for why the fallback-capable EnsureInitialized() must not
        // be called any earlier than this.
        private void Start()
        {
            EnsureInitialized();
            Refresh();
        }

        // Guarded by a dedicated flag rather than _slotRoots[0] != null: Awake()/OnEnable() now
        // call TryBindAuthoredLayout() directly, and a successful early bind already populates
        // _slotRoots - guarding on that would make this whole method, including
        // RegisterWithUIManager(), silently skip forever once Start() runs. See
        // CardRewardPresenter.EnsureInitialized() for the full writeup, including why any guard
        // is needed here at all: _slotRoots and the other runtime fields below are plain
        // (non-serialized) fields, so a domain reload (script recompile, or entering Play Mode
        // with "Reload Domain" enabled) resets them, but Unity's backup/restore cycle re-invokes
        // OnEnable() on already-existing scene objects without calling Awake() again - without
        // this guard, the shop panel crashed the same way the reward panel did
        // (NullReferenceException on a null _slotRoots[index] the first time OnEnable()->Refresh()
        // ran after a reload).
        private bool _isFullyInitialized;

        private void EnsureInitialized()
        {
            if (_isFullyInitialized)
            {
                return;
            }

            _font = ResolveFont();
            LoadSprites();
            ResolveOrBuildPanel();
            RegisterWithUIManager();
            _isFullyInitialized = true;
        }

#if UNITY_EDITOR
        // ShopCanvas.unity is a separate authored scene now (Assets/Scenes/ShopCanvas.unity), not
        // something this presenter creates - OnValidate no longer builds anything (that used to
        // create the duplicate "ShopPresenterRoot" hierarchy this project had to clean up earlier).
        // It only retries the bind, a no-op once EnsureInitialized() already succeeded.
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += DeferredEnsureInitialized;
        }

        private void DeferredEnsureInitialized()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            EnsureInitialized();
            Refresh();
        }
#endif

        private void OnEnable()
        {
            // Same reasoning as Awake() above - OnEnable() runs in the same early phase, before
            // ShopCanvas.unity's additive load can complete on the MainMenu-transition path.
            TryBindAuthoredLayout();

            // ShopCanvas.unity finishes loading at the end of the frame the gameplay scene loaded
            // in, i.e. strictly after this component's Start(). Binding only in Start() therefore
            // always missed it on the MainMenu -> New Run path and the shop silently never
            // appeared. This is the callback that fires once its objects genuinely exist.
            AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;

            if (_service != null)
            {
                _service.StateChanged += Refresh;
            }

            Refresh();
        }

        // Mouse: hovering a slot selects it, clicking buys it. Ignored for the first half second
        // after the shop opens, so the click that ended the last wave cannot buy something.
        private const float ClickGuardSeconds = 0.5f;
        private Vector2 _lastPointer;

        private void Update()
        {
            if (_service == null || !_service.IsOpen || UnityEngine.InputSystem.Mouse.current == null)
            {
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pointer = mouse.position.ReadValue();
            var moved = (pointer - _lastPointer).sqrMagnitude > 4f;
            _lastPointer = pointer;
            var clicked = mouse.leftButton.wasPressedThisFrame && Time.unscaledTime - _service.OpenedAtUnscaledTime > ClickGuardSeconds;

            if (mouse.rightButton.wasPressedThisFrame && Time.unscaledTime - _service.OpenedAtUnscaledTime > ClickGuardSeconds)
            {
                _service.RequestSkip();
                return;
            }

            if (!moved && !clicked)
            {
                return;
            }

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                var slot = _slotRoots[index];

                if (slot == null || !slot.gameObject.activeInHierarchy || !RectTransformUtility.RectangleContainsScreenPoint(slot, pointer, null))
                {
                    continue;
                }

                _service.SelectIndex(index);

                if (clicked)
                {
                    _service.TryPurchaseSelectedOffer();
                }

                return;
            }
        }

        private void OnDisable()
        {
            AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;

            if (_service != null)
            {
                _service.StateChanged -= Refresh;
            }
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName != AdditiveCanvasSceneLoader.ShopCanvasSceneName)
            {
                return;
            }

            // Re-run the full initialize so RegisterWithUIManager() also happens against the real
            // authored panel rather than against whatever was resolved before it existed.
            _isFullyInitialized = false;
            EnsureInitialized();
            Refresh();
        }

        private void OnDestroy()
        {
            // The rarity frame sprites now come from DebugSpriteFactory's shared LoadProjectSpriteRect
            // cache (see LoadSprites) - destroying them here would leave a dangling reference in that
            // cache for the next presenter instance to hand out, same reasoning as
            // CardRewardPresenter.OnDestroy().
            DestroyRuntimeSprite(_panelSprite);
            DestroyRuntimeSprite(_towerUpgradeIconSprite);
            CleanupTexture(_runtimeLoadedPanelTexture);
            CleanupTexture(_runtimeLoadedTowerUpgradeIconTexture);
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void BuildPanel()
        {
            if (TryBindAuthoredLayout())
            {
                return;
            }

            _presenterRoot = EnsurePresenterRoot("ShopPresenterRoot");
            _canvasGroup = _presenterRoot.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = _presenterRoot.gameObject.AddComponent<CanvasGroup>();
            }

            // CreateImage/CreateText now find an existing same-named child before creating a new one
            // (see ProceduralUIFactory) specifically because this fallback path only runs when
            // TryBindAuthoredLayout() fails, but every [ExecuteAlways] domain reload (each script
            // recompile) re-runs Awake() - this project's ShopPresenterRoot accumulated six duplicate
            // Overlay+Panel pairs that way over one long editing session before the authored
            // ShopCanvas layout took over.
            // Shop.md: the shop screen must "sich vom Combat-Bild und von Card-Reward-Screens
            // unterscheiden". Both screens used the identical cool near-black wash, so the only
            // difference between them was the panel art. A warm amber ground reads as a
            // different place at a glance. Kept in sync with ShopCanvas.unity Overlay.
            var overlay = CreateImage("Overlay", _presenterRoot, null, ShopOverlayColor, Image.Type.Simple);
            StretchToParent(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panel = CreateImage("Panel", _presenterRoot, _panelSprite, Color.white, _panelSprite != null ? Image.Type.Sliced : Image.Type.Simple);
            var panelRect = panel.rectTransform;
            SetAnchoredBox(panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 680f));

            // Band positions measured directly off shop_offer_panel.png's pixel data - this art is
            // layout-identical to card_reward_choice_panel.png (same title bar, bright summary strip,
            // footer bar proportions), confirmed by sampling both and comparing. Summary is repurposed
            // to the footer band since what it actually displays (the selected offer's description,
            // e.g. "Increase Weapon damage...") is conceptually the same as CardReward's Description
            // text, not a top subtitle - keeping it in the old top position crowded it against
            // Resources/Footer and none of the three sat inside their actual bright/tan bands.
            _titleText = CreateText("Title", panelRect, 28, FontStyle.Bold, new Color(0.14f, 0.12f, 0.1f, 0.98f), TextAnchor.MiddleCenter);
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(180f, -125f), new Vector2(-180f, -54f));

            _summaryText = CreateText("Summary", panelRect, 18, FontStyle.Normal, new Color(0.16f, 0.14f, 0.11f, 0.96f), TextAnchor.MiddleCenter);
            StretchToParent(_summaryText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(287f, 71f), new Vector2(-289f, 133f));

            // Footer ("Confirm buys. Cancel skips.") gets the full bright strip directly under the
            // title, centered - per playtest feedback it needs to read as a hint banner for the
            // whole shop, not be squeezed into half a row next to Resources. Resources moves to a
            // compact top-left corner badge (the unused margin beside Title's centered band) instead.
            _resourceText = CreateText("Resources", panelRect, 18, FontStyle.Bold, new Color(0.18f, 0.16f, 0.12f, 0.96f), TextAnchor.MiddleLeft);
            StretchToParent(_resourceText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(174f, -40f));

            _footerText = CreateText("Footer", panelRect, 18, FontStyle.Italic, new Color(0.18f, 0.16f, 0.12f, 0.96f), TextAnchor.MiddleCenter);
            StretchToParent(_footerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(180f, -196f), new Vector2(-180f, -148f));

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                var existingSlot = panelRect.Find("ShopSlot" + index);
                var slotObject = existingSlot != null ? existingSlot.gameObject : new GameObject("ShopSlot" + index, typeof(RectTransform), typeof(Image));

                if (existingSlot == null)
                {
                    slotObject.transform.SetParent(panelRect, false);
                }

                var slotRect = slotObject.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.anchoredPosition = new Vector2((index - 1) * SlotSpacing, SlotCenterY);
                slotRect.sizeDelta = new Vector2(SlotWidth, SlotHeight);
                _slotRoots[index] = slotRect;

                var highlight = slotObject.GetComponent<Image>();
                highlight.color = Color.clear;
                highlight.raycastTarget = false;
                _slotHighlights[index] = highlight;

                var frameImage = CreateImage("SlotFrame", slotRect, _commonFrameSprite, Color.white, Image.Type.Simple);
                StretchToParent(frameImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _slotFrameImages[index] = frameImage;

                var titleText = CreateText("SlotTitle", slotRect, 18, FontStyle.Bold, new Color(0.14f, 0.12f, 0.1f, 0.98f), TextAnchor.UpperCenter);
                SetAnchoredBox(titleText.rectTransform, new Vector2(0.5f, 0.5f), SlotTitlePosition, SlotTitleSize);
                _slotTitleTexts[index] = titleText;

                var priceChip = CreateImage("SlotCostChip", slotRect, _priceChipSprite, Color.white, Image.Type.Simple);
                // The chip art's own aspect (~555x460, close to square) doesn't match this thin
                // 126x34 UI box - preserveAspect scales it down uniformly and letterboxes instead of
                // stretching it, which was the other half of the distorted-chip look.
                priceChip.preserveAspect = true;
                SetAnchoredBox(priceChip.rectTransform, new Vector2(0.5f, 0.5f), SlotCostPosition, SlotCostChipSize);
                _slotCostChipImages[index] = priceChip;

                var costText = CreateText("SlotCostText", slotRect, 16, FontStyle.Bold, new Color(0.16f, 0.14f, 0.12f, 0.98f), TextAnchor.MiddleCenter);
                SetAnchoredBox(costText.rectTransform, new Vector2(0.5f, 0.5f), SlotCostPosition, SlotCostTextSize);
                _slotCostTexts[index] = costText;

                var descriptionText = CreateText("SlotDescription", slotRect, 14, FontStyle.Normal, new Color(0.14f, 0.12f, 0.1f, 0.96f), TextAnchor.UpperCenter);
                SetAnchoredBox(descriptionText.rectTransform, new Vector2(0.5f, 0.5f), SlotDescriptionPosition, SlotDescriptionSize);
                _slotDescriptionTexts[index] = descriptionText;

                var typeBadge = CreateImage("SlotTypeBadge", slotRect, _towerUpgradeIconSprite, Color.white, Image.Type.Simple);
                SetAnchoredBox(typeBadge.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(TypeBadgeSize, TypeBadgeSize));
                typeBadge.preserveAspect = true;
                typeBadge.gameObject.SetActive(false);
                _slotTypeBadgeImages[index] = typeBadge;
            }
        }

        private void Refresh()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            var isVisible = _service != null && _service.IsOpen;
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;

            if (!isVisible)
            {
                return;
            }

            _titleText.text = _service.Title;
            _summaryText.text = _service.Summary;
            _resourceText.text = _service.ResourceSummary;
            _footerText.text = _service.FooterHint;

            var offers = _service.CurrentOffers;

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                var hasOffer = index < offers.Count && offers[index] != null;
                _slotRoots[index].gameObject.SetActive(hasOffer);

                if (!hasOffer)
                {
                    continue;
                }

                var offer = offers[index];
                // No SetNativeSize() here - see LoadSprites()'s comment. The frame stays sized to
                // its slot via the anchors set up in BuildPanel(); only its sprite changes per rarity.
                _slotFrameImages[index].sprite = ResolveCardFrameSprite(_service.ResolveOfferRarity(offer));
                _slotFrameImages[index].type = Image.Type.Sliced;
                if (_slotCostChipImages[index] != null)
                {
                    _slotCostChipImages[index].sprite = _priceChipSprite;
                }
                _slotHighlights[index].color = index == _service.SelectedIndex ? new Color(0.96f, 0.72f, 0.22f, 0.18f) : new Color(0.06f, 0.08f, 0.12f, 0.08f);
                _slotTitleTexts[index].text = _service.ResolveOfferTitle(offer);
                _slotCostTexts[index].text = LocalizationService.Resolve("Cost ", "Kosten ") + _service.ResolveOfferCost(offer);
                _slotDescriptionTexts[index].text = _service.ResolveOfferDescription(offer);

                // Shop.md requires the UI show "Kosten, Kaufbarkeit, Seltenheit". Affordability
                // was only evaluated on Confirm, so an offer the player could not pay for looked
                // exactly like one they could. Two cues rather than one, because colour alone is
                // not a safe carrier: the cost text turns red and the whole slot dims.
                var canAfford = _service.CanAffordOffer(offer);
                _slotCostTexts[index].color = canAfford
                    ? new Color(0.16f, 0.14f, 0.12f, 0.98f)
                    : new Color(0.62f, 0.12f, 0.1f, 1f);
                _slotFrameImages[index].color = canAfford ? Color.white : new Color(0.62f, 0.62f, 0.66f, 0.85f);
                // Epic and Legendary frames have a dark header plaque; dark-on-dark made those
                // titles invisible on the reward screen and here alike (playtest 2026-09-22).
                var rarity = _service.ResolveOfferRarity(offer);
                var darkPlaque = rarity == Cards.CardRarity.Epic || rarity == Cards.CardRarity.Legendary;
                _slotTitleTexts[index].color = canAfford
                    ? (darkPlaque ? new Color(0.95f, 0.91f, 0.82f, 1f) : new Color(0.14f, 0.12f, 0.1f, 0.98f))
                    : (darkPlaque ? new Color(0.7f, 0.67f, 0.6f, 0.85f) : new Color(0.42f, 0.4f, 0.38f, 0.85f));
                _slotTitleTexts[index].resizeTextForBestFit = true;
                _slotTitleTexts[index].resizeTextMinSize = 10;
                _slotTitleTexts[index].resizeTextMaxSize = Mathf.Max(11, _slotTitleTexts[index].fontSize);
                _slotTitleTexts[index].verticalOverflow = VerticalWrapMode.Truncate;

                if (_slotTypeBadgeImages[index] != null)
                {
                    var isTowerUpgrade = offer.OfferType == ShopOfferType.TowerUpgrade;
                    _slotTypeBadgeImages[index].gameObject.SetActive(isTowerUpgrade && _towerUpgradeIconSprite != null);
                }
            }

            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private Sprite ResolveCardFrameSprite(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare:
                    return _rareFrameSprite != null ? _rareFrameSprite : _commonFrameSprite;
                case CardRarity.Epic:
                    return _epicFrameSprite != null ? _epicFrameSprite : _rareFrameSprite;
                case CardRarity.Legendary:
                    return _legendaryFrameSprite != null ? _legendaryFrameSprite : _epicFrameSprite;
                default:
                    return _commonFrameSprite;
            }
        }

        private void LoadSprites()
        {
            _runtimeLoadedPanelTexture = DebugSpriteFactory.LoadTextureFromProjectFile(PanelTexturePath);

            if (_runtimeLoadedPanelTexture == null)
            {
                _runtimeLoadedPanelTexture = DebugSpriteFactory.LoadTextureFromProjectFile(PanelTextureFallbackPath);
            }

            _runtimeLoadedTowerUpgradeIconTexture = DebugSpriteFactory.LoadTextureFromProjectFile(TowerUpgradeIconPath);
            _panelSprite = DebugSpriteFactory.CreateSprite(_runtimeLoadedPanelTexture, 100f);
            _towerUpgradeIconSprite = DebugSpriteFactory.CreateSprite(_runtimeLoadedTowerUpgradeIconTexture, 100f);

            // shop_price_chip.png is the same story again: a 1113x607 canvas where the actual chip
            // (the beveled hexagonal readout with the glowing amber border) only occupies a ~555x460
            // region in the middle, flanked by separate decorative wing pieces on both sides that were
            // never meant to render as part of the small 126x34 UI chip. Rendering the raw full canvas
            // unsliced/uncropped into that box squished the whole thing - wings, screws and all - into
            // a thin distorted strip, which is what read as visual noise around the cost text.
            _priceChipSprite = DebugSpriteFactory.LoadProjectSpriteRect(PriceChipTexturePath, new Rect(285f, 52f, 555f, 460f));

            // Same fix as CardRewardPresenter.ResolveCardFrameSprites(): the four card_frame_*.png
            // files are each a 1113x607 canvas with the actual frame art occupying only a ~445x545
            // portrait region in the middle - loading the raw full canvas here (as this used to) and
            // then calling SetNativeSize() on it in RebuildOffers() sized each shop slot's frame to
            // that whole padded canvas, vastly larger than the 282x414 slot, which is exactly why the
            // Foundry Exchange screen still looked like Card Reward's pre-fix overlap bug - this
            // presenter never got the same fix applied to it. Cropping to the shared visible-content
            // rect and slicing it fixes both the oversized-canvas problem and keeps the frame's
            // corner art crisp instead of squished.
            _commonFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(CommonFramePath, CardFrameContentRect, 100f, CardFrameBorder);
            _rareFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(RareFramePath, CardFrameContentRect, 100f, CardFrameBorder);
            _epicFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(EpicFramePath, CardFrameContentRect, 100f, CardFrameBorder);
            _legendaryFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(LegendaryFramePath, CardFrameContentRect, 100f, CardFrameBorder);
        }

        private static readonly Rect CardFrameContentRect = new Rect(335f, 25f, 465f, 560f);
        private static readonly Vector4 CardFrameBorder = new Vector4(55f, 55f, 55f, 55f);

        private void RegisterWithUIManager()
        {
            _managedPanel = UIManager.RegisterPanel(_presenterRoot, "shop_panel", UIPanelLayer.BlockingOverlay, true, true, _canvasGroup);
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
            rectTransform.SetAsLastSibling();
            return rectTransform;
        }

        private void ResolveOrBuildPanel()
        {
            if (TryBindAuthoredLayout())
            {
                return;
            }

            // ShopCanvas.unity is still being loaded additively - its objects appear at the end of
            // this frame. Building the local fallback now would put a placeholder panel on screen
            // that the real authored canvas then renders on top of. Wait for CanvasSceneReady
            // instead; EnsureInitialized() is re-run from there.
            if (AdditiveCanvasSceneLoader.IsScenePending(AdditiveCanvasSceneLoader.ShopCanvasSceneName))
            {
                return;
            }

            BuildPanel();
        }

        // See CardRewardPresenter.TryBindAuthoredLayout() for why this searches globally instead
        // of using transform.Find()/cached serialized fields: ShopCanvas.unity is loaded additively
        // by AdditiveCanvasSceneLoader, so it is never a child of this presenter's own GameObject.
        // Same root cause and fix as CardRewardPresenter.TryBindAuthoredLayout() (2026-09-16
        // playtest): the extracted scene's Image components saved with no sprite, and this
        // presenter's own EnsureSceneShopLayout/EnsureEditorImage below never runs anymore for
        // the same reason - and use Editor-only AssetDatabase besides. Repairs are applied here,
        // build-safe, on every bind.
        private bool TryBindAuthoredLayout()
        {
            var canvasObject = GameObject.Find(ShopCanvasName);
            var canvasRoot = canvasObject != null ? canvasObject.transform as RectTransform : null;
            var shopRoot = canvasRoot != null ? canvasRoot.Find(ShopRootName) as RectTransform : null;

            if (shopRoot == null)
            {
                return false;
            }

            _authoredPresenterRoot = shopRoot;
            _authoredCanvasGroup = shopRoot.GetComponent<CanvasGroup>();
            var panel = shopRoot.Find(ShopPanelName) as RectTransform;

            if (panel == null)
            {
                return false;
            }

            RepairSprite(panel.GetComponent<Image>(), PanelTexturePath, PanelTextureFallbackPath);

            _authoredTitleText = FindText(panel, "Title");
            _authoredSummaryText = FindText(panel, "Summary");
            _authoredResourceText = FindText(panel, "Resources");
            _authoredFooterText = FindText(panel, "Footer");

            if (_authoredPresenterRoot == null || _authoredCanvasGroup == null || _authoredTitleText == null || _authoredSummaryText == null || _authoredResourceText == null || _authoredFooterText == null)
            {
                return false;
            }

            _presenterRoot = _authoredPresenterRoot;
            _canvasGroup = _authoredCanvasGroup;
            _titleText = _authoredTitleText;
            _summaryText = _authoredSummaryText;
            _resourceText = _authoredResourceText;
            _footerText = _authoredFooterText;

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                // ShopSlot0/1/2 are children of the ShopOfferPanel image, not direct children of
                // the presenter root (see EnsureSceneShopLayout: slots are parented under
                // panelRect, one level below shopRoot) - Find() only searches immediate children,
                // so the lookup has to go through the panel by name.
                var slotTransform = _authoredPresenterRoot.Find(ShopPanelName + "/ShopSlot" + index) as RectTransform;

                if (slotTransform == null)
                {
                    return false;
                }

                _slotRoots[index] = slotTransform;
                _slotHighlights[index] = slotTransform.GetComponent<Image>();
                _slotFrameImages[index] = FindImage(slotTransform, "SlotFrame", CommonFramePath, null);
                _slotTitleTexts[index] = FindText(slotTransform, "SlotTitle");
                _slotCostChipImages[index] = FindImage(slotTransform, "SlotCostChip", PriceChipTexturePath, null);
                _slotCostTexts[index] = FindText(slotTransform, "SlotCostText");
                _slotDescriptionTexts[index] = FindText(slotTransform, "SlotDescription");
                _slotTypeBadgeImages[index] = FindImage(slotTransform, "SlotTypeBadge", TowerUpgradeIconPath, null);

                if (_slotTypeBadgeImages[index] != null)
                {
                    _slotTypeBadgeImages[index].sprite = _towerUpgradeIconSprite;
                }

                if (_slotFrameImages[index] == null || _slotCostChipImages[index] == null || _slotTitleTexts[index] == null || _slotCostTexts[index] == null || _slotDescriptionTexts[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        // Only assigns when missing - never overwrites a sprite already set by hand, so this is
        // safe to call on every bind. Tries primaryPath first, then fallbackPath (some source art
        // in this project was imported with a doubled ".png.png" extension).
        private static void RepairSprite(Image image, string primaryPath, string fallbackPath)
        {
            if (image == null || image.sprite != null)
            {
                return;
            }

            var sprite = !string.IsNullOrWhiteSpace(primaryPath) ? DebugSpriteFactory.LoadProjectSprite(primaryPath) : null;

            if (sprite == null && !string.IsNullOrWhiteSpace(fallbackPath))
            {
                sprite = DebugSpriteFactory.LoadProjectSprite(fallbackPath);
            }

            image.sprite = sprite;
        }

        // Find-only, no position/anchor/size touched - ShopCanvas.unity's own saved RectTransform
        // values are the sole source of truth (2026-09-17: "I want the no code driven way"). Hand-
        // drag something in the Scene view and it stays put.
        private static Text FindText(RectTransform parent, string name)
        {
            var child = parent != null ? parent.Find(name) as RectTransform : null;
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Image FindImage(RectTransform parent, string name, string primaryPath, string fallbackPath)
        {
            var child = parent != null ? parent.Find(name) as RectTransform : null;

            if (child == null)
            {
                return null;
            }

            var image = child.GetComponent<Image>();
            RepairSprite(image, primaryPath, fallbackPath);
            return image;
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, fontStyle, color, anchor);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type imageType)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color, imageType);
        }

        private static void DestroyRuntimeSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(sprite);
            }
            else
            {
                DestroyImmediate(sprite);
            }
        }

        private static void CleanupTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }

        private static Font ResolveFont()
        {
            return Corebound.UI.UIFontResolver.Resolve();
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
            rectTransform.pivot = anchor;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }

#if UNITY_EDITOR
        private void EnsureSceneShopLayout()
        {
            if (_authoredPresenterRoot != null && _authoredCanvasGroup != null && _authoredTitleText != null && _authoredSummaryText != null)
            {
                return;
            }

            var canvasTransform = transform.Find(ShopCanvasName) as RectTransform;

            if (canvasTransform == null)
            {
                var canvasObject = new GameObject(ShopCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                canvasTransform = canvasObject.GetComponent<RectTransform>();
                StretchToParent(canvasTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            var shopRoot = EnsureEditorUiChild(canvasTransform, ShopRootName, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _authoredPresenterRoot = shopRoot;
            var isNewCanvasGroup = shopRoot.GetComponent<CanvasGroup>() == null;
            _authoredCanvasGroup = shopRoot.gameObject.GetOrAdd<CanvasGroup>();

            if (isNewCanvasGroup)
            {
                // Same fix as CardRewardPresenter.EnsureSceneRewardLayout() (2026-09-15): this runs
                // from OnValidate() via delayCall, independent of the normal Awake()->Refresh()
                // lifecycle - AddComponent<CanvasGroup>() defaults to fully visible/interactable, so
                // a freshly (re)built shop panel would sit visible and blocking gameplay until
                // something else happened to trigger Refresh(). Start hidden; Refresh() runs right
                // after and corrects this if the shop is actually open at that moment.
                _authoredCanvasGroup.alpha = 0f;
                _authoredCanvasGroup.interactable = false;
                _authoredCanvasGroup.blocksRaycasts = false;
            }

            var overlay = EnsureEditorImage(shopRoot, "Overlay", null, null, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overlay.color = new Color(0.02f, 0.04f, 0.06f, 0.88f);

            var panel = EnsureEditorImage(shopRoot, ShopPanelName, PanelTexturePath, PanelTextureFallbackPath, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 680f));
            var panelRect = panel.rectTransform;

            // Same measured-off-the-art band positions as BuildPanel() above, and the same Summary
            // repurpose (top subtitle -> bottom description band, matching what it actually shows).
            _authoredTitleText = EnsureEditorText(panelRect, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(180f, -125f), new Vector2(-180f, -54f), TextAnchor.MiddleCenter, 28, new Color(0.14f, 0.12f, 0.1f, 0.98f));
            _authoredSummaryText = EnsureEditorText(panelRect, "Summary", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(287f, 71f), new Vector2(-289f, 133f), TextAnchor.MiddleCenter, 18, new Color(0.16f, 0.14f, 0.11f, 0.96f));
            _authoredResourceText = EnsureEditorText(panelRect, "Resources", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(174f, -40f), TextAnchor.MiddleLeft, 18, new Color(0.18f, 0.16f, 0.12f, 0.96f));
            _authoredFooterText = EnsureEditorText(panelRect, "Footer", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(180f, -196f), new Vector2(-180f, -148f), TextAnchor.MiddleCenter, 18, new Color(0.18f, 0.16f, 0.12f, 0.96f));

            for (var index = 0; index < 3; index++)
            {
                var slotRect = EnsureEditorUiChild(panelRect, "ShopSlot" + index, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((index - 1) * SlotSpacing, SlotCenterY), new Vector2(SlotWidth, SlotHeight));
                var slotImage = slotRect.gameObject.GetOrAdd<Image>();
                slotImage.color = Color.clear;
                slotImage.raycastTarget = false;
                EnsureEditorImage(slotRect, "SlotFrame", CommonFramePath, null, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                EnsureEditorText(slotRect, "SlotTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), SlotTitlePosition - SlotTitleSize * 0.5f, SlotTitlePosition + SlotTitleSize * 0.5f, TextAnchor.UpperCenter, 18, new Color(0.14f, 0.12f, 0.1f, 0.98f));
                EnsureEditorImage(slotRect, "SlotCostChip", PriceChipTexturePath, null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), SlotCostPosition, SlotCostChipSize);
                EnsureEditorText(slotRect, "SlotCostText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), SlotCostPosition - SlotCostTextSize * 0.5f, SlotCostPosition + SlotCostTextSize * 0.5f, TextAnchor.MiddleCenter, 16, new Color(0.16f, 0.14f, 0.12f, 0.98f));
                EnsureEditorText(slotRect, "SlotDescription", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), SlotDescriptionPosition - SlotDescriptionSize * 0.5f, SlotDescriptionPosition + SlotDescriptionSize * 0.5f, TextAnchor.UpperCenter, 14, new Color(0.14f, 0.12f, 0.1f, 0.96f));
                var typeBadge = EnsureEditorImage(slotRect, "SlotTypeBadge", TowerUpgradeIconPath, null, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(TypeBadgeSize, TypeBadgeSize));
                typeBadge.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(this);
        }

        private static RectTransform EnsureEditorUiChild(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var child = parent.Find(name) as RectTransform;

            if (child == null)
            {
                var childObject = new GameObject(name, typeof(RectTransform));
                childObject.layer = 5;
                childObject.transform.SetParent(parent, false);
                child = childObject.GetComponent<RectTransform>();
            }

            child.anchorMin = anchorMin;
            child.anchorMax = anchorMax;
            child.pivot = pivot;
            child.anchoredPosition = anchoredPosition;
            child.sizeDelta = sizeDelta;
            return child;
        }

        private Image EnsureEditorImage(RectTransform parent, string name, string primarySpritePath, string fallbackSpritePath, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            _ = child.gameObject.GetOrAdd<CanvasRenderer>();
            var image = child.gameObject.GetOrAdd<Image>();
            image.sprite = ResolveEditorSprite(primarySpritePath, fallbackSpritePath);
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = name == "SlotTypeBadge";
            return image;
        }

        private Text EnsureEditorText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, TextAnchor alignment, int fontSize, Color color)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, Vector2.zero, Vector2.zero);
            StretchToParent(child, anchorMin, anchorMax, offsetMin, offsetMax);
            _ = child.gameObject.GetOrAdd<CanvasRenderer>();
            var text = child.gameObject.GetOrAdd<Text>();
            text.font = _font != null ? _font : ResolveFont();
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = name;
            return text;
        }

        private static Sprite ResolveEditorSprite(string primaryPath, string fallbackPath)
        {
            var sprite = !string.IsNullOrWhiteSpace(primaryPath) ? AssetDatabase.LoadAssetAtPath<Sprite>(primaryPath) : null;

            if (sprite != null)
            {
                return sprite;
            }

            return !string.IsNullOrWhiteSpace(fallbackPath) ? AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath) : null;
        }
#endif
    }
}
