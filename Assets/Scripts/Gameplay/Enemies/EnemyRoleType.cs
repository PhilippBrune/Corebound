namespace Corebound.Gameplay.Enemies
{
    public enum EnemyRoleType
    {
        Chaser,
        RangedPressure,
        Bruiser,

        // Appended, never inserted: this enum is serialized by index in the
        // EnemyBehaviour_*.asset files, so an insert would silently repoint every existing
        // enemy to the wrong role. The two names come from Enemies.md's own list of example
        // roles - "Flächenkontrolle oder Support für andere Enemies" - and close the
        // Vision Section 9 target of five distinct enemy types.

        // Ignores the Player entirely, advances on the Core until it reaches its hold
        // distance, then anchors and fires full-circle projectile rings. Denies ground rather
        // than chasing anyone.
        AreaControl,

        // Never attacks. Steers toward the enemies it is reinforcing and multiplies their
        // move speed and damage while they stand in its aura, which makes it the correct
        // thing to shoot first.
        Support
    }
}