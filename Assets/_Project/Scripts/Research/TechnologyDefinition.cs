using UnityEngine;

namespace Game.Research
{
    [CreateAssetMenu(menuName = "AstraLink/Research/Technology Definition", fileName = "TechnologyDefinition")]
    public sealed class TechnologyDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _targetResourceId;
        [SerializeField] private float _progressRequired = 100f;

        public string Id => _id;
        public string TargetResourceId => _targetResourceId;
        public float ProgressRequired => _progressRequired;
    }
}
