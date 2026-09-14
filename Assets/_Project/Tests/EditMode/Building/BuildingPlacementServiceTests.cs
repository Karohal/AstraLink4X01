using Game.Building;
using Game.Economy;
using Game.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Building
{
    public class BuildingPlacementServiceTests
    {
        private static Planet CreateRevealedPlanet()
        {
            var zones = new Zone[5, 5];
            for (var x = 0; x < 5; x++)
            for (var y = 0; y < 5; y++)
            {
                var zone = new Zone(new GridPosition(x, y), TerrainType.Plains, true);
                zone.Reveal();
                zones[x, y] = zone;
            }

            return new Planet(System.Guid.NewGuid(), 0, 5, 5, zones);
        }

        private static BuildingDefinition CreateDefinition(float woodCost)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            definition.Initialize(
                id: "warehouse",
                displayName: "Warehouse",
                cost: new[] { new ResourceAmount { ResourceId = "wood", Quantity = woodCost } },
                constructionDuration: 10f);
            return definition;
        }

        [Test]
        public void Build_WithSufficientResources_DeductsCostAndStartsUnderConstruction()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(50f);
            var inventory = new Inventory();
            inventory.TryAdd("wood", 100f);

            var service = new BuildingPlacementService();
            var building = service.Build(planet, definition, 2, 2, inventory);

            Assert.AreEqual(50f, inventory.GetQuantity("wood"));
            Assert.AreEqual(BuildingState.UnderConstruction, building.State); // FR-042
        }

        [Test]
        public void CanBuild_WithInsufficientResources_ReturnsFalseWithMissingResource()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(50f);
            var inventory = new Inventory(); // vide

            var service = new BuildingPlacementService();
            var canBuild = service.CanBuild(planet, definition, 2, 2, inventory, out var missing);

            Assert.IsFalse(canBuild); // FR-006
            CollectionAssert.Contains(missing, "wood");
        }
    }
}
