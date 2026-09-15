using System;
using System.Collections.Generic;
using Game.Building;
using Game.Economy;
using Game.Procedural;
using NUnit.Framework;

namespace Game.Tests.EditMode.Economy
{
    public class MultiPurposeExtractorServiceTests
    {
        private static readonly HashSet<string> AllowedResourceIds = new HashSet<string> { "water", "stone", "wood" };

        private static Planet CreatePlanetWithDeposit(string resourceId, float quantity = 100f, bool isInfinite = false)
        {
            var zones = new Zone[2, 2];
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            {
                Deposit deposit = null;
                if (x == 0 && y == 0) deposit = new Deposit(resourceId, quantity, isInfinite);
                if (x == 1 && y == 1) deposit = new Deposit("iron", quantity); // gisement incompatible (FR-051)

                var zone = new Zone(new GridPosition(x, y), TerrainType.Plains, true, deposit);
                zone.Reveal();
                zones[x, y] = zone;
            }

            return new Planet(Guid.NewGuid(), 0, 2, 2, zones);
        }

        [Test]
        public void CanPlaceOn_ReturnsTrue_ForCompatibleDeposit()
        {
            var planet = CreatePlanetWithDeposit("water");
            var service = new MultiPurposeExtractorService();

            Assert.IsTrue(service.CanPlaceOn(planet, AllowedResourceIds, null, 0, 0, out _)); // FR-051
        }

        [Test]
        public void CanPlaceOn_ReturnsFalse_ForIncompatibleDeposit()
        {
            var planet = CreatePlanetWithDeposit("water");
            var service = new MultiPurposeExtractorService();

            Assert.IsFalse(service.CanPlaceOn(planet, AllowedResourceIds, null, 1, 1, out var reason)); // gisement de fer
            Assert.AreEqual("incompatible-deposit", reason);
        }

        [Test]
        public void CanPlaceOn_ReturnsFalse_WhenAnotherExtractorAlreadyOnSameDeposit()
        {
            var planet = CreatePlanetWithDeposit("water");
            var service = new MultiPurposeExtractorService();
            var other = new MultiPurposeExtractor(Guid.NewGuid());
            other.PlaceOn(0, 0);

            var canPlace = service.CanPlaceOn(planet, AllowedResourceIds, new[] { other }, 0, 0, out var reason);

            Assert.IsFalse(canPlace);
            Assert.AreEqual("deposit-occupied", reason);
        }

        [Test]
        public void PlaceOn_ThenMoveTo_UpdatesPositionWithoutDestroyingInstance()
        {
            var planet = CreatePlanetWithDeposit("wood");
            var extractor = new MultiPurposeExtractor(Guid.NewGuid());
            var service = new MultiPurposeExtractorService();

            service.PlaceOn(extractor, planet, AllowedResourceIds, null, 0, 0);
            Assert.IsTrue(extractor.IsPlaced);
            Assert.AreEqual(0, extractor.TargetX);

            // FR-052 : même instance déplacée, jamais détruite/reconstruite.
            var sameInstanceId = extractor.Id;
            service.MoveTo(extractor, planet, AllowedResourceIds, null, 0, 0); // re-place sur le même gisement, doit rester accepté
            Assert.AreEqual(sameInstanceId, extractor.Id);
            Assert.AreEqual(0, extractor.TargetX);
            Assert.AreEqual(0, extractor.TargetY);
        }

        [Test]
        public void Tick_DoesNothing_WhenNotPlaced()
        {
            var planet = CreatePlanetWithDeposit("stone");
            var extractor = new MultiPurposeExtractor(Guid.NewGuid());
            var service = new MultiPurposeExtractorService();
            var target = new Inventory();

            service.Tick(extractor, planet, target, deltaSimTime: 5f);

            Assert.AreEqual(0f, target.GetQuantity("stone"));
        }

        [Test]
        public void Tick_ExtractsIntoTargetInventory_OnceStoneIsPlaced()
        {
            var planet = CreatePlanetWithDeposit("stone", quantity: 10f);
            var extractor = new MultiPurposeExtractor(Guid.NewGuid());
            var service = new MultiPurposeExtractorService();
            service.PlaceOn(extractor, planet, AllowedResourceIds, null, 0, 0);
            var target = new Inventory();

            service.Tick(extractor, planet, target, deltaSimTime: 1f); // débloque + extrait automatiquement (FR-051/FR-052)

            Assert.Greater(target.GetQuantity("stone"), 0f);
            planet.TryGetZone(0, 0, out var zone);
            Assert.AreEqual(DepositState.Extracting, zone.Deposit.State);
        }
    }
}
