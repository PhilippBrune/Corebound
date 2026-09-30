using System.Globalization;
using System.Text;
using Corebound.Gameplay.Weapons;
using Corebound.UI;
using UnityEngine;

namespace Corebound.Gameplay.Cards
{
    // Docs/AI/Reward_Screen.md: the reward footer is "Explanation of the Card with % values". Card
    // descriptions are authored prose ("a solid Tower damage increase"), so the numbers are built
    // from the effect data itself - which also means they can never disagree with what the card
    // actually does.
    public static class CardEffectDescriber
    {
        private const string Separator = "  ·  ";

        public static string Describe(CardData card)
        {
            if (card == null)
            {
                return string.Empty;
            }

            if (card.IsWeaponCard)
            {
                return DescribeWeapon(card.GrantedWeapon);
            }

            var builder = new StringBuilder(96);
            var effects = card.Effects;

            if (effects != null)
            {
                for (var index = 0; index < effects.Length; index++)
                {
                    if (effects[index] != null)
                    {
                        Append(builder, DescribeCardEffect(effects[index]));
                    }
                }
            }

            var modifiers = card.WeaponModifiers;

            if (modifiers != null)
            {
                for (var index = 0; index < modifiers.Length; index++)
                {
                    if (modifiers[index] != null)
                    {
                        Append(builder, DescribeWeaponModifier(modifiers[index]));
                    }
                }
            }

            return builder.ToString();
        }

        public static string DescribeWeapon(WeaponData weapon)
        {
            if (weapon == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(96);
            var damage = weapon.DamageProfile != null ? weapon.DamageProfile.BaseDamage : 1f;
            var pattern = weapon.AttackPattern;
            Append(builder, LocalizationService.Resolve("Damage ", "Schaden ") + Number(damage));

            if (pattern != null)
            {
                Append(builder, (pattern.ProjectileCount > 1 ? pattern.ProjectileCount + "x " : string.Empty)
                    + LocalizationService.Resolve("every ", "alle ") + Number(pattern.FireIntervalSeconds) + " s");

                if (pattern.MagazineSize > 0)
                {
                    Append(builder, pattern.MagazineSize + LocalizationService.Resolve(" rounds, ", " Schuss, ") + Number(pattern.ReloadSeconds) + LocalizationService.Resolve(" s reload", " s Nachladen"));
                }
            }

            if (weapon.Projectile != null && weapon.Projectile.PierceCount > 0)
            {
                Append(builder, LocalizationService.Resolve("pierces ", "durchdringt ") + weapon.Projectile.PierceCount);
            }

            if (weapon.DamageProfile != null && weapon.DamageProfile.ApplyStatusEffect)
            {
                Append(builder, weapon.DamageProfile.StatusEffectType == Combat.StatusEffectType.Freeze
                    ? LocalizationService.Resolve("freezes", "friert ein")
                    : LocalizationService.Resolve("burns", "verbrennt"));
            }

            var innate = weapon.InnateEffects;

            if (innate != null)
            {
                for (var index = 0; index < innate.Length; index++)
                {
                    Append(builder, DescribeWeaponTarget(innate[index].Target, innate[index].Value));
                }
            }

            return builder.ToString();
        }

        private static string DescribeCardEffect(CardEffectData effect)
        {
            // The NEW_CARD_MECHANICS targets are mechanics, not stat multipliers, so "+0.5 HealPerKill"
            // would be the honest-but-useless fallback; each gets a sentence with its real numbers.
            var mechanic = DescribeMechanic(effect.Target, effect.Value);

            if (mechanic != null)
            {
                return mechanic;
            }

            var label = CardTargetLabel(effect.Target);

            if (effect.Operation == CardEffectOperation.Add)
            {
                return Signed(effect.Value) + " " + label;
            }

            return Percent(effect.Value) + " " + label;
        }

        private static string DescribeMechanic(CardEffectTarget target, float value)
        {
            switch (target)
            {
                case CardEffectTarget.PoisonSpreadRadius:
                    return LocalizationService.Resolve("Poison spreads on death (", "Gift springt beim Tod über (") + Number(value) + LocalizationService.Resolve(" units)", " Einheiten)");
                case CardEffectTarget.HealOnWaveClear:
                    return Signed(value * 100f) + LocalizationService.Resolve("% health per wave clear", " % Leben pro geschaffter Welle");
                case CardEffectTarget.HealPerKill:
                    return Signed(value) + LocalizationService.Resolve(" health per kill (", " Leben pro Kill (") + Combat.PlayerSupportRules.HealPerKillCapPerWave + LocalizationService.Resolve(" per wave)", " pro Welle)");
                case CardEffectTarget.ShieldStacks:
                    return LocalizationService.Resolve("shield blocks one hit (", "Schild blockt einen Treffer (") + Number(Combat.PlayerSupportRules.ShieldRechargeSeconds) + LocalizationService.Resolve(" s recharge)", " s Aufladung)");
                case CardEffectTarget.TowerAuraFireRate:
                    return Signed(value * 100f) + LocalizationService.Resolve("% fire rate for towers within ", " % Feuerrate für Türme im Umkreis von ") + Number(Combat.PlayerSupportRules.TowerAuraRadius);
                case CardEffectTarget.CoreIntermissionRegen:
                    return LocalizationService.Resolve("Core regenerates ", "Kern regeneriert ") + Number(value * 100f) + LocalizationService.Resolve("%/s in an intermission", " %/s in der Zwischenphase");
                case CardEffectTarget.CoreDamageTakenMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" Core damage taken", " erlittener Kernschaden");
                case CardEffectTarget.SentryDrones:
                    return Signed(value) + LocalizationService.Resolve(" sentry drone (", " Wächterdrohne (") + Number(Combat.PlayerSupportRules.DroneDamageFraction * 100f) + LocalizationService.Resolve("% of primary damage)", " % des Primärschadens)");
                default:
                    return null;
            }
        }

        private static string DescribeWeaponModifier(WeaponModifierData modifier)
        {
            var text = DescribeWeaponTarget(modifier.Target, modifier.Value);

            if (!string.IsNullOrWhiteSpace(modifier.RequiredWeaponId))
            {
                text += " (" + modifier.RequiredWeaponId.Replace("weapon_", string.Empty) + LocalizationService.Resolve(" only)", " nur)");
            }

            return text;
        }

        public static string DescribeWeaponTarget(WeaponModifierTarget target, float value)
        {
            switch (target)
            {
                case WeaponModifierTarget.DamageMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" weapon damage", " Waffenschaden");
                case WeaponModifierTarget.FireIntervalMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" fire interval", " Feuerintervall");
                case WeaponModifierTarget.ProjectileSpeedMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" projectile speed", " Projektiltempo");
                case WeaponModifierTarget.CriticalChance:
                    return Signed(value * 100f) + LocalizationService.Resolve("% crit chance", " % Kritchance");
                case WeaponModifierTarget.CriticalDamageMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" crit damage", " Kritschaden");
                case WeaponModifierTarget.AdditionalProjectiles:
                    return Signed(value) + LocalizationService.Resolve(" projectiles", " Projektile");
                case WeaponModifierTarget.StatusEffectChance:
                    return Signed(value * 100f) + LocalizationService.Resolve("% status chance", " % Statuschance");
                case WeaponModifierTarget.AdditionalPierce:
                    return Signed(value) + LocalizationService.Resolve(" pierce", " Durchschlag");
                case WeaponModifierTarget.CriticalEveryNthShot:
                    return LocalizationService.Resolve("every ", "jeder ") + Mathf.RoundToInt(value) + LocalizationService.Resolve("th shot crits", ". Schuss kritisch");
                case WeaponModifierTarget.FullRing:
                    return LocalizationService.Resolve("fires a full ring", "feuert einen vollen Ring");
                case WeaponModifierTarget.ExplodeOnExpire:
                    return LocalizationService.Resolve("misses explode", "Fehlschüsse explodieren");
                case WeaponModifierTarget.ExplodeOnImpact:
                    return LocalizationService.Resolve("hits burst for splash", "Treffer zerplatzen mit Flächenschaden");
                case WeaponModifierTarget.SlowOnHit:
                    return LocalizationService.Resolve("slows on hit", "verlangsamt bei Treffer");
                case WeaponModifierTarget.HeldDamageRamp:
                    return Signed(value * 100f) + LocalizationService.Resolve("% damage per second held", " % Schaden pro Sekunde Dauerfeuer");
                case WeaponModifierTarget.SpreadAngleMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" spread", " Streuung");
                case WeaponModifierTarget.MagazineBonus:
                    return Signed(value) + LocalizationService.Resolve(" magazine", " Magazin");
                case WeaponModifierTarget.ReloadTimeMultiplier:
                    return Percent(value) + LocalizationService.Resolve(" reload time", " Nachladezeit");
                case WeaponModifierTarget.PoisonChance:
                    return Signed(value * 100f) + LocalizationService.Resolve("% poison chance", " % Giftchance");
                case WeaponModifierTarget.ShockChance:
                    return Signed(value * 100f) + LocalizationService.Resolve("% chance to arc to ", " % Chance, auf ")
                        + Combat.RunCombatEffects.ShockTargets + LocalizationService.Resolve(" enemies", " Gegner überzuspringen");
                case WeaponModifierTarget.VulnerableOnHit:
                    return LocalizationService.Resolve("hits mark: +", "Treffer markieren: +") + Number(Combat.RunCombatEffects.VulnerableBonusPerCopy * 100f)
                        + LocalizationService.Resolve("% damage taken for ", " % erlittener Schaden für ") + Number(Combat.RunCombatEffects.VulnerableSeconds) + " s";
                default:
                    return string.Empty;
            }
        }

        private static string CardTargetLabel(CardEffectTarget target)
        {
            switch (target)
            {
                case CardEffectTarget.WeaponDamageMultiplier:
                    return LocalizationService.Resolve("weapon damage", "Waffenschaden");
                case CardEffectTarget.WeaponFireIntervalMultiplier:
                    return LocalizationService.Resolve("fire interval", "Feuerintervall");
                case CardEffectTarget.ResourceGainMultiplier:
                    return LocalizationService.Resolve("Scrap gained", "erhaltener Schrott");
                case CardEffectTarget.TowerDamageMultiplier:
                    return LocalizationService.Resolve("tower damage", "Turmschaden");
                case CardEffectTarget.IncomingDamageTakenMultiplier:
                    return LocalizationService.Resolve("damage taken", "erlittener Schaden");
                case CardEffectTarget.ProjectileSpeedMultiplier:
                    return LocalizationService.Resolve("projectile speed", "Projektiltempo");
                case CardEffectTarget.PlayerMovementSpeedMultiplier:
                    return LocalizationService.Resolve("move speed", "Bewegungstempo");
                case CardEffectTarget.PurchaseCostMultiplier:
                    return LocalizationService.Resolve("prices", "Preise");
                default:
                    return target.ToString();
            }
        }

        // x1.15 -> "+15%", x0.85 -> "-15%". Whether lower is better (interval, damage taken, prices)
        // is left to the label; the sign is the literal change to the number.
        private static string Percent(float multiplier)
        {
            return Signed((multiplier - 1f) * 100f) + "%";
        }

        private static string Signed(float value)
        {
            var rounded = Mathf.Round(value * 10f) / 10f;
            return (rounded >= 0f ? "+" : "-") + Mathf.Abs(rounded).ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string Number(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder builder, string part)
        {
            if (string.IsNullOrEmpty(part))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(Separator);
            }

            builder.Append(part);
        }
    }
}
