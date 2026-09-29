using System.Collections.Generic;
using Corebound.Gameplay.Shared;
using Corebound.SaveSystem;
using Corebound.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.MetaProgression.SkillTree
{
    // The node graph from SKILL_TREE_NODE_GRAPH_2026-09-28: radial branches fanning from a central
    // portrait (N2), positions authored per key node (N1), node state readable without colour alone
    // (N4).
    //
    // This draws the tree. It deliberately does **not** own selection or purchasing: the existing
    // MetaProgressionRouteTargetController still does both, and its list is still what the player
    // interacts with. Two reasons. First, purchases are the one thing in this screen that must not
    // regress, and the graph could not be interactively playtested before it shipped. Second, N3's
    // navigation (tabs coarse, stick fine, shoulders jump tiers) is a feel problem, and feel
    // problems should not be guessed at blind.
    //
    // So this is the visual half, complete and judgeable, with the interactive half left as the
    // reviewable next step. It also finally gives the four finished-but-unreferenced skill tree art
    // files something to attach to.
    public sealed class SkillTreeGraphPresenter : MonoBehaviour
    {
        private const string KeyFramePath = "Assets/Art/UI/SkillTree/skill_node_key_frame.png";
        private const string SubFramePath = "Assets/Art/UI/SkillTree/skill_node_sub_frame.png";
        private const string LinkPath = "Assets/Art/UI/SkillTree/skill_link_segment.png";
        private const string PortraitFramePath = "Assets/Art/UI/SkillTree/character_portrait_frame.png";

        // Sized to fit inside the Progression panel. The first version used roughly double these
        // and spilled the outer tiers over the menu behind the panel - the graph drew correctly and
        // simply did not fit.
        private const float BranchRadius = 78f;
        private const float TierStep = 44f;
        private const float SubNodeArcRadius = 30f;
        private const float KeyNodeSize = 40f;
        private const float SubNodeSize = 22f;
        private const float PortraitSize = 72f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private RectTransform _root;

        public void Build(RectTransform parent, SkillTreeDefinitionData tree, MetaProgressionSaveData save)
        {
            Clear();

            if (parent == null || tree == null || tree.Branches == null || tree.Branches.Length == 0)
            {
                return;
            }

            if (_root == null)
            {
                var rootObject = new GameObject("SkillTreeGraph", typeof(RectTransform));
                rootObject.transform.SetParent(parent, false);
                _root = rootObject.GetComponent<RectTransform>();
                ProceduralUIBuilder.StretchToParent(_root);

                // Behind the rows: the list still owns selection and purchasing, so it has to stay
                // readable. The graph is the backdrop until it takes over interaction.
                _root.SetAsFirstSibling();
            }

            BuildPortrait();

            var branches = tree.Branches;

            for (var branchIndex = 0; branchIndex < branches.Length; branchIndex++)
            {
                var branch = branches[branchIndex];

                if (branch == null || branch.KeyNodes == null)
                {
                    continue;
                }

                // Branch direction: authored positions win, and an unauthored branch falls back to
                // an even fan so the tree still reads instead of piling onto the portrait.
                var branchAngle = branches.Length > 0 ? 90f - branchIndex * (360f / branches.Length) : 90f;
                var previousPosition = Vector2.zero;

                for (var nodeIndex = 0; nodeIndex < branch.KeyNodes.Length; nodeIndex++)
                {
                    var keyNode = branch.KeyNodes[nodeIndex];

                    if (keyNode == null)
                    {
                        continue;
                    }

                    var position = ResolveKeyNodePosition(keyNode, branchAngle, nodeIndex);
                    var state = SkillTreeRules.ResolveKeyNodeState(save, tree, branch, keyNode);

                    // The link is drawn before the node so the node sits on top of it.
                    DrawLink(previousPosition, position, branch.AccentColor, state);
                    DrawNode(position, KeyNodeSize, KeyFramePath, branch.AccentColor, state,
                        keyNode.DisplayName, keyNode.Cost, 0, 0);
                    previousPosition = position;

                    DrawSubNodes(tree, save, branch, keyNode, position);
                }
            }
        }

        private void DrawSubNodes(
            SkillTreeDefinitionData tree,
            MetaProgressionSaveData save,
            SkillBranchData branch,
            SkillKeyNodeData keyNode,
            Vector2 keyPosition)
        {
            var subNodes = keyNode.SubNodes;

            if (subNodes == null || subNodes.Length == 0)
            {
                return;
            }

            // N1: sub-nodes are placed on a small arc around their key node automatically, so only
            // key nodes carry an authored position.
            var spread = Mathf.Min(150f, 44f * subNodes.Length);
            var baseAngle = Mathf.Atan2(keyPosition.y, keyPosition.x) * Mathf.Rad2Deg;

            for (var index = 0; index < subNodes.Length; index++)
            {
                var subNode = subNodes[index];

                if (subNode == null)
                {
                    continue;
                }

                var offset = subNodes.Length == 1
                    ? 0f
                    : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, index / (float)(subNodes.Length - 1));
                var angle = (baseAngle + offset) * Mathf.Deg2Rad;
                var position = keyPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SubNodeArcRadius;
                var state = SkillTreeRules.ResolveSubNodeState(save, tree, keyNode, subNode);
                var rank = save != null ? save.GetSkillNodeRank(save.SelectedCharacterId, subNode.NodeId) : 0;

                DrawLink(keyPosition, position, branch.AccentColor, state);
                DrawNode(position, SubNodeSize, SubFramePath, branch.AccentColor, state,
                    subNode.DisplayName, subNode.CostPerRank, rank, subNode.MaxRank);
            }
        }

        private Vector2 ResolveKeyNodePosition(SkillKeyNodeData keyNode, float branchAngleDegrees, int tier)
        {
            if (keyNode.GraphPosition.sqrMagnitude > 0.0001f)
            {
                return keyNode.GraphPosition;
            }

            var radians = branchAngleDegrees * Mathf.Deg2Rad;
            var radius = BranchRadius + tier * TierStep;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        private void BuildPortrait()
        {
            var sprite = DebugSpriteFactory.LoadProjectSprite(PortraitFramePath);
            var image = ProceduralUIBuilder.CreateImage("Portrait", _root, sprite, Color.white);
            image.raycastTarget = false;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
            _spawned.Add(image.gameObject);
        }

        private void DrawLink(Vector2 from, Vector2 to, Color accent, SkillNodeState state)
        {
            var delta = to - from;
            var length = delta.magnitude;

            if (length < 1f)
            {
                return;
            }

            var sprite = DebugSpriteFactory.LoadProjectSprite(LinkPath);
            // Unique per link: ProceduralUIFactory reuses a child it finds by name, so a shared
            // name makes every link collapse onto one object - the same reuse-by-name trap that
            // has bitten the reward and settings panels before.
            var image = ProceduralUIBuilder.CreateImage("Link_" + _spawned.Count, _root, sprite, ResolveStateColor(accent, state, true));
            image.raycastTarget = false;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(length, 8f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            _spawned.Add(image.gameObject);
        }

        private void DrawNode(
            Vector2 position,
            float size,
            string framePath,
            Color accent,
            SkillNodeState state,
            string displayName,
            int cost,
            int rank,
            int maxRank)
        {
            var sprite = DebugSpriteFactory.LoadProjectSprite(framePath);
            var image = ProceduralUIBuilder.CreateImage("Node_" + _spawned.Count, _root, sprite, ResolveStateColor(accent, state, false));
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
            // ones show their price, and locked ones show a lock - so the graph still parses in
            // greyscale.
            var caption = state == SkillNodeState.Purchased
                ? (maxRank > 0 ? rank + "/" + maxRank : "*")
                : (state == SkillNodeState.Available || state == SkillNodeState.Unaffordable
                    ? cost.ToString()
                    : "-");

            var label = ProceduralUIBuilder.CreateText("NodeLabel_" + _spawned.Count, rect, Mathf.RoundToInt(size * 0.32f),
                FontStyle.Bold, TextAnchor.MiddleCenter,
                state == SkillNodeState.Unaffordable ? new Color(1f, 0.55f, 0.45f, 1f) : Color.white);
            label.raycastTarget = false;
            ProceduralUIBuilder.StretchToParent(label.rectTransform);

            var nameLabel = ProceduralUIBuilder.CreateSecondaryText("NodeName_" + _spawned.Count, rect, 9, FontStyle.Normal, TextAnchor.UpperCenter);
            nameLabel.raycastTarget = false;
            nameLabel.text = displayName;
            var nameRect = nameLabel.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 0f);
            nameRect.anchorMax = new Vector2(0.5f, 0f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -2f);
            nameRect.sizeDelta = new Vector2(Mathf.Max(96f, size * 1.8f), 26f);

            label.text = caption;
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
                // graph vanishes. Exactly the trap CLAUDE.md documents, and exactly what happened
                // here before this guard: the graph built correctly and drew nothing.
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
