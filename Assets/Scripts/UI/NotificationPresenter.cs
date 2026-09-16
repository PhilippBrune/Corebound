using System.Collections.Generic;
using Corebound.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.UI
{
    // The "system notices/errors" UI surface UI.md's minimum UI structures call for - previously
    // entirely absent despite NotificationStyleData already modelling exactly this. Exposes a
    // static Show(...) any system can call, and wires itself to ISaveManager's SaveFailed/
    // LoadFailed events (added alongside SaveSystem's atomic-write hardening) so a save/load
    // problem is now something the player can actually see, not only a Debug.LogError.
    public sealed class NotificationPresenter : MonoBehaviour
    {
        private static NotificationPresenter _instance;

        [SerializeField] private NotificationStyleData _styleData;
        [SerializeField] private UnityEngine.Object _saveManagerSource;

        private readonly Queue<string> _pendingMessages = new Queue<string>();
        private Text _messageText;
        private CanvasGroup _canvasGroup;
        private float _timer;
        private bool _isShowing;

        private ISaveManager SaveManager => SaveManagerResolver.Resolve(_saveManagerSource);

        public static void Show(string message)
        {
            if (_instance != null)
            {
                _instance.Enqueue(message);
            }
        }

        private void Awake()
        {
            _instance = this;
            BuildUi();
        }

        private void OnEnable()
        {
            var saveManager = SaveManager;

            if (saveManager != null)
            {
                saveManager.SaveFailed += OnSaveFailed;
                saveManager.LoadFailed += OnLoadFailed;
            }
        }

        private void OnDisable()
        {
            var saveManager = SaveManager;

            if (saveManager != null)
            {
                saveManager.SaveFailed -= OnSaveFailed;
                saveManager.LoadFailed -= OnLoadFailed;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnSaveFailed(string path, string message)
        {
            Enqueue(_styleData != null ? _styleData.SaveFailedText : "Could not save your progress. Check available disk space and try again.");
        }

        private void OnLoadFailed(string path, string message)
        {
            Enqueue(_styleData != null ? _styleData.LoadFailedText : "A save file could not be read and was skipped.");
        }

        private void Enqueue(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                _pendingMessages.Enqueue(message);
            }
        }

        private void Update()
        {
            if (!_isShowing)
            {
                if (_pendingMessages.Count == 0)
                {
                    return;
                }

                _messageText.text = _pendingMessages.Dequeue();
                _isShowing = true;
                _timer = 0f;
            }

            _timer += Time.deltaTime;
            var displaySeconds = _styleData != null ? _styleData.DisplayDurationSeconds : 2.25f;
            var fadeIn = _styleData != null ? _styleData.FadeInSeconds : 0.12f;
            var fadeOut = _styleData != null ? _styleData.FadeOutSeconds : 0.16f;

            if (_timer < fadeIn)
            {
                _canvasGroup.alpha = Mathf.Clamp01(_timer / Mathf.Max(0.01f, fadeIn));
            }
            else if (_timer < displaySeconds - fadeOut)
            {
                _canvasGroup.alpha = 1f;
            }
            else if (_timer < displaySeconds)
            {
                _canvasGroup.alpha = Mathf.Clamp01((displaySeconds - _timer) / Mathf.Max(0.01f, fadeOut));
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _isShowing = false;
            }
        }

        private void BuildUi()
        {
            var canvasRoot = new GameObject("NotificationCanvas");
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelObject = new GameObject("NotificationPanel");
            panelObject.transform.SetParent(canvasRoot.transform, false);
            var panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -36f);
            panelRect.sizeDelta = new Vector2(620f, 56f);

            var backgroundColor = _styleData != null ? _styleData.BackgroundColor : new Color(0.04f, 0.08f, 0.12f, 0.9f);
            var image = panelObject.AddComponent<Image>();
            image.color = backgroundColor;

            _canvasGroup = panelObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            var textObject = new GameObject("NotificationText");
            textObject.transform.SetParent(panelObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 6f);
            textRect.offsetMax = new Vector2(-18f, -6f);
            _messageText = textObject.AddComponent<Text>();
            _messageText.font = UIFontResolver.Resolve();
            _messageText.fontSize = 20;
            _messageText.alignment = TextAnchor.MiddleCenter;
            _messageText.color = _styleData != null ? _styleData.ForegroundColor : Color.white;
            _messageText.horizontalOverflow = HorizontalWrapMode.Wrap;

            UIManager.RegisterPanel(panelRect, "system_notice", UIPanelLayer.System, false, false, _canvasGroup);
        }
    }
}
