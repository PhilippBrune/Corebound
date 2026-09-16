using System;
using Corebound.Gameplay.Cards;
using Corebound.Gameplay.Resources;
using UnityEngine;

namespace Corebound.Gameplay.Economy
{
    // The centralized purchase-commit path Economy.md's Technical Design calls for. Shop and
    // Tower purchases previously each independently resolved a cost and called
    // ResourceWalletService.TrySpend directly, duplicating the same validate-then-spend logic
    // with no shared point to hook a future discount system, purchase log, or audio cue. Both now
    // route through here; the four documented events (OnPurchaseRequested, OnPurchaseSucceeded,
    // OnPurchaseFailed, OnPriceModified) fire regardless of which system made the purchase.
    //
    // Economy.md's "DiscountModifierData: standardisierte Preis-Modifikatoren aus Cards, Unlocks
    // oder Events" is implemented via the same pull-model multiplier pattern already used for
    // WeaponDamageMultiplier/IncomingDamageTakenMultiplier elsewhere: a CardEffectData with
    // Target=PurchaseCostMultiplier feeds CardService.PurchaseCostMultiplier, read live here.
    // Callers must resolve the final cost via ResolveCost() BEFORE both displaying it and passing
    // it to TryPurchase() - resolving twice would silently double-discount, so TryPurchase charges
    // exactly the amount it is given rather than resolving again itself.
    public sealed class EconomyService : MonoBehaviour
    {
        [SerializeField] private ResourceWalletService _resourceWalletService;
        [SerializeField] private CardService _cardService;

        public event Action<int> PurchaseRequested;

        public event Action<int> PurchaseSucceeded;

        public event Action<int> PurchaseFailed;

        public event Action<int, int> PriceModified;

        private void Awake()
        {
            if (_cardService == null)
            {
                _cardService = GetComponent<CardService>();
            }
        }

        public int ResolveCost(int baseCost)
        {
            var multiplier = _cardService != null ? _cardService.PurchaseCostMultiplier : 1f;
            var finalCost = Mathf.Max(0, Mathf.RoundToInt(baseCost * multiplier));

            if (finalCost != baseCost)
            {
                PriceModified?.Invoke(baseCost, finalCost);
            }

            return finalCost;
        }

        public bool TryPurchase(int cost)
        {
            PurchaseRequested?.Invoke(cost);

            if (_resourceWalletService == null || !_resourceWalletService.TrySpend(cost))
            {
                PurchaseFailed?.Invoke(cost);
                return false;
            }

            PurchaseSucceeded?.Invoke(cost);
            return true;
        }

        public void Refund(int amount)
        {
            _resourceWalletService?.AddReward(amount);
        }
    }
}
