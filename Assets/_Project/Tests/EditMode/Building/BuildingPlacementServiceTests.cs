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

        // FR-055 : la pompe ne se construit plus jamais directement sur l'eau (contrairement à
        // l'ancienne règle FR-044) ; elle exige soit une case adjacente à l'eau, soit un gisement
        // nappe phréatique sur sa propre case. Grille 3x3 : (1,1) = eau + gisement, (0,1) = terre
        // adjacente sans gisement, (0,0) = terre non adjacente, (2,2) = terre avec son propre
        // gisement (nappe phréatique).
        private static Planet CreatePlanetWithWaterAndLand()
        {
            var zones = new Zone[3, 3];
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
            {
                Zone zone;
                if (x == 1 && y == 1)
                    zone = new Zone(new GridPosition(x, y), TerrainType.Water, false, new Deposit("water", 500f, true));
                else if (x == 2 && y == 2)
                    zone = new Zone(new GridPosition(x, y), TerrainType.Plains, true, new Deposit("water", 200f, true)); // nappe phréatique
                else
                    zone = new Zone(new GridPosition(x, y), TerrainType.Plains, true);

                zone.Reveal();
                zones[x, y] = zone;
            }

            return new Planet(System.Guid.NewGuid(), 0, 3, 3, zones);
        }

        private static BuildingDefinition CreatePumpDefinition()
        {
            var pumpDefinition = ScriptableObject.CreateInstance<BuildingDefinition>();
            pumpDefinition.Initialize("pump", "Pump", new ResourceAmount[0], 0f, extractsAdjacentWater: true);
            return pumpDefinition;
        }

        [Test]
        public void CanBuild_Pump_OnWaterTile_ReturnsFalse()
        {
            var planet = CreatePlanetWithWaterAndLand();
            var pumpDefinition = CreatePumpDefinition();
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            var canBuild = service.CanBuild(planet, pumpDefinition, 1, 1, inventory, out var missing);

            Assert.IsFalse(canBuild); // FR-055 : jamais directement sur l'eau
            CollectionAssert.Contains(missing, "cannot-build-on-water");
        }

        [Test]
        public void CanBuild_Pump_OnLandAdjacentToWater_ReturnsTrue_AndTargetsNeighborDeposit()
        {
            var planet = CreatePlanetWithWaterAndLand();
            var pumpDefinition = CreatePumpDefinition();
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            Assert.IsTrue(service.CanBuild(planet, pumpDefinition, 0, 1, inventory, out _)); // FR-055 : adjacent à (1,1)

            var pump = service.Build(planet, pumpDefinition, 0, 1, inventory);
            Assert.AreEqual(0, pump.X);
            Assert.AreEqual(1, pump.Y);
            Assert.AreEqual(1, pump.DepositX); // exploite le gisement voisin, pas le sien
            Assert.AreEqual(1, pump.DepositY);
        }

        [Test]
        public void CanBuild_Pump_OnLandNotAdjacentToWater_ReturnsFalse()
        {
            var planet = CreatePlanetWithWaterAndLand();
            var pumpDefinition = CreatePumpDefinition();
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            var canBuild = service.CanBuild(planet, pumpDefinition, 0, 0, inventory, out var missing); // aucun voisin en eau

            Assert.IsFalse(canBuild);
            CollectionAssert.Contains(missing, "no-adjacent-water");
        }

        [Test]
        public void CanBuild_Pump_OnGroundwaterDeposit_ReturnsTrue_AndTargetsOwnZone()
        {
            var planet = CreatePlanetWithWaterAndLand();
            var pumpDefinition = CreatePumpDefinition();
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            Assert.IsTrue(service.CanBuild(planet, pumpDefinition, 2, 2, inventory, out _)); // nappe phréatique

            var pump = service.Build(planet, pumpDefinition, 2, 2, inventory);
            Assert.AreEqual(2, pump.DepositX); // exploite son propre gisement
            Assert.AreEqual(2, pump.DepositY);
        }

        [Test]
        public void Build_NonPumpDefinition_DepositTargetDefaultsToOwnZone()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(0f);
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            var building = service.Build(planet, definition, 2, 2, inventory);

            Assert.AreEqual(2, building.DepositX);
            Assert.AreEqual(2, building.DepositY);
        }

        [Test]
        public void Build_WithOffsetAndRotation_PersistsCosmeticPlacement()
        {
            var planet = CreateRevealedPlanet();
            var definition = CreateDefinition(0f);
            var inventory = new Inventory();

            var service = new BuildingPlacementService();
            var building = service.Build(planet, definition, 2, 2, inventory, offsetX: 0.2f, offsetY: 0.8f, rotation: BuildingRotation.Deg90);

            Assert.AreEqual(0.2f, building.OffsetX); // FR-054 : purement cosmétique
            Assert.AreEqual(0.8f, building.OffsetY);
            Assert.AreEqual(BuildingRotation.Deg90, building.Rotation);
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
