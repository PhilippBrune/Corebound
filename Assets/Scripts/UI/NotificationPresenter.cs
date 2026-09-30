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
            TryBind();
        }

        private void OnEnable()
        {
            Corebound.Gameplay.Shared.AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;
            var saveManager = SaveManager;

            if (saveManager != null)
            {
                saveManager.SaveFailed += OnSaveFailed;
                saveManager.LoadFailed += OnLoadFailed;
            }
        }

        private void OnDisable()
        {
            Corebound.Gameplay.Shared.AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;
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
            // Messages queue until the overlay scene has loaded, then play in order.
            if (_canvasGroup == null || _messageText == null)
            {
                return;
            }

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

        // Open Topics 4 (b), 2026-09-30: the panel lives in OverlayCanvas.unity. Only the style
        // asset's colours are applied here - those are data, not layout.
        private void TryBind()
        {
            if (_canvasGroup != null)
            {
                return;
            }

            var root = Corebound.Gameplay.Shared.AdditiveCanvasSceneLoader.FindRoot(
                Corebound.Gameplay.Shared.AdditiveCanvasSceneLoader.OverlayCanvasSceneName, "NotificationCanvas");
            var panel = root != null ? root.transform.Find("NotificationPanel") as RectTransform : null;

            if (panel == null)
            {
                return;
            }

            _canvasGroup = panel.GetComponent<CanvasGroup>();
            var textTransform = panel.Find("NotificationText");
            _messageText = textTransform != null ? textTransform.GetComponent<Text>() : null;
            var background = panel.GetComponent<Image>();

            if (_styleData != null)
            {
                if (background != null)
                {
                    background.color = _styleData.BackgroundColor;
                }

                if (_messageText != null)
                {
                    _messageText.color = _styleData.ForegroundColor;
                }
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                UIManager.RegisterPanel(panel, "system_notice", UIPanelLayer.System, false, false, _canvasGroup);
            }
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName == Corebound.Gameplay.Shared.AdditiveCanvasSceneLoader.OverlayCanvasSceneName)
            {
                TryBind();
            }
        }
    }
}
