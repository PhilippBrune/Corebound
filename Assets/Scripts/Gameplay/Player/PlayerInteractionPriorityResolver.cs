namespace Corebound.Gameplay.Player
{
    public static class PlayerInteractionPriorityResolver
    {
        public static int GetPriority(PlayerInteractionType interactionType)
        {
            switch (interactionType)
            {
                case PlayerInteractionType.BlockingUi:
                    return 0;
                case PlayerInteractionType.TowerSlot:
                    return 1;
                case PlayerInteractionType.Reward:
                    return 2;
                case PlayerInteractionType.Shop:
                    return 3;
                case PlayerInteractionType.Pickup:
                    return 4;
                default:
                    return int.MaxValue;
            }
        }
    }
}