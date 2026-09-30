using Corebound.MetaProgression;
using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // The active class's signature, as numbers the rest of combat can read without a reference to
    // the player (CHARACTER_CLASSES_2026-09-30, K2-1: strong signatures). Configured once when the
    // run's player spawns, from the class and its skill tree, and again whenever the cards change,
    // because each class has cards that push its signature further.
    //
    // "Signature power" is the one knob every class shares: the tree's and cards' generic class
    // nodes raise it, and each class scales its headline number by it.
    public static class CharacterEffects
    {
        // Vanguard: every active Set Bonus tier also adds this much weapon damage.
        public const float VanguardDamagePerTier = 0.03f;

        // Engineer
        public const float EngineerAuraRate = 0.3f;
        public const float EngineerAuraRadius = 3f;
        public const float OverchargeChannelSeconds = 2f;
        public const float OverchargeSeconds = 5f;
        public const float OverchargeDamageMultiplier = 2f;
        public const float OverchargeReach = 1.8f;

        // Salvager
        public const float SalvagerScrapMultiplier = 1.5f;
        public const float SalvagerMagnetRange = 4f;
        public const int HoardThresholdBase = 150;
        public const float HoardDamagePerTenScrap = 0.01f;

        // Custodian
        public const float CustodianTetherRadius = 5f;
        public const float CustodianCoreReduction = 0.3f;
        public const float MaxCoreReduction = 0.6f;
        public const float ShockwaveRadius = 4f;
        public const float ShockwaveCooldownSeconds = 8f;
        public const float ShockwaveDamage = 2f;
        public const float ShockwaveImpulse = 6f;

        // Catalyst
        public const float CatalystDurationBonus = 0.5f;
        public const float CatalystContagionRadius = 2f;
        public const float CatalystPoisonChance = 0.1f;

        private static float _skillPower;
        private static int _skillCharges;
        private static float _skillHoardReduction;
        private static float _skillTether;
        private static float _skillContagion;
        private static int _skillFocus;

        private static float _cardPower;
        private static int _cardCharges;
        private static float _cardHoardReduction;
        private static float _cardTether;
        private static float _cardContagion;
        private static int _cardFocus;

        public static CharacterSignature Signature { get; private set; } = CharacterSignature.Vanguard;

        public static CharacterData Character { get; private set; }

        // Bumped on every change, so the loadout knows to re-resolve its Set Bonus tiers.
        public static int Version { get; private set; }

        public static float SignaturePower => 1f + _skillPower + _cardPower;

        private static bool Is(CharacterSignature signature) => Signature == signature;

        // ---- Vanguard ----------------------------------------------------------------------------
        public static int SetBonusCopyDiscount => Is(CharacterSignature.Vanguard) ? 1 : 0;

        // Full Kit: the archetype carried most counts this many extra copies on top.
        public static int SetBonusFocusBonus => Is(CharacterSignature.Vanguard) ? _skillFocus + _cardFocus : 0;

        public static float DamagePerActiveSetBonusTier => Is(CharacterSignature.Vanguard) ? VanguardDamagePerTier * SignaturePower : 0f;

        // ---- Engineer ----------------------------------------------------------------------------
        public static float TowerAuraRate => Is(CharacterSignature.Engineer) ? EngineerAuraRate * SignaturePower : 0f;

        public static int OverchargeCharges => Is(CharacterSignature.Engineer) ? 1 + _skillCharges + _cardCharges : 0;

        // ---- Salvager ----------------------------------------------------------------------------
        public static float ScrapDropMultiplier => Is(CharacterSignature.Salvager) ? SalvagerScrapMultiplier : 1f;

        public static float MagnetRangeBonus => Is(CharacterSignature.Salvager) ? SalvagerMagnetRange : 0f;

        public static int HoardThreshold => Mathf.Max(0, HoardThresholdBase - Mathf.RoundToInt(_skillHoardReduction + _cardHoardReduction));

        public static float HoardDamagePerTen => Is(CharacterSignature.Salvager) ? HoardDamagePerTenScrap * SignaturePower : 0f;

        public static int MinimumChainTier => Is(CharacterSignature.Salvager) ? 1 : 0;

        // Set at each wave start from the Scrap held then; 1 outside a Salvager run.
        public static float HoardDamageMultiplier { get; set; } = 1f;

        // ---- Custodian ---------------------------------------------------------------------------
        public static float TetherRadius => Is(CharacterSignature.Custodian) ? CustodianTetherRadius + _skillTether + _cardTether : 0f;

        public static float TetherCoreReduction => Is(CharacterSignature.Custodian) ? Mathf.Min(MaxCoreReduction, CustodianCoreReduction * SignaturePower) : 0f;

        public static bool IsTethered { get; set; }

        public static bool FreeRepairUsed { get; set; }

        public static bool HasFreeRepair => Is(CharacterSignature.Custodian) && !FreeRepairUsed;

        // ---- Catalyst ----------------------------------------------------------------------------
        public static float StatusDurationMultiplier => Is(CharacterSignature.Catalyst) ? 1f + CatalystDurationBonus * SignaturePower : 1f;

        public static float ContagionRadius => Is(CharacterSignature.Catalyst) ? CatalystContagionRadius + _skillContagion + _cardContagion : 0f;

        public static float ClassPoisonChance => Is(CharacterSignature.Catalyst) ? CatalystPoisonChance : 0f;

        // ---- Configuration -----------------------------------------------------------------------

        public static void ConfigureRun(CharacterData character, SkillModifierSnapshot skills)
        {
            Character = character;
            Signature = character != null ? character.Signature : CharacterSignature.Vanguard;
            _skillPower = skills != null ? skills[SkillEffectTarget.ClassSignaturePower] : 0f;
            _skillCharges = skills != null ? Mathf.RoundToInt(skills[SkillEffectTarget.ClassOverchargeCharges]) : 0;
            _skillHoardReduction = skills != null ? skills[SkillEffectTarget.ClassHoardThresholdReduction] : 0f;
            _skillTether = skills != null ? skills[SkillEffectTarget.ClassTetherRadius] : 0f;
            _skillContagion = skills != null ? skills[SkillEffectTarget.ClassContagionRadius] : 0f;
            _skillFocus = skills != null ? Mathf.RoundToInt(skills[SkillEffectTarget.ClassSetBonusFocus]) : 0;
            HoardDamageMultiplier = 1f;
            IsTethered = false;
            FreeRepairUsed = false;
            Version++;
        }

        public static void SetCardBonuses(float power, int charges, float hoardReduction, float tether, float contagion, int focus)
        {
            if (Mathf.Approximately(power, _cardPower) && charges == _cardCharges && Mathf.Approximately(hoardReduction, _cardHoardReduction)
                && Mathf.Approximately(tether, _cardTether) && Mathf.Approximately(contagion, _cardContagion) && focus == _cardFocus)
            {
                return;
            }

            _cardPower = power;
            _cardCharges = charges;
            _cardHoardReduction = hoardReduction;
            _cardTether = tether;
            _cardContagion = contagion;
            _cardFocus = focus;
            Version++;
        }
    }
}
