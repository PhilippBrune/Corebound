using UnityEngine;

namespace Corebound.SaveSystem
{
    // Every gameplay/UI service that takes an optional save-manager reference (typed as
    // UnityEngine.Object in the inspector so either a scene component or an interface-only
    // Object reference can be assigned) needs the same "use it if wired, else fall back to the
    // DontDestroyOnLoad singleton" resolution. Centralized here so that fallback can't be
    // accidentally omitted in a new call site (it previously was, silently, in a couple of
    // services).
    public static class SaveManagerResolver
    {
        public static ISaveManager Resolve(Object saveManagerSource)
        {
            return saveManagerSource as ISaveManager ?? LocalProfileSaveManager.Instance;
        }
    }
}
