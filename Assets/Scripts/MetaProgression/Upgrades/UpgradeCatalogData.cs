using System;
using Corebound.UI;
using UnityEngine;

namespace Corebound.MetaProgression
{
    // Upgrades.md. Serialized by index in UpgradeCatalogData and in the save
    // (MetaProgressionSaveData.UpgradeRank.Stat), so it is append-only.
    public enum UpgradableStat
    {
        Damage = 0,
        FireSpeed = 1,
        Magazine = 2,
        ReloadSpeed = 3,
        Accuracy = 4,
        CriticalChance = 5,
        CriticalDamage = 6,
        Range = 7,
        BuildDiscount = 8
    }

    public enum UpgradeTargetKind
    {
        Weapon = 0,
        Tower = 1
    }

    // One permanent, per-item stat track. Authored inline in UpgradeCatalogData rather than as
    // separate assets: eleven near-identical three-number assets are harder to review side by side
    // than one list, and nothing references a track except the catalog.
    [Serializable]
    public sealed class UpgradeTrackData
    {
        [SerializeField] private string _trackId = "weapon_damage";
        [SerializeField] private string _displayName = "Damage";
        [SerializeField] private string _displayNameDe = "Schaden";
        [SerializeField] private UpgradableStat _stat = UpgradableStat.Damage;
        [SerializeField] private UpgradeTargetKind _appliesTo = UpgradeTargetKind.Weapon;

        // Signed and per rank: +0.06 is +6% damage, -0.04 is 4% shorter fire interval, +1 is one
        // more round in the magazine. UpgradeSnapshot documents how each stat applies it.
        [SerializeField] private float _valuePerRank = 0.06f;
        [SerializeField] private int _maxRank = 10;
        [SerializeField] private int _baseCost = 5;

        public string TrackId => _trackId;

        public string DisplayName => LocalizationService.Resolve(_displayName, _displayNameDe);

        public UpgradableStat Stat => _stat;

        public UpgradeTargetKind AppliesTo => _appliesTo;

        public float ValuePerRank => _valuePerRank;

        public int MaxRank => Mathf.Max(1, _maxRank);

        public int BaseCost => Mathf.Max(1, _baseCost);

        // Upgrades.md cost curve: rank n costs BaseCost * n.
        public int CostForRank(int rank)
        {
            return BaseCost * Mathf.Max(1, rank);
        }

        public float TotalValueAtRank(int rank)
        {
            return _valuePerRank * Mathf.Clamp(rank, 0, MaxRank);
        }
    }

    [CreateAssetMenu(menuName = "Corebound/Meta Progression/Upgrade Catalog Data", fileName = "UpgradeCatalogData")]
    public sealed class UpgradeCatalogData : ScriptableObject
    {
        [SerializeField] private UpgradeTrackData[] _tracks = Array.Empty<UpgradeTrackData>();

        public UpgradeTrackData[] Tracks => _tracks;

        public UpgradeTrackData FindTrack(UpgradeTargetKind kind, UpgradableStat stat)
        {
            if (_tracks == null)
            {
                return null;
            }

            for (var index = 0; index < _tracks.Length; index++)
            {
                var track = _tracks[index];

                if (track != null && track.AppliesTo == kind && track.Stat == stat)
                {
                    return track;
                }
            }

            return null;
        }
    }
}
