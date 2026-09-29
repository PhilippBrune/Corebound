using Corebound.Gameplay.Factory;
using Corebound.Gameplay.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI.Gameplay
{
    // OPEN_TOPICS 2.2, half one: "is it on?"
    //
    // The Factory's amplifier paid out with nothing on screen to say it existed, so a player could
    // build towers, receive a better rate on everything the chain made, and never learn that the
    // two were connected. Vision section 5.5 asks that a player can recognise a system is working.
    //
    // This is the readout: a pip per chain tier, and the live amplifier multiplier beside it. It
    // also finally wires factory_chain_tier_indicator.png, which has been finished and unreferenced
    // since it was delivered.
    //
    // Deliberately small and in a corner. The Factory is a supporting system by design boundary 2,
    // and a large always-on panel for it would be exactly the clutter section 19 warns about. The
    // whole row hides when there is no tier and no bonus, so a run that ignores the Factory sees
    // nothing at all.
    public sealed class FactoryChainIndicatorPresenter : MonoBehaviour
    {
        private const string IndicatorSpritePath = "Assets/Art/UI/HUD/factory_chain_tier_indicator.png";
        private const float RefreshIntervalSeconds = 0.35f;
        private const int MaxPips = 4;

        private static readonly Color PipLitColor = new Color(1f, 0.78f, 0.35f, 0.95f);
        private static readonly Color PipDimColor = new Color(0.4f, 0.45f, 0.52f, 0.55f);

        [SerializeField] private FactoryService _factoryService;

        private RectTransform _root;
        private Image[] _pips;
        private Text _bonusText;
        private float _refreshTimer;

        public void Build(RectTransform parent, Font font)
        {
            if (parent == null || _root != null)
            {
                return;
            }

            var rootObject = new GameObject("FactoryChainIndicator", typeof(RectTransform));
            rootObject.transform.SetParent(parent, false);
            _root = rootObject.GetComponent<RectTransform>();
            _root.anchorMin = new Vector2(0f, 0f);
            _root.anchorMax = new Vector2(0f, 0f);
            _root.pivot = new Vector2(0f, 0f);
            _root.anchoredPosition = new Vector2(18f, 18f);
            _root.sizeDelta = new Vector2(170f, 22f);

            var sprite = DebugSpriteFactory.LoadProjectSprite(IndicatorSpritePath);
            _pips = new Image[MaxPips];

            for (var index = 0; index < MaxPips; index++)
            {
                var pipObject = new GameObject("Pip" + index, typeof(RectTransform), typeof(Image));
                pipObject.transform.SetParent(_root, false);
                var pip = pipObject.GetComponent<Image>();
                pip.sprite = sprite;
                pip.preserveAspect = true;
                pip.raycastTarget = false;
                var rect = pip.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(18f, 18f);
                rect.anchoredPosition = new Vector2(index * 21f, 0f);
                _pips[index] = pip;
            }

            _bonusText = ProceduralUIFactory.CreateText(
                "FactoryBonus", _root, font, 13, FontStyle.Bold, PipLitColor, TextAnchor.MiddleLeft);
            var textRect = _bonusText.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.sizeDelta = new Vector2(80f, 20f);
            textRect.anchoredPosition = new Vector2(MaxPips * 21f + 6f, 0f);
            _bonusText.raycastTarget = false;

            Refresh();
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            _refreshTimer -= Time.unscaledDeltaTime;

            if (_refreshTimer > 0f)
            {
                return;
            }

            _refreshTimer = RefreshIntervalSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (_factoryService == null)
            {
                _factoryService = FindFirstObjectByType<FactoryService>();
            }

            var tier = _factoryService != null ? _factoryService.HighestChainTier : 0;
            var multiplier = _factoryService != null ? _factoryService.ActiveAmplifierMultiplier : 1f;
            var hasBonus = multiplier > 1.001f;
            var show = tier > 0 || hasBonus;

            if (_root.gameObject.activeSelf != show)
            {
                _root.gameObject.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            for (var index = 0; index < _pips.Length; index++)
            {
                if (_pips[index] != null)
                {
                    _pips[index].color = index < tier ? PipLitColor : PipDimColor;
                }
            }

            if (_bonusText != null)
            {
                // The multiplier only appears while an amplifier is actually live, so its presence
                // is itself the answer to "is it on?".
                _bonusText.text = hasBonus ? "x" + multiplier.ToString("0.0#") : string.Empty;
            }
        }
    }
}
