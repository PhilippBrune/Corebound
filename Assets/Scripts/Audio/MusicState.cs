namespace Corebound.Audio
{
    // The three states Audio.md's Data Model calls for a MusicStateData catalog to cover:
    // a calm intermission/menu bed, standard combat, and a distinct boss-encounter state.
    public enum MusicState
    {
        Calm,
        Combat,
        Boss,
    }
}
