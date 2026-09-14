using UnityEngine;

namespace Game.Logistics
{
    // Catalogue de contenu extensible (FR-044) : le colon (transporteur de base) est une première
    // entrée ; de futurs véhicules (camions, etc.) suivront le même principe avec leurs propres
    // capacités, sans modification de la logique de transport elle-même.
    [CreateAssetMenu(menuName = "AstraLink/Logistics/Transporter Definition", fileName = "TransporterDefinition")]
    public sealed class TransporterDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private float _massCapacityKg = 12f;
        [SerializeField] private float _volumeCapacityCubicMeters = 0.012f;
        [SerializeField] private float _speedTilesPerSecond = 2f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float MassCapacityKg => _massCapacityKg;
        public float VolumeCapacityCubicMeters => _volumeCapacityCubicMeters;
        public float SpeedTilesPerSecond => _speedTilesPerSecond;

        // Construction programmatique (tests, imports de contenu) puisque les champs sont privés
        // et normalement renseignés via l'Inspector.
        public void Initialize(string id, string displayName, float massCapacityKg, float volumeCapacityCubicMeters, float speedTilesPerSecond = 2f)
        {
            _id = id;
            _displayName = displayName;
            _massCapacityKg = massCapacityKg;
            _volumeCapacityCubicMeters = volumeCapacityCubicMeters;
            _speedTilesPerSecond = speedTilesPerSecond;
        }
    }
}
