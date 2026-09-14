using UnityEngine;

namespace Game.Colonists
{
    [CreateAssetMenu(menuName = "AstraLink/Colonists/Job Definition", fileName = "JobDefinition")]
    public sealed class JobDefinition : ScriptableObject
    {
        // Nomenclature officielle des métiers (utilisée dans toute l'interface) :
        public const string ResearcherJobId = "researcher"; // Chercheur — recherche
        public const string WoodcutterJobId = "woodcutter"; // Bûcheron — extraction du bois spécifiquement
        public const string MinerJobId = "miner"; // Mineur — extraction hors bois (pierre, eau, futurs minerais)
        public const string MasonJobId = "mason"; // Maçon — chantiers
        public const string EngineerJobId = "engineer"; // Ingénieur — projets/ingénierie (réservé, pas encore de mécanique associée)

        [SerializeField] private string _id;
        [SerializeField] private string _displayName;

        public string Id => _id;
        public string DisplayName => _displayName;
    }
}
