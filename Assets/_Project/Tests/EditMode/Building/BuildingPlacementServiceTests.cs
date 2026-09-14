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

        [Test]
        public void CanBuild_OnWaterTile_ReturnsFalse_UnlessDefinitionAllowsWater()
        {
            var zones = new Zone[1, 1];
            var waterZone = new Zone(new GridPosition(0, 0), TerrainType.Water, isBuildable: false);
            waterZone.Reveal();
            zones[0, 0] = waterZone;
            var planet = new Planet(System.Guid.NewGuid(), 0, 1, 1, zones);

            var landOnlyDefinition = CreateDefinition(0f);
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            Assert.IsFalse(service.CanBuild(planet, landOnlyDefinition, 0, 0, inventory, out _)); // pas de pompe

            var pumpDefinition = ScriptableObject.CreateInstance<BuildingDefinition>();
            pumpDefinition.Initialize("pump", "Pump", new ResourceAmount[0], 0f, canBuildOnWater: true);

            Assert.IsTrue(service.CanBuild(planet, pumpDefinition, 0, 0, inventory, out _)); // FR-044
        }

        [Test]
        public void CanBuild_OnAlreadyOccupiedZone_ReturnsFalse()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(0f);
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            service.Build(planet, definition, 2, 2, inventory);

            var canBuildAgain = service.CanBuild(planet, definition, 2, 2, inventory, out var missing);

            Assert.IsFalse(canBuildAgain); // une zone occupée ne peut pas recevoir un second bâtiment
            CollectionAssert.Contains(missing, "zone-occupied");
        }

        [Test]
        public void Recycle_RefundsPartialCost_AndFreesZone()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(50f); // ratio par défaut 0.5
            var inventory = new Inventory();
            inventory.TryAdd("wood", 100f);

            var service = new BuildingPlacementService();
            var building = service.Build(planet, definition, 2, 2, inventory);
            Assert.AreEqual(50f, inventory.GetQuantity("wood"));

            service.Recycle(planet, building, definition, inventory);

            Assert.AreEqual(75f, inventory.GetQuantity("wood")); // 50 restant + 50% de 50 remboursé
            planet.TryGetZone(2, 2, out var zone);
            Assert.IsNull(zone.BuildingId); // zone libérée, reconstructible
        }

        [Test]
        public void TryCancelConstruction_BeforeAnyProgress_RefundsFullCost_AndFreesZone()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(50f);
            var inventory = new Inventory();
            inventory.TryAdd("wood", 100f);

            var service = new BuildingPlacementService();
            var building = service.Build(planet, definition, 2, 2, inventory);
            Assert.AreEqual(50f, inventory.GetQuantity("wood"));

            var cancelled = service.TryCancelConstruction(planet, building, definition, inventory);

            Assert.IsTrue(cancelled);
            Assert.AreEqual(100f, inventory.GetQuantity("wood")); // remboursement intégral, rien n'a été construit
            planet.TryGetZone(2, 2, out var zone);
            Assert.IsNull(zone.BuildingId);
        }

        [Test]
        public void TryCancelConstruction_AfterProgressStarted_ReturnsFalse_AndRefundsNothing()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(50f);
            var inventory = new Inventory();
            inventory.TryAdd("wood", 100f);

            var service = new BuildingPlacementService();
            var building = service.Build(planet, definition, 2, 2, inventory);
            building.AdvanceConstruction(1f, definition.ConstructionDuration); // un colon a déjà travaillé

            var cancelled = service.TryCancelConstruction(planet, building, definition, inventory);

            Assert.IsFalse(cancelled); // doit passer par Recycle (remboursement partiel) à la place
            Assert.AreEqual(50f, inventory.GetQuantity("wood")); // aucun remboursement
            planet.TryGetZone(2, 2, out var zone);
            Assert.AreEqual(building.Id, zone.BuildingId); // toujours en place
        }
    }
}
