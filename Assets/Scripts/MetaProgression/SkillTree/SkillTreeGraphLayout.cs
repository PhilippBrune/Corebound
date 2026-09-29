using UnityEngine;

namespace Corebound.MetaProgression.SkillTree
{
    // Where every node of the radial skill graph sits, in panel pixels around the portrait.
    //
    // Shared by the presenter, which draws the nodes there, and the route controller, which uses
    // the same positions for stick navigation (N3: "the stick moves within the branch"). Two
    // copies of this arithmetic would drift, and the stick would then move to a node that is not
    // where the eye expects it.
    //
    // Each tier is a key node with its sub-nodes in a row across the branch axis, two per side.
    // The first graph put the sub-nodes on an arc ahead of the key node, which is where the next
    // tier's key node also sits - at a playable size they overlapped. A row across the axis only
    // needs one node's height between tiers.
    public static class SkillTreeGraphLayout
    {
        // Sized for reading, not for fitting the whole tree on screen: the presenter pans to the
        // active branch and clips the rest (playtest 2026-09-29 - at whole-tree scale the
        // sub-nodes were dots and their rank captions unreadable).
        public const float BranchRadius = 180f;
        public const float TierStep = 100f;
        public const float SubNodeInnerOffset = 64f;
        public const float SubNodeOuterOffset = 116f;
        public const float KeyNodeSize = 72f;
        public const float SubNodeSize = 46f;
        public const float PortraitSize = 150f;

        // Middle of a branch's key-node chain, for centring the view on it.
        public static Vector2 BranchCentre(int branchIndex, int branchCount, int keyNodeCount)
        {
            return BranchAxis(branchIndex, branchCount) * (BranchRadius + Mathf.Max(0, keyNodeCount - 1) * TierStep * 0.5f);
        }

        public static Vector2 BranchAxis(int branchIndex, int branchCount)
        {
            var degrees = branchCount > 0 ? 90f - branchIndex * (360f / branchCount) : 90f;
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        public static Vector2 KeyNode(SkillKeyNodeData keyNode, int branchIndex, int branchCount, int tier)
        {
            // N1: an authored position wins; the computed one is the fallback until one is set.
            if (keyNode != null && keyNode.GraphPosition.sqrMagnitude > 0.0001f)
            {
                return keyNode.GraphPosition;
            }

            return BranchAxis(branchIndex, branchCount) * (BranchRadius + tier * TierStep);
        }

        // Sub-nodes 0 and 1 go to one side (inner, then outer), 2 and 3 to the other. A fifth or
        // later sub-node continues outwards on alternating sides.
        public static Vector2 SubNode(Vector2 keyPosition, int subIndex)
        {
            var axis = keyPosition.sqrMagnitude > 0.0001f ? keyPosition.normalized : Vector2.up;
            var across = new Vector2(-axis.y, axis.x);
            var side = subIndex % 4 < 2 ? 1f : -1f;
            var step = subIndex % 2 == 0 ? SubNodeInnerOffset : SubNodeOuterOffset;
            step += (subIndex / 4) * (SubNodeOuterOffset - SubNodeInnerOffset) * 2f;
            return keyPosition + across * (side * step);
        }

        public static Vector2 SubNode(SkillKeyNodeData keyNode, int branchIndex, int branchCount, int tier, int subIndex)
        {
            return SubNode(KeyNode(keyNode, branchIndex, branchCount, tier), subIndex);
        }
    }
}
