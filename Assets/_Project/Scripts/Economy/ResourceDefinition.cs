using UnityEngine;

namespace Game.Economy
{
    [CreateAssetMenu(menuName = "AstraLink/Economy/Resource Definition", fileName = "ResourceDefinition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private bool _isRaw = true;

        // kg/m³ (densité réelle quand connue, ex: eau 1000, bois 650, pierre 2600, fer 7870,
        // cuivre 8960, nickel 8900, uranium 19050) : propriété de contenu, pas une liste codée en
        // dur — chaque nouvelle ressource/minerai définit la sienne ici. Utilisée pour la double
        // limite de capacité de transport (masse ET volume), cf. Game.Logistics.TransportCapacityCalculator.
        [SerializeField] private float _densityKgPerCubicMeter = 1000f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public bool IsRaw => _isRaw;
        public float DensityKgPerCubicMeter => _densityKgPerCubicMeter;

        // Construction programmatique (tests, imports de contenu) puisque les champs sont privés
        // et normalement renseignés via l'Inspector.
        public void Initialize(string id, string displayName, bool isRaw = true, float densityKgPerCubicMeter = 1000f)
        {
            _id = id;
            _displayName = displayName;
            _isRaw = isRaw;
            _densityKgPerCubicMeter = densityKgPerCubicMeter;
        }
    }
}
