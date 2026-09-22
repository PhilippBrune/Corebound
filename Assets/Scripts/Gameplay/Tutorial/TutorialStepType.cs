namespace Corebound.Gameplay.Tutorial
{
    // The six mandatory tutorial steps named explicitly in Tutorial.md's resolved design
    // decision: Movement, Weapon-Nutzung, Resource-Sammeln, Tower-Bau, Card-Auswahl,
    // Core-Verteidigung.
    public enum TutorialStepType
    {
        Movement,
        WeaponUse,
        ResourceCollect,
        TowerBuild,
        CardSelect,
        CoreDefense,

        // Appended 2026-09-22 (spec review B7): the tutorial predated the six-weapon loadout and
        // the Core repair. Appended, never inserted - the step assets serialize this by index.
        WeaponLoadout,
        CoreRepair
    }
}
