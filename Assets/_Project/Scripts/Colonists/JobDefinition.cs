using UnityEngine;

namespace Game.Colonists
{
    [CreateAssetMenu(menuName = "AstraLink/Colonists/Job Definition", fileName = "JobDefinition")]
    public sealed class JobDefinition : ScriptableObject
    {
        public const string ResearcherJobId = "researcher";

        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        public string Id => _id;
        public string DisplayName => _displayName;
    }
}
