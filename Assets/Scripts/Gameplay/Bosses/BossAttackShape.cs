namespace Corebound.Gameplay.Bosses
{
    // The answer to "five bosses exist; how differently they play is uneven" (00_PROJECT_VISION.md
    // section 20.1). The honest diagnosis was worse than uneven: only two BossAttackPatternData
    // assets existed and all five bosses shared them, so every boss fired the same aimed fan and
    // differed only in numbers.
    //
    // This is one data field, not a subsystem. It changes how the existing projectile burst is
    // *arranged*, reusing the same pool, the same damage context and the same telegraph. That keeps
    // it inside the vision's rule that new content extends existing systems rather than introducing
    // mechanics, while giving each boss a dodge that feels different:
    //
    //   Fan     - the current behaviour. Dodge sideways, out of the cone.
    //   Ring    - 360 degrees, ignores aim. Dodge by finding a gap, or by being far away.
    //   Spiral  - a rotating offset per volley, sweeping the arena. Dodge by moving with the sweep.
    //   Volley  - parallel shots abreast, a wall rather than a cone. Dodge around an end.
    //   Lance   - every shot on one bearing. Dodge by never standing in line.
    //
    // Append-only: this enum is serialized by index in the BossAttackPattern assets.
    public enum BossAttackShape
    {
        Fan = 0,
        Ring = 1,
        Spiral = 2,
        Volley = 3,
        Lance = 4
    }
}
