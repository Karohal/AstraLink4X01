using UnityEngine;

namespace Game.Economy
{
    [CreateAssetMenu(menuName = "AstraLink/Economy/Resource Definition", fileName = "ResourceDefinition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isRaw = true;

        public string Id => _id;
        public string DisplayName => _displayName;
        public bool IsRaw => _isRaw;
    }
}
