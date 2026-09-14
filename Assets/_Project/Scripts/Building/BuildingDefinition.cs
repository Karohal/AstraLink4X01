using System;
using UnityEngine;

namespace Game.Building
{
    [Serializable]
    public struct ResourceAmount
    {
        public string ResourceId;
        public float Quantity;
    }

    // Catalogue de contenu extensible (FR-044) : de nouveaux assets BuildingDefinition peuvent être
    // ajoutés sans modifier la spec, le plan, ou ce script.
    [CreateAssetMenu(menuName = "AstraLink/Building/Building Definition", fileName = "BuildingDefinition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ResourceAmount[] _cost;
        [SerializeField] private float _constructionDuration = 10f;
        [SerializeField] private string[] _technologyPrerequisiteIds;
        [SerializeField] private int _fogRadius = 5;
        [SerializeField] private string[] _jobIds;
        [SerializeField] private bool _isHousing;
        [SerializeField] private bool _canBuildOnWater;
        [SerializeField] private float _recycleRefundRatio = 0.5f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public ResourceAmount[] Cost => _cost ?? Array.Empty<ResourceAmount>();
        public float ConstructionDuration => _constructionDuration;
        public string[] TechnologyPrerequisiteIds => _technologyPrerequisiteIds ?? Array.Empty<string>();
        public int FogRadius => _fogRadius;
        public string[] JobIds => _jobIds ?? Array.Empty<string>();
        public bool IsHousing => _isHousing;

        // Vrai uniquement pour les bâtiments d'extraction liquide (pompe) : autorise le placement
        // sur une case d'eau, seule exception à la règle générale Zone.EstConstructible.
        public bool CanBuildOnWater => _canBuildOnWater;

        // Fraction du coût initial remboursée au recyclage (valeur d'équilibrage du catalogue de
        // contenu, pas de la spec — cf. data-model.md § Recyclage).
        public float RecycleRefundRatio => _recycleRefundRatio;

        // Construction programmatique (tests, imports de contenu) puisque les champs sont privés
        // et normalement renseignés via l'Inspector.
        public void Initialize(string id, string displayName, ResourceAmount[] cost, float constructionDuration,
            string[] technologyPrerequisiteIds = null, int fogRadius = 5, string[] jobIds = null, bool isHousing = false,
            bool canBuildOnWater = false, float recycleRefundRatio = 0.5f)
        {
            _id = id;
            _displayName = displayName;
            _cost = cost;
            _constructionDuration = constructionDuration;
            _technologyPrerequisiteIds = technologyPrerequisiteIds;
            _fogRadius = fogRadius;
            _jobIds = jobIds;
            _isHousing = isHousing;
            _canBuildOnWater = canBuildOnWater;
            _recycleRefundRatio = recycleRefundRatio;
        }
    }
}
