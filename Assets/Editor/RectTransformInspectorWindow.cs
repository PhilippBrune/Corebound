using UnityEditor;
using UnityEngine;

namespace Corebound.EditorTools
{
    // Approved alongside the Input Simulator (unused-assets/skills review, 2026-09-16/17) - a
    // recurring cost this session was diagnosing UI layout bugs (the reward-screen slot-title
    // overflow, the HUD nested-Canvas overlay bug) by hand: reading anchorMin/anchorMax/
    // anchoredPosition/sizeDelta out of the .unity YAML and manually working through Unity's
    // stretched-anchor formula to figure out an element's actual on-screen bounds relative to its
    // parent. This window does that arithmetic live for whatever RectTransform is selected, and
    // flags the specific failure mode that bit the reward screen: a child rect that extends
    // outside its parent's bounds despite looking "correct" by anchoredPosition/sizeDelta alone.
    //
    // World corners (GetWorldCorners) work identically in Edit and Play mode with no Update()/
    // Input System involvement, so unlike the Input Simulator this has no Play-Mode-only failure
    // surface - what you see in Edit mode is exactly what's baked into the scene file.
    public sealed class RectTransformInspectorWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Corebound/RectTransform Inspector")]
        public static void Open()
        {
            var window = GetWindow<RectTransformInspectorWindow>("Rect Inspector");
            window.minSize = new Vector2(320f, 200f);
        }

        private void OnEnable()
        {
            Selection.selectionChanged -= Repaint;
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            var selected = Selection.activeGameObject;

            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select a UI GameObject (with a RectTransform) in the Hierarchy or Scene view.", MessageType.Info);
                return;
            }

            var rect = selected.transform as RectTransform;

            if (rect == null)
            {
                EditorGUILayout.HelpBox(selected.name + " has a plain Transform, not a RectTransform - select a UI element under a Canvas.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawSelected(rect);
            EditorGUILayout.Space();
            DrawParentComparison(rect);
            EditorGUILayout.Space();
            DrawAncestorChain(rect);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSelected(RectTransform rect)
        {
            EditorGUILayout.LabelField(rect.name, EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Anchors", $"min={Fmt(rect.anchorMin)}  max={Fmt(rect.anchorMax)}  pivot={Fmt(rect.pivot)}");
            EditorGUILayout.LabelField("Anchored Position", Fmt(rect.anchoredPosition));
            EditorGUILayout.LabelField("Size Delta", Fmt(rect.sizeDelta));

            var isStretchedX = !Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x);
            var isStretchedY = !Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y);
            EditorGUILayout.LabelField("Stretch", (isStretchedX ? "horizontal " : "") + (isStretchedY ? "vertical" : "") + (isStretchedX || isStretchedY ? "" : "none (point anchor)"));

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var worldSize = corners[2] - corners[0];
            var worldCenter = (corners[0] + corners[2]) * 0.5f;
            EditorGUILayout.LabelField("World Size (w x h)", $"{worldSize.x:0.##} x {worldSize.y:0.##}");
            EditorGUILayout.LabelField("World Center", Fmt(worldCenter));
        }

        private static void DrawParentComparison(RectTransform rect)
        {
            var parent = rect.parent as RectTransform;

            if (parent == null)
            {
                EditorGUILayout.LabelField("Parent", "(none - this is a root RectTransform)");
                return;
            }

            EditorGUILayout.LabelField("Parent: " + parent.name, EditorStyles.boldLabel);

            var childCorners = new Vector3[4];
            rect.GetWorldCorners(childCorners);
            var parentCorners = new Vector3[4];
            parent.GetWorldCorners(parentCorners);

            // corners[0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right (Unity's own
            // GetWorldCorners order) - compare on-axis mins/maxes rather than assuming no
            // rotation, so this still reads correctly on a rotated element.
            var childMinX = Mathf.Min(childCorners[0].x, childCorners[1].x, childCorners[2].x, childCorners[3].x);
            var childMaxX = Mathf.Max(childCorners[0].x, childCorners[1].x, childCorners[2].x, childCorners[3].x);
            var childMinY = Mathf.Min(childCorners[0].y, childCorners[1].y, childCorners[2].y, childCorners[3].y);
            var childMaxY = Mathf.Max(childCorners[0].y, childCorners[1].y, childCorners[2].y, childCorners[3].y);

            var parentMinX = Mathf.Min(parentCorners[0].x, parentCorners[1].x, parentCorners[2].x, parentCorners[3].x);
            var parentMaxX = Mathf.Max(parentCorners[0].x, parentCorners[1].x, parentCorners[2].x, parentCorners[3].x);
            var parentMinY = Mathf.Min(parentCorners[0].y, parentCorners[1].y, parentCorners[2].y, parentCorners[3].y);
            var parentMaxY = Mathf.Max(parentCorners[0].y, parentCorners[1].y, parentCorners[2].y, parentCorners[3].y);

            var overflowLeft = parentMinX - childMinX;
            var overflowRight = childMaxX - parentMaxX;
            var overflowBottom = parentMinY - childMinY;
            var overflowTop = childMaxY - parentMaxY;

            var maxOverflow = Mathf.Max(overflowLeft, overflowRight, overflowBottom, overflowTop);
            const float overflowToleranceWorldUnits = 0.5f;

            if (maxOverflow > overflowToleranceWorldUnits)
            {
                var sides = "";
                if (overflowLeft > overflowToleranceWorldUnits) sides += $"left by {overflowLeft:0.#}  ";
                if (overflowRight > overflowToleranceWorldUnits) sides += $"right by {overflowRight:0.#}  ";
                if (overflowBottom > overflowToleranceWorldUnits) sides += $"bottom by {overflowBottom:0.#}  ";
                if (overflowTop > overflowToleranceWorldUnits) sides += $"top by {overflowTop:0.#}  ";
                EditorGUILayout.HelpBox("Extends outside parent bounds: " + sides.Trim(), MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("Fits within parent bounds", "yes");
            }
        }

        private static void DrawAncestorChain(RectTransform rect)
        {
            EditorGUILayout.LabelField("Ancestor chain (world position, screen-space canvases show canvas-relative units)", EditorStyles.boldLabel);

            var current = rect;

            while (current != null)
            {
                var label = current == rect ? "> " + current.name + " (selected)" : "  " + current.name;
                EditorGUILayout.LabelField(label, current.parent != null ? "anchoredPos=" + Fmt(current.anchoredPosition) : "(root)");
                current = current.parent as RectTransform;
            }
        }

        private static string Fmt(Vector2 v)
        {
            return $"({v.x:0.##}, {v.y:0.##})";
        }

        private static string Fmt(Vector3 v)
        {
            return $"({v.x:0.##}, {v.y:0.##}, {v.z:0.##})";
        }
    }
}
