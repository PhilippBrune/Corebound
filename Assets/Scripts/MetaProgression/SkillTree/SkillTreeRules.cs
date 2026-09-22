using System.Collections.Generic;
using Corebound.SaveSystem;
using UnityEngine;

namespace Corebound.MetaProgression
{
    public enum SkillNodeState
    {
        Purchased,          // key node bought, or sub node at max rank
        Available,          // can be bought right now
        Unaffordable,       // every requirement met except Skill Points
        LockedPrerequisite, // prerequisite key node not bought
        LockedBranchSpend,  // not enough SP spent in this branch yet
        LockedParent        // sub node whose key node is not bought
    }

    // Purchase validation, respec and snapshot building for SkillTree.md, as pure functions over
    // the save and the tree asset. Kept free of MonoBehaviour state so the main menu and the
    // gameplay scene resolve a tree identically, and so the rules can be tested in isolation.
    public static class SkillTreeRules
    {
        public static int ResolveBranchSpend(MetaProgressionSaveData save, SkillTreeDefinitionData tree, SkillBranchData branch)
        {
            if (save == null || tree == null || branch == null || branch.KeyNodes == null)
            {
                return 0;
            }

            var spent = 0;

            for (var keyIndex = 0; keyIndex < branch.KeyNodes.Length; keyIndex++)
            {
                var keyNode = branch.KeyNodes[keyIndex];

                if (keyNode == null)
                {
                    continue;
                }

                if (save.GetSkillNodeRank(tree.CharacterId, keyNode.NodeId) > 0)
                {
                    spent += keyNode.Cost;
                }

                if (keyNode.SubNodes == null)
                {
                    continue;
                }

                for (var subIndex = 0; subIndex < keyNode.SubNodes.Length; subIndex++)
                {
                    var subNode = keyNode.SubNodes[subIndex];

                    if (subNode != null)
                    {
                        spent += subNode.CostPerRank * save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId);
                    }
                }
            }

            return spent;
        }

        public static int ResolveTotalTreeSpend(MetaProgressionSaveData save, SkillTreeDefinitionData tree)
        {
            var total = 0;

            if (tree == null || tree.Branches == null)
            {
                return 0;
            }

            for (var index = 0; index < tree.Branches.Length; index++)
            {
                total += ResolveBranchSpend(save, tree, tree.Branches[index]);
            }

            return total;
        }

        public static SkillNodeState ResolveKeyNodeState(MetaProgressionSaveData save, SkillTreeDefinitionData tree, SkillBranchData branch, SkillKeyNodeData keyNode)
        {
            if (save.GetSkillNodeRank(tree.CharacterId, keyNode.NodeId) > 0)
            {
                return SkillNodeState.Purchased;
            }

            if (!string.IsNullOrWhiteSpace(keyNode.PrerequisiteNodeId) && save.GetSkillNodeRank(tree.CharacterId, keyNode.PrerequisiteNodeId) <= 0)
            {
                return SkillNodeState.LockedPrerequisite;
            }

            if (ResolveBranchSpend(save, tree, branch) < keyNode.BranchSpendRequirement)
            {
                return SkillNodeState.LockedBranchSpend;
            }

            return save.SkillPointsAvailable >= keyNode.Cost ? SkillNodeState.Available : SkillNodeState.Unaffordable;
        }

        public static SkillNodeState ResolveSubNodeState(MetaProgressionSaveData save, SkillTreeDefinitionData tree, SkillKeyNodeData parent, SkillSubNodeData subNode)
        {
            var rank = save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId);

            if (rank >= subNode.MaxRank)
            {
                return SkillNodeState.Purchased;
            }

            if (save.GetSkillNodeRank(tree.CharacterId, parent.NodeId) <= 0)
            {
                return SkillNodeState.LockedParent;
            }

            return save.SkillPointsAvailable >= subNode.CostPerRank ? SkillNodeState.Available : SkillNodeState.Unaffordable;
        }

        public static bool TryPurchaseKeyNode(MetaProgressionSaveData save, SkillTreeDefinitionData tree, SkillBranchData branch, SkillKeyNodeData keyNode)
        {
            if (ResolveKeyNodeState(save, tree, branch, keyNode) != SkillNodeState.Available || !save.TrySpendSkillPoints(keyNode.Cost))
            {
                return false;
            }

            save.SetSkillNodeRank(tree.CharacterId, keyNode.NodeId, 1);
            return true;
        }

        public static bool TryPurchaseSubNode(MetaProgressionSaveData save, SkillTreeDefinitionData tree, SkillKeyNodeData parent, SkillSubNodeData subNode)
        {
            if (ResolveSubNodeState(save, tree, parent, subNode) != SkillNodeState.Available || !save.TrySpendSkillPoints(subNode.CostPerRank))
            {
                return false;
            }

            save.SetSkillNodeRank(tree.CharacterId, subNode.NodeId, save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId) + 1);
            return true;
        }

        // Full respec only (SkillTree.md): refunds exactly what the tree cost, Ascension untouched.
        public static int Respec(MetaProgressionSaveData save, SkillTreeDefinitionData tree)
        {
            var refund = ResolveTotalTreeSpend(save, tree);
            save.ClearSkillNodes(tree.CharacterId, refund);
            return refund;
        }

        // A content update that changed the tree grants one free respec automatically.
        public static bool ApplyTreeVersion(MetaProgressionSaveData save, SkillTreeDefinitionData tree)
        {
            if (save == null || tree == null || save.SkillTreeVersion == tree.TreeVersion)
            {
                return false;
            }

            var hadNodes = ResolveTotalTreeSpend(save, tree) > 0;

            if (hadNodes && save.SkillTreeVersion != 0)
            {
                Respec(save, tree);
            }

            save.SetSkillTreeVersion(tree.TreeVersion);
            return true;
        }

        public static bool IsTreeComplete(MetaProgressionSaveData save, SkillTreeDefinitionData tree)
        {
            if (save == null || tree == null || tree.Branches == null)
            {
                return false;
            }

            for (var branchIndex = 0; branchIndex < tree.Branches.Length; branchIndex++)
            {
                var branch = tree.Branches[branchIndex];

                if (branch == null || branch.KeyNodes == null)
                {
                    continue;
                }

                for (var keyIndex = 0; keyIndex < branch.KeyNodes.Length; keyIndex++)
                {
                    var keyNode = branch.KeyNodes[keyIndex];

                    if (keyNode == null)
                    {
                        continue;
                    }

                    if (save.GetSkillNodeRank(tree.CharacterId, keyNode.NodeId) <= 0)
                    {
                        return false;
                    }

                    if (keyNode.SubNodes == null)
                    {
                        continue;
                    }

                    for (var subIndex = 0; subIndex < keyNode.SubNodes.Length; subIndex++)
                    {
                        var subNode = keyNode.SubNodes[subIndex];

                        if (subNode != null && save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId) < subNode.MaxRank)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // The offer is rolled once and persisted, and rerolled only when a rank is bought, so the
        // choice is real but not farmable by reopening the menu.
        public static IReadOnlyList<string> EnsureAscensionOffer(MetaProgressionSaveData save, SkillTreeDefinitionData tree)
        {
            if (save.PendingAscensionOffer.Count > 0 || tree.AscensionPool == null || tree.AscensionPool.Length == 0)
            {
                return save.PendingAscensionOffer;
            }

            var pool = new List<string>();

            for (var index = 0; index < tree.AscensionPool.Length; index++)
            {
                if (tree.AscensionPool[index] != null)
                {
                    pool.Add(tree.AscensionPool[index].BonusId);
                }
            }

            var offer = new List<string>();
            var random = new System.Random(save.AscensionRank * 7919 + save.SkillPointsEarned * 31 + 17);

            while (offer.Count < tree.AscensionOfferCount && pool.Count > 0)
            {
                var pick = random.Next(pool.Count);
                offer.Add(pool[pick]);
                pool.RemoveAt(pick);
            }

            save.SetPendingAscensionOffer(offer);
            return save.PendingAscensionOffer;
        }

        public static bool TryPurchaseAscension(MetaProgressionSaveData save, SkillTreeDefinitionData tree, int offerIndex)
        {
            if (!IsTreeComplete(save, tree))
            {
                return false;
            }

            var offer = EnsureAscensionOffer(save, tree);

            if (offerIndex < 0 || offerIndex >= offer.Count)
            {
                return false;
            }

            var cost = tree.AscensionCostForRank(save.AscensionRank);

            if (!save.TrySpendSkillPoints(cost))
            {
                return false;
            }

            save.AddAscensionRank(offer[offerIndex]);
            return true;
        }

        public static SkillModifierSnapshot BuildSnapshot(MetaProgressionSaveData save, SkillTreeDefinitionData tree)
        {
            var snapshot = new SkillModifierSnapshot();

            if (save == null || tree == null)
            {
                return snapshot;
            }

            if (tree.Branches != null)
            {
                for (var branchIndex = 0; branchIndex < tree.Branches.Length; branchIndex++)
                {
                    var branch = tree.Branches[branchIndex];

                    if (branch == null || branch.KeyNodes == null)
                    {
                        continue;
                    }

                    for (var keyIndex = 0; keyIndex < branch.KeyNodes.Length; keyIndex++)
                    {
                        var keyNode = branch.KeyNodes[keyIndex];

                        if (keyNode == null || save.GetSkillNodeRank(tree.CharacterId, keyNode.NodeId) <= 0)
                        {
                            continue;
                        }

                        ApplyEffects(snapshot, keyNode.Effects, 1);

                        if (keyNode.SubNodes == null)
                        {
                            continue;
                        }

                        for (var subIndex = 0; subIndex < keyNode.SubNodes.Length; subIndex++)
                        {
                            var subNode = keyNode.SubNodes[subIndex];

                            if (subNode != null)
                            {
                                ApplyEffects(snapshot, subNode.Effects, Mathf.Min(subNode.MaxRank, save.GetSkillNodeRank(tree.CharacterId, subNode.NodeId)));
                            }
                        }
                    }
                }
            }

            // Ascension choices are account-wide numbers; they are stored as bonus ids in the order
            // chosen, so each entry is one rank of that bonus.
            for (var index = 0; index < save.AscensionChoices.Count; index++)
            {
                var bonus = tree.FindAscensionBonus(save.AscensionChoices[index]);

                if (bonus != null)
                {
                    snapshot.Apply(bonus.Effect, 1);
                }
            }

            return snapshot;
        }

        private static void ApplyEffects(SkillModifierSnapshot snapshot, SkillEffectData[] effects, int rank)
        {
            if (effects == null || rank <= 0)
            {
                return;
            }

            for (var index = 0; index < effects.Length; index++)
            {
                snapshot.Apply(effects[index], rank);
            }
        }
    }
}
