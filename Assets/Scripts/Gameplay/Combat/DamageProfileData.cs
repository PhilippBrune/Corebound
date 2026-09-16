using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Combat/Damage Profile", fileName = "DamageProfile")]
    public sealed class DamageProfileData : ScriptableObject
    {
        [SerializeField] private DamageKind _damageKind = DamageKind.Direct;
        [SerializeField] private float _baseDamage = 1f;
        [SerializeField] private StatusEffectType _statusEffectType = StatusEffectType.Burn;
        [SerializeField] private bool _applyStatusEffect;

        public DamageKind DamageKind => _damageKind;

        public float BaseDamage => _baseDamage;

        public bool ApplyStatusEffect => _applyStatusEffect;

        public StatusEffectType StatusEffectType => _statusEffectType;
    }
}