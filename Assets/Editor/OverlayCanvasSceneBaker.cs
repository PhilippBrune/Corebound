using System.Collections.Generic;
using System.IO;
using Corebound.Gameplay.Shared;
using Corebound.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Corebound.EditorTools
{
    // Open Topics 4, owner decision (b), 2026-09-29: the Boss HUD, the system notification and the
    // relic reveal move into an authored canvas scene, like Reward/Shop/HUD - which is where the
    // canvas regressions stopped. All three used to build themselves in code with their pixel
    // offsets hard-coded in the presenter.
    //
    // This writes that scene once, from the layout the presenters used to build, so nothing moves
    // on screen on the day of the switch. From then on OverlayCanvas.unity is the source of truth:
    // open it and edit it like HUDCanvas. The baker never overwrites an existing file - it only
    // runs when the scene is missing, or from the menu with a confirmation - so an edited scene
    // cannot be silently reset to these numbers.
    [InitializeOnLoad]
    public static class OverlayCanvasSceneBaker
    {
        public const string ScenePath = "Assets/Scenes/" + AdditiveCanvasSceneLoader.OverlayCanvasSceneName + ".unity";

        static OverlayCanvasSceneBaker()
        {
            EditorApplication.delayCall += BakeIfMissing;
        }

        private static void BakeIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (File.Exists(ScenePath))
            {
                EnsureInBuildSettings();
                AddMissingCanvases();
                return;
            }

            Bake();
            Debug.Log("[OverlayCanvas] " + ScenePath + " did not exist and was baked from the presenters' former layout.");
        }

        [MenuItem("Corebound/UI/Rebake Overlay Canvas Scene")]
        private static void RebakeFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                    "Rebake Overlay Canvas",
                    ScenePath + " exists. Rebaking replaces every edit made to it with the original layout.",
                    "Replace",
                    "Cancel"))
            {
                return;
            }

            Bake();
        }

        private static readonly string[] CanvasRootNames = { "BossHudCanvas", "NotificationCanvas", "RelicRevealCanvas", "CoreVoiceCanvas", "EndingCanvas" };

        // A canvas added to the overlay after the scene was first baked (2026-09-30: the Core's
        // voice and the ending card) is added to the existing scene - built into it, saved, and
        // nothing already in it touched - so edits made to the scene survive.
        private static void AddMissingCanvases()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }

            var present = new HashSet<string>();

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                present.Add(rootObject.name);
            }

            var font = UIFontResolver.Resolve();
            var added = new List<string>();

            if (!present.Contains("CoreVoiceCanvas"))
            {
                BuildCoreVoice(font, scene);
                added.Add("CoreVoiceCanvas");
            }

            if (!present.Contains("EndingCanvas"))
            {
                BuildEndingCard(font, scene);
                added.Add("EndingCanvas");
            }

            if (added.Count > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[OverlayCanvas] added " + string.Join(", ", added) + " to " + ScenePath);
            }

            if (openedHere)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Bake()
        {
            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var font = UIFontResolver.Resolve();

            BuildBossHud(font, scene);
            BuildNotification(font, scene);
            BuildRelicReveal(font, scene);
            BuildCoreVoice(font, scene);
            BuildEndingCard(font, scene);

            // A new additive scene becomes the active one, so anything that lazily creates a
            // GameObject while it is (the first bake caught a UIManager singleton) lands in it.
            // Only the three canvases belong here.
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (System.Array.IndexOf(CanvasRootNames, rootObject.name) < 0)
                {
                    Debug.LogWarning("[OverlayCanvas] removed stray '" + rootObject.name + "' from the baked scene.");
                    Object.DestroyImmediate(rootObject);
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            if (previousActive.IsValid() && previousActive.isLoaded)
            {
                SceneManager.SetActiveScene(previousActive);
            }

            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
            EnsureInBuildSettings();
        }

        private static void EnsureInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (var entry in scenes)
            {
                if (entry.path == ScenePath)
                {
                    return;
                }
            }

            if (!File.Exists(ScenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            // Without this the change lives only in memory until the Editor quits, and a crash
            // loses it - LoadScene by name then fails in Play Mode as well as in a build.
            AssetDatabase.SaveAssets();
            Debug.Log("[OverlayCanvas] added " + ScenePath + " to the build settings.");
        }

        // ---- The three canvases, as the presenters built them until 2026-09-30 -------------------

        private static RectTransform CreateCanvas(string name, int sortingOrder, Scene scene, bool raycaster)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            if (raycaster)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            return canvasObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            var rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image AddImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text AddText(RectTransform rect, Font font, int size, FontStyle style, TextAnchor anchor, Color color, string placeholder)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.text = placeholder;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void BuildBossHud(Font font, Scene scene)
        {
            var root = CreateCanvas("BossHudCanvas", 0, scene, true);

            var panel = CreateRect("BossPanel", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(720f, 75f));
            AddImage(panel, new Color(0.08f, 0.08f, 0.12f, 0.85f));

            var name = CreateRect("BossNameText", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -8f), new Vector2(400f, 30f));
            AddText(name, font, 24, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.3f, 1f), "BOSS");

            var phase = CreateRect("PhaseText", panel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -8f), new Vector2(260f, 30f));
            AddText(phase, font, 20, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.4f, 0.85f, 1f, 1f), "Phase 1");

            var barBg = CreateRect("HealthBarBg", panel, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-40f, 22f));
            AddImage(barBg, new Color(0.2f, 0.05f, 0.05f, 0.9f));

            var barFill = CreateRect("HealthBarFill", barBg, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(680f, 0f));
            AddImage(barFill, new Color(0.85f, 0.15f, 0.15f, 1f));

            var health = CreateRect("HealthText", barBg, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(health, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddText(health, font, 16, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, "100%");

            var warning = CreateRect("WarningPanel", root, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650f, 60f));
            AddImage(warning, new Color(0.4f, 0.02f, 0.02f, 0.88f));

            var warningText = CreateRect("WarningText", warning, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(warningText, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddText(warningText, font, 32, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.2f, 1f), "WARNING");

            // Hidden until a boss spawns or a notice is shown, as the presenter always started them.
            panel.gameObject.SetActive(false);
            warning.gameObject.SetActive(false);
        }

        private static void BuildNotification(Font font, Scene scene)
        {
            var root = CreateCanvas("NotificationCanvas", 500, scene, false);

            var panel = CreateRect("NotificationPanel", root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(620f, 56f));
            AddImage(panel, new Color(0.04f, 0.08f, 0.12f, 0.9f));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var text = CreateRect("NotificationText", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(text, Vector2.zero, Vector2.one, new Vector2(18f, 6f), new Vector2(-18f, -6f));
            AddText(text, font, 20, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white, string.Empty);
        }

        // STORY_2026-09-30 (S2-1): the Core's lines. Low in the frame, clear of the boss bar, above
        // the HUD and the relic reveal, below notifications.
        private static void BuildCoreVoice(Font font, Scene scene)
        {
            var root = CreateCanvas("CoreVoiceCanvas", 470, scene, false);

            var panel = CreateRect("CoreVoicePanel", root, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 72f));
            AddImage(panel, new Color(0.03f, 0.05f, 0.08f, 0.62f));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var text = CreateRect("CoreVoiceText", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(text, Vector2.zero, Vector2.one, new Vector2(24f, 6f), new Vector2(-24f, -6f));
            AddText(text, font, 26, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.85f, 0.93f, 1f, 1f), string.Empty);
        }

        // STORY_2026-09-30 (S4-1): the ending, over the Run Complete screen at the first full clear.
        private static void BuildEndingCard(Font font, Scene scene)
        {
            var root = CreateCanvas("EndingCanvas", 480, scene, false);

            var panel = CreateRect("EndingPanel", root, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 470f));
            AddImage(panel, new Color(0.02f, 0.03f, 0.05f, 0.9f));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var title = CreateRect("EndingTitle", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(title, new Vector2(0f, 0.8f), new Vector2(1f, 0.96f), new Vector2(40f, 0f), new Vector2(-40f, 0f));
            AddText(title, font, 40, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.5f, 1f), string.Empty);

            var body = CreateRect("EndingText", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(body, new Vector2(0f, 0.06f), new Vector2(1f, 0.78f), new Vector2(60f, 0f), new Vector2(-60f, 0f));
            AddText(body, font, 25, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.9f, 0.93f, 0.97f, 1f), string.Empty);
        }

        private static void BuildRelicReveal(Font font, Scene scene)
        {
            // Above the HUD and wave banner, below the modal overlays (pause, reward, shop).
            var root = CreateCanvas("RelicRevealCanvas", 450, scene, false);

            var panel = CreateRect("RelicRevealPanel", root, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 340f));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // Both images get their sprites at runtime from the art pipeline (StreamingAssets
            // first), like every other runtime-art Image; the scene only holds their place.
            var flash = CreateRect("RevealFlash", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(flash, new Vector2(-0.25f, -0.2f), new Vector2(1.25f, 1.2f), Vector2.zero, Vector2.zero);
            AddImage(flash, Color.white);

            var frame = CreateRect("Frame", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddImage(frame, Color.white).type = Image.Type.Sliced;

            var kind = CreateRect("KindText", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(kind, new Vector2(0f, 0.82f), new Vector2(1f, 0.96f), Vector2.zero, Vector2.zero);
            AddText(kind, font, 18, FontStyle.BoldAndItalic, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.4f, 1f), "Relic Gained");

            var name = CreateRect("NameText", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(name, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
            AddText(name, font, 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.16f, 0.14f, 0.11f, 0.98f), "Relic");

            var description = CreateRect("DescriptionText", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(description, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.64f), Vector2.zero, Vector2.zero);
            AddText(description, font, 14, FontStyle.Normal, TextAnchor.UpperCenter, new Color(0.2f, 0.18f, 0.14f, 0.95f), string.Empty)
                .verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
