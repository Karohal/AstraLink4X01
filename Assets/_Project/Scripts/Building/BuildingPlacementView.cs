using System;
using Game.Economy;
using Game.Procedural;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Building
{
    // Orchestration MonoBehaviour (Principe II) : traduit les clics du menu/de la grille en appels
    // à IBuildingPlacementService ; aucune règle métier ici (coût, chantier, etc. vivent dans le
    // service).
    public sealed class BuildingPlacementView : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private BuildingDefinition[] _availableBuildings;

        private VisualElement _root;
        private BuildingDefinition _selectedDefinition;

        public IBuildingPlacementService PlacementService { get; set; } = new BuildingPlacementService();
        public Planet Planet { get; set; }
        public Inventory Inventory { get; set; }

        public event Action<string> BuildingPlacementFailed;
        public event Action<BuildingInstance> BuildingPlaced;

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            BuildMenu();
        }

        private void BuildMenu()
        {
            if (_root == null || _availableBuildings == null) return;

            var menu = new VisualElement { name = "build-menu" };
            foreach (var definition in _availableBuildings)
            {
                var captured = definition;
                var button = new Button(() => _selectedDefinition = captured) { text = captured.DisplayName };
                menu.Add(button);
            }

            _root.Add(menu);
        }

        // Appelé par l'orchestrateur de sélection de zone (grille/raycast), hors périmètre de ce
        // script pour rester découplé de la représentation visuelle de la planète.
        public void TryPlaceAt(int x, int y)
        {
            if (_selectedDefinition == null || Planet == null || Inventory == null) return;

            if (!PlacementService.CanBuild(Planet, _selectedDefinition, x, y, Inventory, out var missing))
            {
                BuildingPlacementFailed?.Invoke(string.Join(",", missing));
                return;
            }

            var building = PlacementService.Build(Planet, _selectedDefinition, x, y, Inventory);
            BuildingPlaced?.Invoke(building);
        }
    }
}
