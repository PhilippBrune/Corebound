using System.Collections.Generic;
using Corebound.Gameplay.Shared;
using Corebound.UI;
using Corebound.UI.MainMenu;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Corebound.Gameplay.Cards
{
    [ExecuteAlways]
    public sealed class CardRewardPresenter : MonoBehaviour
    {
        private const string RewardChoicePanelTexturePath = "Assets/Art/UI/HUD/card_reward_choice_panel.png";
        private static readonly Color OverlayColor = new Color(0.02f, 0.04f, 0.06f, 0.88f);
        private static readonly Color PanelFillColor = new Color(0.96f, 0.94f, 0.9f, 0.08f);
        private static readonly Color CardFillColor = new Color(0.06f, 0.08f, 0.12f, 0.08f);
        private static readonly Color CardSelectedFillColor = new Color(0.96f, 0.72f, 0.22f, 0.18f);
        private const string CommonFramePath = "Assets/Art/Cards/Frames/card_frame_common.png";
        private const string RareFramePath = "Assets/Art/Cards/Frames/card_frame_rare.png";
        private const string EpicFramePath = "Assets/Art/Cards/Frames/card_frame_epic.png";
        private const string LegendaryFramePath = "Assets/Art/Cards/Frames/card_frame_legendary.png";
        private const string RevealFlashSpritePath = "Assets/Art/Combat/VFX/vfx_card_reveal_flash.png";
        private const float RevealFlashFadeSeconds = 0.4f;

        private const float SlotWidth = 282f;
        private const float SlotHeight = 414f;
        private const float SlotSpacing = 332f;
        private const float SlotCenterY = -8f;

        private const int MaxTagIconsPerSlot = 4;
        private const float AuthoredTagIconSize = 34f;
        private const float AuthoredTagIconSpacing = 40f;
        private const float AuthoredTagIconBottom = 18f;

        // Tag icons this presenter created itself on the authored path. Only these are laid out in
        // code; an icon the scene authored keeps the scene's position.
        private readonly System.Collections.Generic.HashSet<Image> _runtimeTagIcons = new System.Collections.Generic.HashSet<Image>();

        private readonly RectTransform[] _slotRoots = new RectTransform[3];
        private readonly Text[] _slotTitleTexts = new Text[3];
        private readonly Text[] _slotRarityTexts = new Text[3];
        private readonly Text[] _slotDescriptionTexts = new Text[3];
        private readonly Image[] _slotHighlights = new Image[3];
        private readonly Image[] _slotFrameImages = new Image[3];
        private readonly Image[] _slotRevealFlashImages = new Image[3];
        private readonly Image[][] _slotTagIcons = new Image[3][];

        [SerializeField] private CardRewardController _controller;
        [SerializeField] private MainMenuSkinData _skinData;
        [SerializeField] private Texture2D _rewardChoicePanelTexture;
        [SerializeField] private Vector4 _rewardChoicePanelBorder = new Vector4(220f, 140f, 220f, 140f);
        // Deliberately NOT [SerializeField]: these are cross-scene references (RewardCanvas.unity
        // is a separate scene from this presenter), always re-resolved fresh via GameObject.Find()
        // in TryBindAuthoredLayout() - never read back from a serialized value. Marking them
        // [SerializeField] made Unity try to persist a cross-scene reference on domain reload/exit
        // Play Mode, logging "Cross scene references are not supported" and nulling the field out
        // (2026-09-16 playtest: 40+ such warnings flooding the Console during a single Play
        // session), which could then be read as a stale null before the next rebind ran.
        private RectTransform _authoredPresenterRoot;
        private CanvasGroup _authoredCanvasGroup;
        private Text _authoredTitleText;
        private Text _authoredResourceText;
        private Text _authoredTowerText;
        private Text _authoredDescriptionText;

        private RectTransform _presenterRoot;
        private CanvasGroup _canvasGroup;
        private Text _titleText;
        private Text _resourceText;
        private Text _towerText;
        private Text _descriptionText;
        private Font _font;
        private Sprite _frameSprite;
        private Sprite _panelSprite;
        private Texture2D _runtimeLoadedPanelTexture;
        private Sprite _commonFrameSprite;
        private Sprite _rareFrameSprite;
        private Sprite _epicFrameSprite;
        private Sprite _legendaryFrameSprite;
        private Sprite _revealFlashSprite;
        private UIManagedPanel _managedPanel;
        private bool _wasVisible;

        private const string RewardCanvasName = "RewardCanvas";
        private const string RewardRootName = "RewardScreen";
        private const string RewardPanelName = "CardRewardChoicePanel";

        // Entry point for Assets/Editor/AuthoredCanvasRepairTool.cs - a one-time, explicit bake of
        // correct sprite/position data into RewardCanvas.unity itself (see that tool's own comment
        // for why: playtest 2026-09-16, "I still cannot see the image in the Unity Scene window").
        public bool RepairAuthoredLayoutForEditorTool()
        {
            return TryBindAuthoredLayout();
        }

        // Playtest feedback (2026-09-16): going through MainMenu -> New Run left the reward
        // screen broken/empty; opening SampleScene directly and pressing Play worked fine. Root
        // cause: RewardCanvas.unity loads additively between this scene's OnEnable and Start
        // phases (see AdditiveCanvasSceneLoader), so on the MainMenu-transition path this Awake()
        // runs BEFORE it exists - the old code called the fallback-capable EnsureInitialized()
        // here, which (since TryBindAuthoredLayout() fails at this point) fell through to
        // BuildPanel()'s local fallback and set _slotRoots[0], permanently satisfying
        // EnsureInitialized()'s "already initialized" guard. Start()'s later retry (by which
        // point the additive scene load HAS completed) then saw the guard already satisfied and
        // never re-attempted the real bind, leaving the wrong locally-built panel in place
        // forever. Opening SampleScene directly in the Editor worked because
        // AdditiveCanvasSceneAutoLoader had RewardCanvas already loaded before Play Mode even
        // started, so the very first Awake()-time bind attempt already succeeded. Fix: Awake()
        // now only ever attempts a bind (a safe no-op if the canvas is not yet loaded - it does
        // not build anything), so the guard stays unsatisfied until Start()'s retry can actually
        // succeed or genuinely needs to fall back.
        private void Awake()
        {
            TryBindAuthoredLayout();
            Refresh();
        }

        // The one place guaranteed to run after RewardCanvas.unity's additive load has completed
        // - see the Awake() comment above for why the fallback-capable EnsureInitialized() must
        // not be called any earlier than this.
        private void Start()
        {
            EnsureInitialized();
            Refresh();
        }

        // Guarded by a dedicated flag rather than _slotRoots[0] != null: Awake()/OnEnable() now
        // call TryBindAuthoredLayout() directly (2026-09-16 fix, see Awake()'s comment), and a
        // successful early bind already populates _slotRoots - guarding on that would make this
        // whole method, including RegisterWithUIManager(), silently skip forever once Start()
        // runs. _isFullyInitialized is set only at the end of this method, once every step
        // (including registration) has actually completed.
        //
        // _slotRoots and the other runtime fields populated here are plain (non-serialized)
        // fields, so a domain reload (script recompile, or entering Play Mode with "Reload
        // Domain" enabled) resets them to their default/empty state. Unity's backup/restore cycle
        // for already-existing scene objects re-invokes OnEnable() after a reload but NOT Awake()
        // - so without this guard, the first OnEnable()->Refresh()->RebuildCards() after any
        // reload hit a null _slotRoots[index] and threw a NullReferenceException instead of ever
        // showing card content (see 2026-09-15 reward-screen investigation).
        private bool _isFullyInitialized;

        private void EnsureInitialized()
        {
            if (_isFullyInitialized)
            {
                return;
            }

            _font = ResolveFont();
            _frameSprite = CreateRuntimeSprite(_skinData != null ? _skinData.ButtonFrameTexture : null, _skinData != null ? _skinData.ButtonFrameBorder : Vector4.zero);
            _revealFlashSprite = DebugSpriteFactory.LoadProjectSprite(RevealFlashSpritePath);
            _panelSprite = ResolvePanelSprite();
            ResolveCardFrameSprites();
            ResolveOrBuildPanel();
            RegisterWithUIManager();
            _isFullyInitialized = true;
        }

#if UNITY_EDITOR
        // RewardCanvas.unity is a separate authored scene now (Assets/Scenes/RewardCanvas.unity),
        // not something this presenter creates - OnValidate no longer builds anything (that used to
        // create a duplicate, locally-parented canvas fighting the real one). It only retries the
        // bind, which is a no-op once EnsureInitialized() has already succeeded, and picks the
        // canvas up if the user opens RewardCanvas.unity additively alongside this scene later.
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
            // RewardCanvas.unity's additive load can complete on the MainMenu-transition path.
            TryBindAuthoredLayout();

            // RewardCanvas.unity only finishes loading at the end of the frame the gameplay scene
            // loaded in - after this component's Start(). Binding in Start() alone therefore always
            // missed it on the MainMenu -> New Run path. This fires once its objects really exist.
            AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;

            if (_controller != null)
            {
                _controller.StateChanged += Refresh;
            }

            Refresh();
        }

        // A click within this window after the screen opens is ignored: the reward usually opens
        // while the player is still clicking at the last enemy, and that click must not pick a card.
        private const float ClickGuardSeconds = 0.5f;
        private Vector2 _lastPointer;

        private void Update()
        {
            if (_controller == null || !_controller.IsOpen || UnityEngine.InputSystem.Mouse.current == null)
            {
                return;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            var pointer = mouse.position.ReadValue();
            var moved = (pointer - _lastPointer).sqrMagnitude > 4f;
            _lastPointer = pointer;
            var clicked = mouse.leftButton.wasPressedThisFrame && Time.unscaledTime - _controller.OpenedAtUnscaledTime > ClickGuardSeconds;

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

                _controller.SelectIndex(index);

                if (clicked)
                {
                    _controller.ConfirmSelection();
                }

                return;
            }
        }

        private void OnDisable()
        {
            AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;

            if (_controller != null)
            {
                _controller.StateChanged -= Refresh;
            }
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName != AdditiveCanvasSceneLoader.RewardCanvasSceneName)
            {
                return;
            }

            _isFullyInitialized = false;
            EnsureInitialized();
            Refresh();
        }

        private void OnDestroy()
        {
            // The rarity frame sprites now come from DebugSpriteFactory's shared LoadProjectSpriteRect
            // cache (see ResolveCardFrameSprites) - destroying them here would leave a dangling
            // reference in that cache for the next presenter instance to hand out. Only _frameSprite
            // and _panelSprite are still created per-instance via CreateRuntimeSprite and need cleanup.
            DestroySprite(_frameSprite);
            DestroySprite(_panelSprite);
            CleanupTexture(_runtimeLoadedPanelTexture);
            UIManager.UnregisterPanel(_managedPanel);
        }

        private void BuildPanel()
        {
            if (TryBindAuthoredLayout())
            {
                return;
            }

            _presenterRoot = EnsurePresenterRoot("CardRewardPresenterRoot");
            _canvasGroup = _presenterRoot.GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = _presenterRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var overlay = CreateImage("Overlay", _presenterRoot, null, OverlayColor, Image.Type.Simple);
            StretchToParent(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var panelSprite = _panelSprite != null ? _panelSprite : _frameSprite;
            var panel = CreateImage("Panel", _presenterRoot, panelSprite, Color.white, panelSprite != null ? Image.Type.Sliced : Image.Type.Simple);
            var panelRect = panel.rectTransform;
            SetAnchoredBox(panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 680f));

            // Vertical bands below are measured directly off card_reward_choice_panel.png's pixel
            // data (title tan bar, bright summary strip, footer tan bar), not guessed - the previous
            // offsets put Resources/TowerInfo well above the actual bright strip (overlapping the
            // ornate corner brackets of the title bar instead) and Description below the footer bar
            // entirely, onto the dark background outside the panel art.
            _titleText = CreateText("Title", panelRect, 28, FontStyle.Bold, new Color(0.16f, 0.14f, 0.11f, 0.98f), TextAnchor.MiddleCenter);
            StretchToParent(_titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(180f, -125f), new Vector2(-180f, -54f));

            _resourceText = CreateText("Resources", panelRect, 18, FontStyle.Normal, new Color(0.18f, 0.16f, 0.12f, 0.96f), TextAnchor.MiddleLeft);
            StretchToParent(_resourceText.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(381f, -196f), new Vector2(-20f, -148f));

            _towerText = CreateText("TowerInfo", panelRect, 18, FontStyle.Normal, new Color(0.18f, 0.16f, 0.12f, 0.96f), TextAnchor.MiddleRight);
            StretchToParent(_towerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(20f, -196f), new Vector2(-353f, -148f));

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                // Find-existing-first for the same reason as ShopPresenter's ShopSlot loop: this
                // fallback only runs when TryBindAuthoredLayout() fails, but [ExecuteAlways] re-runs
                // Awake() on every script recompile.
                var existingSlot = panelRect.Find("RewardSlot" + index);
                var slotObject = existingSlot != null ? existingSlot.gameObject : new GameObject("RewardSlot" + index, typeof(RectTransform), typeof(Image));

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

                var revealFlash = CreateImage("RevealFlash", slotRect, _revealFlashSprite, Color.clear, Image.Type.Simple);
                StretchToParent(revealFlash.rectTransform, new Vector2(-0.15f, -0.15f), new Vector2(1.15f, 1.15f), Vector2.zero, Vector2.zero);
                revealFlash.raycastTarget = false;
                _slotRevealFlashImages[index] = revealFlash;

                var frameImage = CreateImage("SlotFrame", slotRect, _frameSprite, Color.white, Image.Type.Sliced);
                StretchToParent(frameImage.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _slotFrameImages[index] = frameImage;

                var titleText = CreateText("SlotTitle", slotRect, 18, FontStyle.Bold, new Color(0.14f, 0.12f, 0.1f, 0.98f), TextAnchor.UpperLeft);
                // Extended up into the band the now-hidden SlotRarity label used to occupy
                // (-316 -> -286) so the title has more wrap room instead of leaving that space dead.
                StretchToParent(titleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 28f), new Vector2(-22f, -286f));
                _slotTitleTexts[index] = titleText;

                var rarityText = CreateText("SlotRarity", slotRect, 16, FontStyle.Bold, new Color(0.28f, 0.24f, 0.18f, 0.98f), TextAnchor.UpperLeft);
                StretchToParent(rarityText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 58f), new Vector2(-22f, -286f));
                // Rarity is already conveyed by the frame color/art - the text label is redundant
                // clutter inside the card box. Kept bound (not removed) since the rest of this class
                // still references the field; just hidden.
                rarityText.gameObject.SetActive(false);
                _slotRarityTexts[index] = rarityText;

                var descriptionText = CreateText("SlotDescription", slotRect, 14, FontStyle.Normal, new Color(0.14f, 0.12f, 0.1f, 0.96f), TextAnchor.UpperLeft);
                // Bottom edge at 140, not 112 - SlotTitle's own box (see rarityText/titleText above,
                // "SlotTitle" via `new Vector2(22f, 28f), new Vector2(-22f, -286f)`) sits at y 28-128,
                // so a description long enough to wrap this box's full 278px height down to y=112
                // would overlap SlotTitle's text for a few lines. 140 leaves a clean 12px gap above it.
                StretchToParent(descriptionText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 140f), new Vector2(-22f, -24f));
                // Card descriptions vary in length same as Relic descriptions do (see
                // RelicRevealPresenter's equivalent fix, 2026-09-15) - ProceduralUIFactory.CreateText
                // defaults to VerticalWrapMode.Overflow, which let a long card description spill
                // past this slot's frame instead of clipping cleanly at its own box.
                descriptionText.verticalOverflow = VerticalWrapMode.Truncate;
                _slotDescriptionTexts[index] = descriptionText;

                // Card-Tag-Icons (Graphics.md's UI asset list): a small row in the previously
                // empty strip below the description, using card_tag_icon_sheet.png - imported
                // since the original Graphics asset pass but referenced by no code until now.
                var tagIcons = new Image[MaxTagIconsPerSlot];

                for (var iconIndex = 0; iconIndex < MaxTagIconsPerSlot; iconIndex++)
                {
                    var iconImage = CreateImage("TagIcon" + iconIndex, slotRect, null, Color.white, Image.Type.Simple);
                    var iconRect = iconImage.rectTransform;
                    iconRect.anchorMin = new Vector2(0f, 0f);
                    iconRect.anchorMax = new Vector2(0f, 0f);
                    iconRect.pivot = new Vector2(0f, 0f);
                    iconRect.anchoredPosition = new Vector2(22f + iconIndex * 36f, 14f);
                    iconRect.sizeDelta = new Vector2(30f, 30f);
                    iconImage.gameObject.SetActive(false);
                    tagIcons[iconIndex] = iconImage;
                }

                _slotTagIcons[index] = tagIcons;
            }

            _descriptionText = CreateText("Description", panelRect, 18, FontStyle.Normal, new Color(0.16f, 0.14f, 0.11f, 0.96f), TextAnchor.MiddleCenter);
            StretchToParent(_descriptionText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(288f, 71f), new Vector2(-288f, 133f));
        }

        private void Refresh()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            var isVisible = _controller != null && _controller.IsOpen;
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;

            if (!isVisible)
            {
                _wasVisible = false;
                return;
            }

            _titleText.text = _controller.PendingRewardLabel;
            _resourceText.text = _controller.ResourceSummary;
            _towerText.text = _controller.TowerSummary;
            // The card body already carries the prose; the footer carries the numbers.
            _descriptionText.text = _controller.SelectedDescription
                + LocalizationService.Resolve("   (Confirm or click to take", "   (Bestätigen oder Klick zum Nehmen")
                + (_controller.RemainingRerolls > 0
                    ? LocalizationService.Resolve("; Q / LB rerolls, ", "; Q / LB würfelt neu, ") + _controller.RemainingRerolls + LocalizationService.Resolve(" left)", " übrig)")
                    : ")");

            RebuildCards();

            // Only on the closed->open edge, not every StateChanged (selection changes also raise
            // it) - VFX.md calls reward reveals "selten und eindeutig", not a repeat-per-navigation
            // flourish.
            if (!_wasVisible)
            {
                TriggerRevealFlash();
            }

            _wasVisible = true;
            UIManager.NotifyPanelChanged(_managedPanel);
        }

        private void TriggerRevealFlash()
        {
            if (_revealFlashSprite == null)
            {
                return;
            }

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                var flashImage = index < _slotRevealFlashImages.Length ? _slotRevealFlashImages[index] : null;

                if (flashImage == null || !_slotRoots[index].gameObject.activeSelf)
                {
                    continue;
                }

                StartCoroutine(FadeOutRevealFlash(flashImage));
            }
        }

        private System.Collections.IEnumerator FadeOutRevealFlash(Image flashImage)
        {
            flashImage.color = Color.white;
            var elapsed = 0f;

            while (elapsed < RevealFlashFadeSeconds)
            {
                elapsed += Time.deltaTime;
                var alpha = Mathf.Lerp(1f, 0f, elapsed / RevealFlashFadeSeconds);
                flashImage.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            flashImage.color = Color.clear;
        }

        private void RebuildCards()
        {
            var offer = _controller.GetCurrentOffer();

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                var hasCard = index < offer.Count && offer[index] != null;
                _slotRoots[index].gameObject.SetActive(hasCard);

                if (!hasCard)
                {
                    continue;
                }

                var card = offer[index];
                // SetNativeSize() used to run here, which resized the frame to the sprite's raw
                // pixel dimensions (1113x607 - the frame texture's full padded canvas, see
                // ResolveCardFrameSprites) instead of respecting the slot's stretch-anchored
                // 282x414 rect. That is what made all three reward cards balloon out and overlap
                // across the whole panel, burying the description text. The frame now stays sized
                // to its slot via the anchors set up above; only its sprite changes per rarity.
                // Type is set explicitly (not just at creation) because the authored SampleScene
                // layout's SlotFrame Image predates this fix and was saved with Type=Simple, which
                // TryBindAuthoredLayout() picks up as-is without touching component settings.
                _slotFrameImages[index].sprite = ResolveCardFrameSprite(card);
                _slotFrameImages[index].type = Image.Type.Sliced;
                _slotHighlights[index].color = index == _controller.SelectedIndex ? CardSelectedFillColor : CardFillColor;
                // Epic and Legendary frames have a dark header plaque; the dark title colour that
                // reads on Common/Rare silver vanished on them entirely (autopilot shot 2026-09-22).
                var offeredRarity = _controller.ResolveOfferedRarity(card);
                // Playtest 2026-09-28: "adjust font size so the card title fits on the top row".
                // The grade used to be appended here ("Rapid Array  (Rare)"), which is what pushed
                // long titles out of the plaque - the name alone always fits, the name plus a suffix
                // does not. The grade moved to the first line of the description, where there is
                // room, and the rarity-coloured frame already carries it at a glance.
                _slotTitleTexts[index].text = card.DisplayName;
                var darkPlaque = offeredRarity == CardRarity.Epic || offeredRarity == CardRarity.Legendary;
                _slotTitleTexts[index].color = index == _controller.SelectedIndex
                    ? (_skinData != null ? _skinData.SelectedLabelColor : Color.white)
                    : darkPlaque ? new Color(0.95f, 0.91f, 0.82f, 1f) : new Color(0.14f, 0.12f, 0.1f, 0.98f);
                // Long names ("Overcharged Rounds") wrapped out of the plaque; shrink instead.
                _slotTitleTexts[index].resizeTextForBestFit = true;
                _slotTitleTexts[index].resizeTextMinSize = 10;
                _slotTitleTexts[index].resizeTextMaxSize = Mathf.Max(11, _slotTitleTexts[index].fontSize - 2);
                _slotTitleTexts[index].verticalOverflow = VerticalWrapMode.Truncate;
                _slotRarityTexts[index].text = card.Rarity.ToString();
                _slotDescriptionTexts[index].text = card.IsWeaponCard && offeredRarity != CardRarity.Common
                    ? offeredRarity + LocalizationService.Resolve(" copy", "-Kopie") + System.Environment.NewLine + card.Description
                    : card.Description;
                ApplyTagIcons(index, card);
            }
        }

        private void ApplyTagIcons(int slotIndex, CardData card)
        {
            ApplyWeaponCardIcon(slotIndex, card);
            var icons = slotIndex < _slotTagIcons.Length ? _slotTagIcons[slotIndex] : null;

            if (icons == null)
            {
                return;
            }

            var tags = card.Tags;

            for (var iconIndex = 0; iconIndex < icons.Length; iconIndex++)
            {
                var icon = icons[iconIndex];

                if (icon == null)
                {
                    continue;
                }

                if (tags == null || iconIndex >= tags.Length)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }

                var sprite = CardTagIconAtlas.ResolveIcon(tags[iconIndex]);
                icon.sprite = sprite;
                icon.gameObject.SetActive(sprite != null);

                if (_runtimeTagIcons.Contains(icon))
                {
                    var visibleCount = Mathf.Min(tags.Length, icons.Length);
                    var offset = (iconIndex - (visibleCount - 1) * 0.5f) * AuthoredTagIconSpacing;
                    icon.rectTransform.anchoredPosition = new Vector2(offset, AuthoredTagIconBottom);
                }
            }
        }

        // Owner 2026-09-25: "I want a separate icon for the cards for weapon cards". A weapon card
        // shows the weapon's own icon from the 3x3 sheet (Asset 102) in the card body, where an
        // ordinary card shows only its tag icons - so a weapon offer is recognisable before reading
        // a word of it.
        private readonly Image[] _slotWeaponIcons = new Image[3];

        private void ApplyWeaponCardIcon(int slotIndex, CardData card)
        {
            if (slotIndex >= _slotRoots.Length || _slotRoots[slotIndex] == null)
            {
                return;
            }

            var sprite = card != null && card.IsWeaponCard && card.GrantedWeapon != null && card.GrantedWeapon.Presentation != null
                ? card.GrantedWeapon.Presentation.ResolveIconSprite()
                : null;

            if (sprite == null && _slotWeaponIcons[slotIndex] == null)
            {
                return;
            }

            if (_slotWeaponIcons[slotIndex] == null)
            {
                var icon = CreateImage("WeaponIcon", _slotRoots[slotIndex], sprite, Color.white, Image.Type.Simple);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var rect = icon.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -30f);
                rect.sizeDelta = new Vector2(150f, 150f);
                _slotWeaponIcons[slotIndex] = icon;
            }

            _slotWeaponIcons[slotIndex].sprite = sprite;
            _slotWeaponIcons[slotIndex].gameObject.SetActive(sprite != null);
        }

        private Text CreateText(string name, RectTransform parent, int fontSize, FontStyle fontStyle, Color color, TextAnchor anchor)
        {
            return ProceduralUIFactory.CreateText(name, parent, _font, fontSize, fontStyle, color, anchor);
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, Image.Type type)
        {
            return ProceduralUIFactory.CreateImage(name, parent, sprite, color, type);
        }

        private static Sprite CreateRuntimeSprite(Texture2D texture, Vector4 border)
        {
            return DebugSpriteFactory.CreateSprite(texture, 100f, border);
        }

        private Sprite ResolvePanelSprite()
        {
            if (_rewardChoicePanelTexture != null)
            {
                return CreateRuntimeSprite(_rewardChoicePanelTexture, _rewardChoicePanelBorder);
            }

            _runtimeLoadedPanelTexture = DebugSpriteFactory.LoadTextureFromProjectFile(RewardChoicePanelTexturePath);
            return CreateRuntimeSprite(_runtimeLoadedPanelTexture, _rewardChoicePanelBorder);
        }

        // The four card_frame_*.png files are each a 1113x607 canvas with the actual frame artwork
        // occupying only a ~445x545 portrait region in the middle (confirmed by reading each file's
        // alpha bounding box directly) - the rest is transparent padding. Loading the raw full-canvas
        // rect (as CreateRuntimeSprite/_frameSprite do) and then calling SetNativeSize() on it, as
        // RebuildCards() used to, sized each slot's frame to that whole padded 1113x607 canvas -
        // vastly larger than the 282x414 slot, which is why frames overlapped across the whole
        // reward panel. Cropping to the shared visible-content rect and slicing it fixes both the
        // oversized-canvas problem and lets the frame's corner art stay crisp when
        // stretched into the slot instead of being squished.
        // Re-measured 2026-09-27 from each file's alpha bounding box after the playtest note that
        // the card contents sit slightly offset inside the card. The previous (335, 25, 465, 560)
        // carried ~20px of transparent slack on the right and ~7px on top, so stretching it into
        // the slot drew the visible frame inset from - and off-centre within - the slot's own fill,
        // which is what showed up as the selected card's wash not lining up with its border.
        //
        // The four rarities have slightly different bounds (common 340,35..784,576; rare
        // 340,33..790,580; epic 340,29..793,582; legendary 342,36..787,581 in top-left pixels), so
        // this is their union rather than any one file's: all three slots are the same size on
        // screen and must render their frames identically, whatever rarity landed in them.
        internal static readonly Rect CardFrameContentRect = new Rect(340f, 25f, 453f, 553f);
        // Measured directly off the art: the metal border inside the 465x560 crop is ~90px left,
        // ~75px right, ~100px top, ~80px bottom - a border that size renders at a fixed pixel
        // thickness regardless of the target Image's RectTransform size, so on the much smaller
        // 282x414 slot it consumed most of the card as decorative metal instead of stretched middle,
        // reading as "too thick". Scaled down by the same ratio the slot is smaller than the source
        // crop (~0.67x) so the border reads at roughly the same visual proportion at this size.
        // Halved again from 55 per playtest feedback ("frame is still too thick") - the metal trim
        // now renders at roughly a quarter of its native-art thickness relative to the 282x414 slot.
        internal static readonly Vector4 CardFrameBorder = new Vector4(27f, 27f, 27f, 27f);

        private void ResolveCardFrameSprites()
        {
            _commonFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(CommonFramePath, CardFrameContentRect, 100f, CardFrameBorder);
            _rareFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(RareFramePath, CardFrameContentRect, 100f, CardFrameBorder);
            _epicFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(EpicFramePath, CardFrameContentRect, 100f, CardFrameBorder);
            _legendaryFrameSprite = DebugSpriteFactory.LoadProjectSpriteRect(LegendaryFramePath, CardFrameContentRect, 100f, CardFrameBorder);
        }

        private Sprite ResolveCardFrameSprite(CardData card)
        {
            if (card == null)
            {
                return _frameSprite;
            }

            // A weapon card's rarity is rolled per offer (it is the tier the weapon arrives at), so
            // the frame has to follow the rolled value rather than the authored one.
            switch (_controller != null ? _controller.ResolveOfferedRarity(card) : card.Rarity)
            {
                case CardRarity.Rare:
                    return _rareFrameSprite != null ? _rareFrameSprite : _frameSprite;
                case CardRarity.Epic:
                    return _epicFrameSprite != null ? _epicFrameSprite : _frameSprite;
                case CardRarity.Legendary:
                    return _legendaryFrameSprite != null ? _legendaryFrameSprite : _frameSprite;
                default:
                    return _commonFrameSprite != null ? _commonFrameSprite : _frameSprite;
            }
        }

        private static void DestroySprite(Sprite sprite)
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

            // Still waiting on RewardCanvas.unity's additive load - building the local fallback now
            // would put a placeholder panel on screen that the real canvas renders over a frame
            // later. OnCanvasSceneReady re-runs EnsureInitialized() once it has actually arrived.
            if (AdditiveCanvasSceneLoader.IsScenePending(AdditiveCanvasSceneLoader.RewardCanvasSceneName))
            {
                return;
            }

            BuildPanel();
        }

        // RewardCanvas.unity (Assets/Scenes/RewardCanvas.unity) is loaded additively by
        // AdditiveCanvasSceneLoader, so it is never a child of this presenter's own GameObject -
        // GameObject.Find() searches every loaded scene, which transform.Find() (own-children-only)
        // cannot do. Re-resolved fresh on every call rather than trusting the cached _authored*
        // fields, matching the same "domain reload resets non-serialized state" lesson already
        // applied elsewhere this session - here the risk is instead a stale reference surviving
        // into a save where the canvas scene was not yet loaded when this last ran.
        // Playtest feedback (2026-09-16): the extracted RewardCanvas.unity scene's Image
        // components were saved with no sprite (the Editor-only EnsureSceneRewardLayout/
        // EnsureEditorImage methods below that used to assign them got disconnected from the
        // live flow by an earlier fix, and even reconnected they use AssetDatabase, which does
        // not exist in a real build). This is a REAL runtime bug (the reward screen is genuinely
        // built from this scene's saved data at Play Mode / in a real build), not just an Editor
        // cosmetic issue - every bind re-applies the canonical sprite (via the build-safe
        // DebugSpriteFactory.LoadProjectSprite, only when a sprite is not already assigned - never
        // overwrites art already set by hand).
        //
        // Playtest feedback (2026-09-17): "I want the no code driven way" - positions used to also
        // be reasserted unconditionally on every bind, fighting any hand-adjustment made in the
        // Scene view. This method no longer touches position/anchor/size at all - RewardCanvas.unity
        // is the sole source of truth for layout now that AuthoredCanvasRepairTool (Corebound >
        // Repair Authored Canvases menu) has baked correct starting positions into the scene file.
        // Drag things in the Scene view and it sticks; run that menu command again only if the
        // scene's layout data ever gets corrupted/lost again.
        private bool TryBindAuthoredLayout()
        {
            var canvasObject = GameObject.Find(RewardCanvasName);
            var canvasRoot = canvasObject != null ? canvasObject.transform as RectTransform : null;
            var rewardRoot = canvasRoot != null ? canvasRoot.Find(RewardRootName) as RectTransform : null;

            if (rewardRoot == null)
            {
                return false;
            }

            _authoredPresenterRoot = rewardRoot;
            _authoredCanvasGroup = rewardRoot.GetComponent<CanvasGroup>();
            var panel = rewardRoot.Find(RewardPanelName) as RectTransform;

            if (panel == null)
            {
                return false;
            }

            RepairSprite(panel.GetComponent<Image>(), RewardChoicePanelTexturePath);

            _authoredTitleText = FindText(panel, "Title");
            _authoredResourceText = FindText(panel, "Resources");
            _authoredTowerText = FindText(panel, "TowerInfo");
            _authoredDescriptionText = FindText(panel, "Description");

            if (_authoredPresenterRoot == null || _authoredCanvasGroup == null || _authoredTitleText == null || _authoredDescriptionText == null)
            {
                return false;
            }

            _presenterRoot = _authoredPresenterRoot;
            _canvasGroup = _authoredCanvasGroup;
            _titleText = _authoredTitleText;
            _resourceText = _authoredResourceText;
            _towerText = _authoredTowerText;
            _descriptionText = _authoredDescriptionText;

            for (var index = 0; index < _slotRoots.Length; index++)
            {
                // RewardSlot0/1/2 are children of the CardRewardChoicePanel image, not direct
                // children of the presenter root (see EnsureSceneRewardLayout: slots are parented
                // under panelRect, one level below rewardRoot) - Find() only searches immediate
                // children, so the lookup has to go through the panel by name.
                var slotTransform = _authoredPresenterRoot.Find(RewardPanelName + "/RewardSlot" + index) as RectTransform;

                if (slotTransform == null)
                {
                    return false;
                }

                _slotRoots[index] = slotTransform;
                _slotHighlights[index] = slotTransform.GetComponent<Image>();
                _slotFrameImages[index] = FindImage(slotTransform, "SlotFrame", CommonFramePath);
                _slotTitleTexts[index] = FindText(slotTransform, "SlotTitle");
                _slotRarityTexts[index] = FindText(slotTransform, "SlotRarity");
                _slotDescriptionTexts[index] = FindText(slotTransform, "SlotDescription");

                var revealFlashImage = FindImage(slotTransform, "RevealFlash", RevealFlashSpritePath);

                if (revealFlashImage != null)
                {
                    revealFlashImage.color = Color.clear;
                }

                // Rarity is already conveyed by the frame color/art - the "Common"/"Rare"/etc.
                // label duplicated that information as clutter inside the card box. Kept bound
                // (not removed) since ApplyTagIcons()-style code and the bind-success check below
                // still reference the field; just hidden.
                if (_slotRarityTexts[index] != null)
                {
                    _slotRarityTexts[index].gameObject.SetActive(false);
                }

                _slotRevealFlashImages[index] = revealFlashImage;

                // Tag icons are optional and not part of the bind-success check below: the
                // authored RewardSlot hierarchy in SampleScene.unity predates this field and
                // adding it requires the Unity Editor (the scene has many same-named
                // SlotFrame/SlotDescription children across other presenters, too ambiguous to
                // hand-edit the raw YAML safely). ApplyTagIcons() no-ops per-slot when these are
                // null, so nothing breaks; opening this component once in the Editor after
                // clearing _authoredCanvasGroup would let EnsureSceneRewardLayout() create them.
                _slotTagIcons[index] = ResolveAuthoredTagIcons(slotTransform);

                if (_slotFrameImages[index] == null || _slotTitleTexts[index] == null || _slotRarityTexts[index] == null || _slotDescriptionTexts[index] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private Image[] ResolveAuthoredTagIcons(RectTransform slotTransform)
        {
            var icons = new Image[MaxTagIconsPerSlot];
            var foundAny = false;

            for (var iconIndex = 0; iconIndex < MaxTagIconsPerSlot; iconIndex++)
            {
                var child = slotTransform.Find("TagIcon" + iconIndex);
                icons[iconIndex] = child != null ? child.GetComponent<Image>() : null;
                foundAny |= icons[iconIndex] != null;
            }

            if (foundAny)
            {
                return icons;
            }

            // [ExecuteAlways]: creating children in Edit Mode would silently dirty RewardCanvas.unity.
            if (!Application.isPlaying)
            {
                return null;
            }

            // UI proposal 4 (2026-09-22): the authored RewardSlot hierarchy has no TagIcon children,
            // so on the path the game actually uses the tag row never existed and every card
            // showed no icons even once its tags were populated. The icons are added here as a
            // missing element rather than a re-layout: they are new children centred along the
            // slot's bottom edge, and nothing the scene authored is moved or resized.
            for (var iconIndex = 0; iconIndex < MaxTagIconsPerSlot; iconIndex++)
            {
                var iconImage = CreateImage("TagIcon" + iconIndex, slotTransform, null, Color.white, Image.Type.Simple);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                var iconRect = iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0f);
                iconRect.anchorMax = new Vector2(0.5f, 0f);
                iconRect.pivot = new Vector2(0.5f, 0f);
                iconRect.sizeDelta = new Vector2(AuthoredTagIconSize, AuthoredTagIconSize);
                iconImage.gameObject.SetActive(false);
                icons[iconIndex] = iconImage;
                _runtimeTagIcons.Add(iconImage);
            }

            return icons;
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

        // Only assigns when missing - never overwrites a sprite already set (by hand in the
        // Editor, or by an earlier successful bind), so this is safe to call on every bind.
        private static void RepairSprite(Image image, string spritePath)
        {
            if (image == null || image.sprite != null || string.IsNullOrWhiteSpace(spritePath))
            {
                return;
            }

            image.sprite = DebugSpriteFactory.LoadProjectSprite(spritePath);
        }

        // Find-only, no position/anchor/size touched - RewardCanvas.unity's own saved
        // RectTransform values are the sole source of truth (2026-09-17: "I want the no code
        // driven way"). Hand-drag something in the Scene view and it stays put.
        private static Text FindText(RectTransform parent, string name)
        {
            var child = parent != null ? parent.Find(name) as RectTransform : null;
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Image FindImage(RectTransform parent, string name, string spritePath)
        {
            var child = parent != null ? parent.Find(name) as RectTransform : null;

            if (child == null)
            {
                return null;
            }

            var image = child.GetComponent<Image>();
            RepairSprite(image, spritePath);
            return image;
        }

        private void RegisterWithUIManager()
        {
            var root = _authoredPresenterRoot != null ? _authoredPresenterRoot : _presenterRoot;
            _managedPanel = UIManager.RegisterPanel(root, "reward_screen", UIPanelLayer.BlockingOverlay, true, true, _canvasGroup);
        }

#if UNITY_EDITOR
        private void EnsureSceneRewardLayout()
        {
            if (_authoredPresenterRoot != null && _authoredCanvasGroup != null && _authoredTitleText != null)
            {
                return;
            }

            var canvasTransform = transform.Find(RewardCanvasName) as RectTransform;

            if (canvasTransform == null)
            {
                var canvasObject = new GameObject(RewardCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                canvasTransform = canvasObject.GetComponent<RectTransform>();
                StretchToParent(canvasTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            var rewardRoot = EnsureEditorUiChild(canvasTransform, RewardRootName, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _authoredPresenterRoot = rewardRoot;
            var isNewCanvasGroup = rewardRoot.GetComponent<CanvasGroup>() == null;
            _authoredCanvasGroup = rewardRoot.gameObject.GetOrAdd<CanvasGroup>();

            if (isNewCanvasGroup)
            {
                // This method runs from OnValidate() (via delayCall), independent of the normal
                // Awake()->Refresh() lifecycle that would otherwise immediately correct visibility.
                // AddComponent<CanvasGroup>() defaults to alpha=1/interactable=true/blocksRaycasts=
                // true, so a freshly (re)built reward panel sat fully visible and blocking gameplay
                // until something unrelated happened to trigger Refresh() (2026-09-15 playtest: "The
                // screen is shown the full time"). Start hidden like every other on-demand overlay in
                // this codebase does (see WaveStatePresenter's PausePanel/WaveBanner SetActive(false)
                // right after creation) - Refresh() still runs immediately after and will correctly
                // flip this back to visible if a reward is actually pending at that moment.
                _authoredCanvasGroup.alpha = 0f;
                _authoredCanvasGroup.interactable = false;
                _authoredCanvasGroup.blocksRaycasts = false;
            }
            var overlay = EnsureEditorImage(rewardRoot, "Overlay", null, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overlay.color = OverlayColor;
            var panel = EnsureEditorImage(rewardRoot, RewardPanelName, RewardChoicePanelTexturePath, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 680f));

            var panelRect = panel.rectTransform;
            _authoredTitleText = EnsureEditorText(panelRect, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(180f, -125f), new Vector2(-180f, -54f), TextAnchor.MiddleCenter, 28);
            _authoredResourceText = EnsureEditorText(panelRect, "Resources", new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(381f, -196f), new Vector2(-20f, -148f), TextAnchor.MiddleLeft, 18);
            _authoredTowerText = EnsureEditorText(panelRect, "TowerInfo", new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(20f, -196f), new Vector2(-353f, -148f), TextAnchor.MiddleRight, 18);
            _authoredDescriptionText = EnsureEditorText(panelRect, "Description", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(288f, 71f), new Vector2(-288f, 133f), TextAnchor.MiddleCenter, 18);

            for (var index = 0; index < 3; index++)
            {
                var slotRect = EnsureEditorUiChild(panelRect, "RewardSlot" + index, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((index - 1) * SlotSpacing, SlotCenterY), new Vector2(SlotWidth, SlotHeight));
                var slotImage = slotRect.gameObject.GetOrAdd<Image>();
                slotImage.color = Color.clear;
                slotImage.raycastTarget = false;
                EnsureEditorImage(slotRect, "RevealFlash", RevealFlashSpritePath, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
                EnsureEditorImage(slotRect, "SlotFrame", CommonFramePath, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                EnsureEditorText(slotRect, "SlotTitle", Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(22f, 28f), new Vector2(-22f, -286f), TextAnchor.UpperLeft, 18);
                var editorRarityText = EnsureEditorText(slotRect, "SlotRarity", Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(22f, 58f), new Vector2(-22f, -286f), TextAnchor.UpperLeft, 16);
                editorRarityText.gameObject.SetActive(false);
                var editorDescriptionText = EnsureEditorText(slotRect, "SlotDescription", Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(22f, 140f), new Vector2(-22f, -24f), TextAnchor.UpperLeft, 14);
                editorDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;

                for (var iconIndex = 0; iconIndex < MaxTagIconsPerSlot; iconIndex++)
                {
                    EnsureEditorImage(slotRect, "TagIcon" + iconIndex, null, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(22f + iconIndex * 36f, 14f), new Vector2(30f, 30f));
                }
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

        private Image EnsureEditorImage(RectTransform parent, string name, string spritePath, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var renderer = child.gameObject.GetOrAdd<CanvasRenderer>();
            var image = child.gameObject.GetOrAdd<Image>();
            image.sprite = string.IsNullOrWhiteSpace(spritePath) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private Text EnsureEditorText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, TextAnchor alignment, int fontSize)
        {
            var child = EnsureEditorUiChild(parent, name, anchorMin, anchorMax, pivot, Vector2.zero, Vector2.zero);
            StretchToParent(child, anchorMin, anchorMax, offsetMin, offsetMax);
            var renderer = child.gameObject.GetOrAdd<CanvasRenderer>();
            var text = child.gameObject.GetOrAdd<Text>();
            text.font = ResolveFont();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.14f, 0.12f, 0.1f, 0.98f);
            text.raycastTarget = false;
            // resizeTextForBestFit used to be on here, but combined with the HUD/reward canvases'
            // CanvasScaler (ScaleWithScreenSize) it rasterizes the dynamic font's glyph atlas at the
            // best-fit-computed size and then lets the canvas scale factor stretch that already-
            // rasterized bitmap non-1:1 - exactly the "text is blurry" symptom. The runtime-authored
            // path (ProceduralUIFactory.CreateText) never set this, so scene-authored text rendered
            // blurrier than freshly-built text; matching it here keeps both paths crisp.
            text.resizeTextForBestFit = false;
            text.text = name;
            return text;
        }
#endif
    }
}
