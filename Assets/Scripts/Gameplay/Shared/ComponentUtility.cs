using UnityEngine;

namespace Corebound
{
    // Open Topics 5, owner decision (a), 2026-09-30. `GetComponent<T>() ?? AddComponent<T>()` is
    // wrong in Unity: `??` checks for a C# null, but a missing or destroyed component can come
    // back as a Unity "fake null" - an object that `==` reports as null and `??` treats as
    // present. In the Editor that is what GetComponent returns for a missing native component,
    // which is how the stage crossfade threw on 2026-09-30 and aborted a whole stage change.
    //
    // In the root namespace so every Corebound.* file can call it without a using.
    public static class ComponentUtility
    {
        public static T GetOrAdd<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
