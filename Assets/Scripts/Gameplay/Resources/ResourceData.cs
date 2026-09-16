using UnityEngine;

namespace Corebound.Gameplay.Resources
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Resources/Resource Data", fileName = "ResourceData")]
    public sealed class ResourceData : ScriptableObject
    {
        [SerializeField] private string _resourceId = "resource_primary";
        [SerializeField] private string _displayName = "Resource";
        [SerializeField] private Color _displayColor = new Color(1f, 0.83f, 0.34f, 1f);
        [SerializeField] private float _autoPickupRange = 1.8f;
        [SerializeField] private float _pickupMoveSpeed = 4.5f;

        public string ResourceId => _resourceId;

        public string DisplayName => _displayName;

        public Color DisplayColor => _displayColor;

        // Short auto-pickup range during Combat, per the resolved Resources.md decision; a full
        // wave-end vacuum (see ResourcePickupRuntime.ForceCollect) is the complementary path for
        // pickups the player did not walk over.
        public float AutoPickupRange => Mathf.Max(0f, _autoPickupRange);

        public float PickupMoveSpeed => Mathf.Max(0.1f, _pickupMoveSpeed);
    }
}