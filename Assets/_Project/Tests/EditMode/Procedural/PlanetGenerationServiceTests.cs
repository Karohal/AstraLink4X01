using System;
using Game.Procedural;
using NUnit.Framework;

namespace Game.Tests.EditMode.Procedural
{
    public class PlanetGenerationServiceTests
    {
        [Test]
        public void Generate_ProducesGridOfRequestedSize()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(1, 10, 8, new[] { "iron" });

            Assert.AreEqual(10, planet.Width);
            Assert.AreEqual(8, planet.Height);
            Assert.IsTrue(planet.TryGetZone(0, 0, out _));
            Assert.IsFalse(planet.TryGetZone(10, 0, out _));
        }

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            var service = new PlanetGenerationService();
            var planetA = service.Generate(42, 20, 20, new[] { "iron" });
            var planetB = service.Generate(42, 20, 20, new[] { "iron" });

            for (var x = 0; x < 20; x++)
            for (var y = 0; y < 20; y++)
                Assert.AreEqual(planetA.Zones[x, y].Terrain, planetB.Zones[x, y].Terrain);
        }

        [Test]
        public void Generate_AllZonesStartUnrevealed()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(1, 5, 5, Array.Empty<string>());

            foreach (var zone in planet.Zones)
                Assert.IsFalse(zone.IsRevealed);
        }

        [Test]
        public void Generate_WithWaterResourceId_EveryWaterZoneHasDeposit()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(7, 30, 30, new[] { "wood", "stone" }, waterResourceId: "water");

            foreach (var zone in planet.Zones)
            {
                if (zone.Terrain == TerrainType.Water)
                    Assert.IsNotNull(zone.Deposit, "chaque case d'eau doit porter un gisement d'eau (FR-001)");
                if (zone.Deposit != null && zone.Terrain == TerrainType.Water)
                    Assert.AreEqual("water", zone.Deposit.ResourceId);
            }
        }

        [Test]
        public void Generate_WithInfiniteResourceId_FlagsMatchingDepositsAsInfinite()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(3, 40, 40, new[] { "wood", "stone" }, infiniteResourceIds: new[] { "wood" });

            var foundWoodDeposit = false;
            var foundStoneDeposit = false;
            foreach (var zone in planet.Zones)
            {
                if (zone.Deposit == null) continue;
                if (zone.Deposit.ResourceId == "wood")
                {
                    foundWoodDeposit = true;
                    Assert.IsTrue(zone.Deposit.IsInfinite);
                }
                else if (zone.Deposit.ResourceId == "stone")
                {
                    foundStoneDeposit = true;
                    Assert.IsFalse(zone.Deposit.IsInfinite);
                }
            }

            Assert.IsTrue(foundWoodDeposit && foundStoneDeposit, "seed/taille insuffisants pour couvrir les deux types de gisement");
        }

        [Test]
        public void Generate_WithWaterResourceIdAlsoInfinite_WaterDepositUnderWaterTileIsInfinite()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(7, 20, 20, new[] { "wood", "stone" },
                infiniteResourceIds: new[] { "wood", "water" }, waterResourceId: "water");

            var foundWaterDeposit = false;
            foreach (var zone in planet.Zones)
            {
                if (zone.Terrain != TerrainType.Water || zone.Deposit == null) continue;
                foundWaterDeposit = true;
                Assert.IsTrue(zone.Deposit.IsInfinite); // régression : ignoré pour les cases d'eau
            }

            Assert.IsTrue(foundWaterDeposit, "seed/taille insuffisants pour générer une case d'eau");
        }
    }
}
