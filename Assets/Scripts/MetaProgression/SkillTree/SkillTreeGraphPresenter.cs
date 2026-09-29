using System.Collections.Generic;
using Corebound.Gameplay.Shared;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.MetaProgression.SkillTree
{
    // The node graph from SKILL_TREE_NODE_GRAPH_2026-09-28: radial branches fanning from a central
    // portrait (N2), key-node positions authorable (N1), node state readable without colour alone
    // (N4).
    //
    // Playtest 2026-09-29: "I see the old and the new system for progression. It should be only
    // one." The first version drew the graph *behind* the old row list and left the list in charge
    // of selection and purchasing. On a branch tab the graph is now the whole interface: the
    // selected node carries a ring, the mouse selects and buys on it, and the stick moves across it
    // (MetaProgressionRouteTargetController, using SkillTreeGraphLayout for the same positions).
    //
    // The whole tree is drawn so a branch keeps its place in the whole; the branch of the active
    // tab is at full strength and the other three are dimmed.
    public sealed class SkillTreeGraphPresenter : MonoBehaviour
    {
        private const string KeyFramePath = "Assets/Art/UI/SkillTree/skill_node_key_frame.png";
        private const string SubFramePath = "Assets/Art/UI/SkillTree/skill_node_sub_frame.png";
        private const string LinkPath = "Assets/Art/UI/SkillTree/skill_link_segment.png";
        private const string PortraitFramePath = "Assets/Art/UI/SkillTree/character_portrait_frame.png";

        // Each file is a 911 px canvas with the art in only part of it: the sub-node frame fills
        // 44% of its width and the link 13% of its height, so drawn whole they rendered as dots
        // and hairlines. Cropped to their alpha bounds (bottom-left origin), measured 2026-09-29.
        private static readonly Rect KeyFrameRect = new Rect(120f, 79f, 671f, 753f);
        private static readonly Rect SubFrameRect = new Rect(251f, 251f, 402f, 399f);
        private static readonly Rect LinkRect = new Rect(88f, 396f, 733f, 120f);
        private static readonly Rect PortraitRect = new Rect(156f, 167f, 599f, 588f);

        private const float InactiveBranchAlpha = 0.32f;
        private const float SelectionRingScale = 1.5f;

        private static readonly Color SelectionRingColor = new Color(1f, 0.95f, 0.75f, 1f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<HitTarget> _hitTargets = new List<HitTarget>();

        private RectTransform _root;

        private readonly struct HitTarget
        {
            public HitTarget(RectTransform rect, int keyIndex, int subIndex)
            {
                Rect = rect;
                KeyIndex = keyIndex;
                SubIndex = subIndex;
            }

            public RectTransform Rect { get; }

            public int KeyIndex { get; }

            public int SubIndex { get; }
        }

        // area: the rectangle the graph is centred in and scaled to fit.
        // selectedKey / selectedSub: the node to ring, in the active branch; -1 for none.
        public void Build(
            RectTransform area,
            SkillTreeDefinitionData tree,
            MetaProgressionSaveData save,
            int activeBranchIndex,
            int selectedKey,
            int selectedSub)
        {
            Clear();

            if (area == null || tree == null || tree.Branches == null || tree.Branches.Length == 0)
            {
                return;
            }

            if (_root == null)
            {
                var rootObject = new GameObject("SkillTreeGraph", typeof(RectTransform));
                rootObject.transform.SetParent(area, false);
                _root = rootObject.GetComponent<RectTransform>();
                _root.anchorMin = new Vector2(0.5f, 0.5f);
                _root.anchorMax = new Vector2(0.5f, 0.5f);
                _root.pivot = new Vector2(0.5f, 0.5f);
                _root.sizeDelta = Vector2.zero;
            }

            // A camera on the board: the active branch is centred and at full size, the portrait
            // and the neighbouring branches show at the edges for context, and the area's mask
            // clips the rest. Fitting the whole tree made every node too small to read.
            if (area.GetComponent<RectMask2D>() == null)
            {
                area.gameObject.AddComponent<RectMask2D>();
            }

            var activeBranch = activeBranchIndex >= 0 && activeBranchIndex < tree.Branches.Length ? tree.Branches[activeBranchIndex] : null;
            var keyCount = activeBranch != null && activeBranch.KeyNodes != null ? activeBranch.KeyNodes.Length : 0;
            _root.localScale = Vector3.one;
            _root.anchoredPosition = activeBranch != null
                ? -SkillTreeGraphLayout.BranchCentre(activeBranchIndex, tree.Branches.Length, keyCount)
                : Vector2.zero;

            BuildPortrait();

            var branches = tree.Branches;

            // Inactive branches first, so the active one is drawn on top where the two meet.
            for (var pass = 0; pass < 2; pass++)
            {
                for (var branchIndex = 0; branchIndex < branches.Length; branchIndex++)
                {
                    var active = branchIndex == activeBranchIndex;

                    if (active != (pass == 1))
                    {
                        continue;
                    }

                    BuildBranch(tree, save, branchIndex, active, active ? selectedKey : -1, active ? selectedSub : -1);
                }
            }
        }

        private void BuildBranch(SkillTreeDefinitionData tree, MetaProgressionSaveData save, int branchIndex, bool active, int selectedKey, int selectedSub)
        {
            var branches = tree.Branches;
            var branch = branches[branchIndex];

            if (branch == null || branch.KeyNodes == null)
            {
                return;
            }

            var alpha = active ? 1f : InactiveBranchAlpha;
            var previous = Vector2.zero;
            var firstLinkStart = SkillTreeGraphLayout.BranchAxis(branchIndex, branches.Length) * (SkillTreeGraphLayout.PortraitSize * 0.5f);

            for (var keyIndex = 0; keyIndex < branch.KeyNodes.Length; keyIndex++)
            {
                var keyNode = branch.KeyNodes[keyIndex];

                if (keyNode == null)
                {
                    continue;
                }

                var keyPosition = SkillTreeGraphLayout.KeyNode(keyNode, branchIndex, branches.Length, keyIndex);
                var keyState = SkillTreeRules.ResolveKeyNodeState(save, tree, branch, keyNode);
                DrawLink(keyIndex == 0 ? firstLinkStart : previous, keyPosition, branch.AccentColor, keyState, alpha, 10f);
                previous = keyPosition;

                var subNodes = keyNode.SubNodes;

                if (subNodes != null)
                {
                    for (var subIndex = 0; subIndex < subNodes.Length; subIndex++)
                    {
                        var subNode = subNodes[subIndex];

                        if (subNode == null)
                        {
                            continue;
                        }

                        var subPosition = SkillTreeGraphLayout.SubNode(keyPosition, subIndex);
                        var subState = SkillTreeRules.ResolveSubNodeState(save, tree, keyNode, subNode);
                        var rank = save != null ? save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId) : 0;
                        // Sub-nodes on the same side chain outwards from the key node.
                        var linkFrom = subIndex % 2 == 0 ? keyPosition : SkillTreeGraphLayout.SubNode(keyPosition, subIndex - 1);
                        DrawLink(linkFrom, subPosition, branch.AccentColor, subState, alpha, 7f);

                        if (keyIndex == selectedKey && subIndex == selectedSub)
                        {
                            DrawSelectionRing(subPosition, SkillTreeGraphLayout.SubNodeSize, SubFramePath);
                        }

                        var subRect = DrawNode(subPosition, SkillTreeGraphLayout.SubNodeSize, SubFramePath, branch.AccentColor, subState, alpha,
                            subNode.CostPerRank, rank, subNode.MaxRank);

                        if (active)
                        {
                            _hitTargets.Add(new HitTarget(subRect, keyIndex, subIndex));
                        }
                    }
                }

                if (keyIndex == selectedKey && selectedSub < 0)
                {
                    DrawSelectionRing(keyPosition, SkillTreeGraphLayout.KeyNodeSize, KeyFramePath);
                }

                var keyRect = DrawNode(keyPosition, SkillTreeGraphLayout.KeyNodeSize, KeyFramePath, branch.AccentColor, keyState, alpha,
                    keyNode.Cost, 0, 0);

                if (active)
                {
                    _hitTargets.Add(new HitTarget(keyRect, keyIndex, -1));
                    DrawKeyName(keyPosition, keyNode.DisplayName, keyIndex == selectedKey && selectedSub < 0);
                }
            }

            DrawAscensionRing(tree, save, branchIndex, branch, previous, alpha, active, selectedKey, selectedSub);
        }

        // P7-3: the branch's Ascension bonuses one tier past its last key node, in gold. Dimmed and
        // showing the level cap until Ascension unlocks; afterwards each shows its rank, or its
        // price when it can be bought.
        private readonly List<AscensionBonusData> _ringBuffer = new List<AscensionBonusData>();
        private static readonly Color AscensionAccent = new Color(1f, 0.82f, 0.36f, 1f);

        private void DrawAscensionRing(SkillTreeDefinitionData tree, MetaProgressionSaveData save, int branchIndex, SkillBranchData branch,
            Vector2 lastKeyPosition, float alpha, bool active, int selectedKey, int selectedSub)
        {
            tree.CollectAscensionBonuses(branch.BranchId, _ringBuffer);
            var ringKey = branch.KeyNodes.Length;

            for (var slot = 0; slot < _ringBuffer.Count; slot++)
            {
                var bonus = _ringBuffer[slot];
                var position = SkillTreeGraphLayout.RingNode(branchIndex, tree.Branches.Length, ringKey, slot, _ringBuffer.Count);
                var state = SkillTreeRules.ResolveAscensionNodeState(save, bonus);
                var rank = save != null ? save.GetAscensionNodeRank(bonus.BonusId) : 0;
                var unlocked = state != SkillNodeState.LockedPrerequisite;
                DrawLink(lastKeyPosition, position, AscensionAccent, unlocked ? SkillNodeState.Available : SkillNodeState.LockedPrerequisite, alpha, 5f);

                if (selectedKey == ringKey && selectedSub == slot)
                {
                    DrawSelectionRing(position, SkillTreeGraphLayout.RingNodeSize, KeyFramePath);
                }

                // Rank when owned, price when it can be bought now, "70" while still locked by level.
                var caption = !unlocked
                    ? MetaProgressionSaveData.MaxLevel.ToString()
                    : rank > 0 ? rank.ToString() : SkillTreeDefinitionData.AscensionNodeCost(rank) + "AP";
                var rect = DrawNode(position, SkillTreeGraphLayout.RingNodeSize, KeyFramePath, AscensionAccent,
                    rank > 0 ? SkillNodeState.Purchased : state, alpha, 0, 0, 0, caption);

                if (active)
                {
                    _hitTargets.Add(new HitTarget(rect, ringKey, slot));
                }
            }
        }

        // Screen point to node, for the mouse. Only the active branch answers: a click on a dimmed
        // branch should not buy something on a tab the player is not looking at.
        public bool TryHitNode(Vector2 screenPoint, out int keyIndex, out int subIndex)
        {
            for (var index = _hitTargets.Count - 1; index >= 0; index--)
            {
                var target = _hitTargets[index];

                if (target.Rect != null && RectTransformUtility.RectangleContainsScreenPoint(target.Rect, screenPoint, null))
                {
                    keyIndex = target.KeyIndex;
                    subIndex = target.SubIndex;
                    return true;
                }
            }

            keyIndex = -1;
            subIndex = -1;
            return false;
        }

        private void BuildPortrait()
        {
            var sprite = LoadCropped(PortraitFramePath);
            var image = ProceduralUIBuilder.CreateImage("Portrait", _root, sprite, Color.white);
            image.raycastTarget = false;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(SkillTreeGraphLayout.PortraitSize, SkillTreeGraphLayout.PortraitSize);
            _spawned.Add(image.gameObject);
        }

        private void DrawLink(Vector2 from, Vector2 to, Color accent, SkillNodeState state, float alpha, float thickness)
        {
            var delta = to - from;
            var length = delta.magnitude;

            if (length < 1f)
            {
                return;
            }

            var sprite = LoadCropped(LinkPath);
            // Unique per link: ProceduralUIFactory reuses a child it finds by name, so a shared
            // name makes every link collapse onto one object.
            var image = ProceduralUIBuilder.CreateImage("Link_" + _spawned.Count, _root, sprite, WithAlpha(ResolveStateColor(accent, state, true), alpha));
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _spawned.Add(image.gameObject);
        }

        private void DrawSelectionRing(Vector2 position, float size, string framePath)
        {
            var image = ProceduralUIBuilder.CreateImage("Ring_" + _spawned.Count, _root, LoadCropped(framePath), SelectionRingColor);
            image.raycastTarget = false;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.one * (size * SelectionRingScale);
            _spawned.Add(image.gameObject);
        }

        private RectTransform DrawNode(
            Vector2 position,
            float size,
            string framePath,
            Color accent,
            SkillNodeState state,
            float alpha,
            int cost,
            int rank,
            int maxRank,
            string captionOverride = null)
        {
            var sprite = LoadCropped(framePath);
            var image = ProceduralUIBuilder.CreateImage("Node_" + _spawned.Count, _root, sprite, WithAlpha(ResolveStateColor(accent, state, false), alpha));
            image.raycastTarget = false;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);
            _spawned.Add(image.gameObject);

            // N4: state has to be readable without colour. Owned nodes show their rank, buyable
            // ones show their price, and locked ones a dash - so the graph still parses in
            // greyscale.
            var caption = state == SkillNodeState.Purchased
                ? (maxRank > 0 ? rank + "/" + maxRank : "•")
                : (state == SkillNodeState.Available || state == SkillNodeState.Unaffordable
                    ? (maxRank > 0 && rank > 0 ? rank + "/" + maxRank : cost.ToString())
                    : "-");

            var textColor = state == SkillNodeState.Unaffordable ? new Color(1f, 0.55f, 0.45f, 1f) : Color.white;
            var label = ProceduralUIBuilder.CreateText("NodeLabel_" + _spawned.Count, rect, Mathf.RoundToInt(size * 0.36f),
                FontStyle.Bold, TextAnchor.MiddleCenter, WithAlpha(textColor, alpha));
            label.raycastTarget = false;
            ProceduralUIBuilder.StretchToParent(label.rectTransform);
            // Captions only on the active branch: on a dimmed one they are noise at the edge of
            // the view, and the node's tint already says owned, open or locked.
            label.text = alpha >= 0.99f ? captionOverride ?? caption : string.Empty;
            return rect;
        }

        // Key-node names for the active branch only, beside the row so they never sit on the next
        // tier. Sub-node names live in the details column; forty labels would bury the shape.
        private void DrawKeyName(Vector2 keyPosition, string displayName, bool selected)
        {
            var axis = keyPosition.sqrMagnitude > 0.0001f ? keyPosition.normalized : Vector2.up;
            var across = new Vector2(-axis.y, axis.x);

            // Prefer the right-hand side for a vertical branch and underneath for a horizontal one.
            if (Mathf.Abs(across.x) >= Mathf.Abs(across.y) ? across.x < 0f : across.y > 0f)
            {
                across = -across;
            }

            var clearance = SkillTreeGraphLayout.SubNodeOuterOffset + SkillTreeGraphLayout.SubNodeSize * 0.5f + 8f;
            var horizontal = Mathf.Abs(across.x) >= Mathf.Abs(across.y);
            // Beside a vertical branch a name has the whole side to itself; under a horizontal one
            // it shares the width of one tier with its neighbours, so it wraps into that width.
            var label = ProceduralUIBuilder.CreateText("KeyName_" + _spawned.Count, _root, horizontal ? 20 : 16, selected ? FontStyle.Bold : FontStyle.Normal,
                horizontal ? TextAnchor.MiddleLeft : TextAnchor.UpperCenter,
                selected ? SelectionRingColor : ProceduralUIBuilder.ResolveSecondaryTextColor());
            label.raycastTarget = false;
            label.text = displayName;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = horizontal ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 1f);
            rect.sizeDelta = horizontal ? new Vector2(230f, 28f) : new Vector2(SkillTreeGraphLayout.TierStep - 6f, 44f);
            rect.anchoredPosition = keyPosition + across * clearance;
            _spawned.Add(label.gameObject);
        }

        private static Sprite LoadCropped(string path)
        {
            var rect = path == KeyFramePath ? KeyFrameRect
                : path == SubFramePath ? SubFrameRect
                : path == LinkPath ? LinkRect
                : PortraitRect;
            return DebugSpriteFactory.LoadProjectSpriteRect(path, rect) ?? DebugSpriteFactory.LoadProjectSprite(path);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }

        private static Color ResolveStateColor(Color accent, SkillNodeState state, bool isLink)
        {
            switch (state)
            {
                case SkillNodeState.Purchased:
                    return isLink ? accent : new Color(accent.r, accent.g, accent.b, 1f);
                case SkillNodeState.Available:
                    return isLink ? new Color(accent.r, accent.g, accent.b, 0.6f) : Color.Lerp(accent, Color.white, 0.35f);
                case SkillNodeState.Unaffordable:
                    return new Color(0.62f, 0.62f, 0.66f, isLink ? 0.35f : 0.85f);
                default:
                    // Locked: dimmed and near-empty, distinct from unaffordable, which SkillTree.md
                    // requires explicitly.
                    return new Color(0.3f, 0.32f, 0.36f, isLink ? 0.25f : 0.55f);
            }
        }

        public void Clear()
        {
            _hitTargets.Clear();

            for (var index = 0; index < _spawned.Count; index++)
            {
                var spawned = _spawned[index];

                if (spawned == null)
                {
                    continue;
                }

                // Destroy is deferred to end of frame and ProceduralUIFactory reuses a child it
                // finds by name - so without renaming, deactivating and detaching first, a rebuild
                // in the same frame re-attaches its new nodes to the doomed old ones and the whole
                // graph vanishes (CLAUDE.md, engine gotchas).
                spawned.name += "_Discarded";
                spawned.SetActive(false);
                spawned.transform.SetParent(null, false);
                Destroy(spawned);
            }

            _spawned.Clear();
        }

        public void SetVisible(bool visible)
        {
            if (_root != null && _root.gameObject.activeSelf != visible)
            {
                _root.gameObject.SetActive(visible);
            }
        }
    }
}
