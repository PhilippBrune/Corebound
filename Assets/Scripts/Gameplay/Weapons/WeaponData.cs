using System;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Combat;
using UnityEngine;

namespace Corebound.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Weapons/Weapon Data", fileName = "WeaponData")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string _weaponId = "weapon_default";
        [SerializeField] private string _displayName = "Weapon";
        [SerializeField] [TextArea(2, 3)] private string _summary = string.Empty;
        [SerializeField] private WeaponAttackPatternData _attackPattern;
        [SerializeField] private DamageProfileData _damageProfile;
        [SerializeField] private ProjectileData _projectile;
        [SerializeField] private WeaponAimAssistProfile _aimAssistProfile = WeaponAimAssistProfile.Medium;

        // Optional. When unset the weapon still fires; it just has no distinct fire/crit cue and
        // no icon, which is exactly how every weapon behaved before WeaponPresentationData existed.
        [SerializeField] private WeaponPresentationData _presentation;

        // WeaponLoadout.md: shop pricing, the sell value's basis, and the Set Bonus ladder that
        // gives this archetype its own identity when duplicated. Authored per weapon; never
        // defined centrally in code.
        [SerializeField] private CardRarity _rarity = CardRarity.Common;
        [SerializeField] private int _basePurchaseCost = 14;
        [SerializeField] private WeaponSetBonusData[] _setBonuses = Array.Empty<WeaponSetBonusData>();

        public string WeaponId => _weaponId;

        public string DisplayName => _displayName;

        public string Summary => _summary;

        public WeaponAttackPatternData AttackPattern => _attackPattern;

        public DamageProfileData DamageProfile => _damageProfile;

        public ProjectileData Projectile => _projectile;

        public WeaponAimAssistProfile AimAssistProfile => _aimAssistProfile;

        public WeaponPresentationData Presentation => _presentation;

        public CardRarity Rarity => _rarity;

        public int BasePurchaseCost => Mathf.Max(1, _basePurchaseCost);

        public WeaponSetBonusData[] SetBonuses => _setBonuses;
    }
}