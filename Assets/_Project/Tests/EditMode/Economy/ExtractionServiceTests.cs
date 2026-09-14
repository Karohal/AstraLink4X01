using System;
using Game.Building;
using Game.Economy;
using Game.Procedural;
using Game.Research;
using NUnit.Framework;

namespace Game.Tests.EditMode.Economy
{
    public class ExtractionServiceTests
    {
        [Test]
        public void CanBuildExtractor_ReturnsFalse_WhenTechnologyNotUnlocked()
        {
            var deposit = new Deposit("iron", 100f);
            var technology = new Technology("iron-extraction", "iron", 10f);
            var service = new ExtractionService();

            Assert.IsFalse(service.CanBuildExtractor(deposit, technology)); // FR-009
        }

        [Test]
        public void CanBuildExtractor_ReturnsTrue_WhenTechnologyUnlocked()
        {
            var deposit = new Deposit("iron", 100f);
            var technology = new Technology("iron-extraction", "iron", 10f);
            technology.AddProgress(10f);

            var service = new ExtractionService();

            Assert.IsTrue(service.CanBuildExtractor(deposit, technology));
        }

        [Test]
        public void Tick_ExtractsIntoOutputBuffer_UntilDepleted_ThenStops()
        {
            var deposit = new Deposit("iron", 10f);
            var service = new ExtractionService();
            service.BeginExtraction(deposit);

            var extractor = new BuildingInstance(Guid.NewGuid(), "extractor-iron", 0, 0, isStartingShelter: true);
            var outputBuffer = new Inventory();

            service.Tick(deposit, extractor, outputBuffer, deltaSimTime: 1f); // 5 unités/s -> reste 5
            Assert.AreEqual(5f, deposit.RemainingQuantity);
            Assert.AreEqual(5f, outputBuffer.GetQuantity("iron")); // FR-012 : accumulé au site
            Assert.AreEqual(DepositState.Extracting, deposit.State);

            service.Tick(deposit, extractor, outputBuffer, deltaSimTime: 3f); // au-delà du restant -> épuisé
            Assert.AreEqual(0f, deposit.RemainingQuantity);
            Assert.AreEqual(10f, outputBuffer.GetQuantity("iron"));
            Assert.AreEqual(DepositState.Depleted, deposit.State); // FR-011
        }

        [Test]
        public void Tick_OnInfiniteDeposit_NeverDepletes()
        {
            var deposit = new Deposit("wood", 10f, isInfinite: true); // ressource durable
            var service = new ExtractionService();
            service.BeginExtraction(deposit);

            var extractor = new BuildingInstance(Guid.NewGuid(), "extractor-wood", 0, 0, isStartingShelter: true);
            var outputBuffer = new Inventory();

            service.Tick(deposit, extractor, outputBuffer, deltaSimTime: 10f); // largement au-delà du stock initial

            Assert.AreEqual(10f, deposit.RemainingQuantity); // inchangé
            Assert.AreEqual(50f, outputBuffer.GetQuantity("wood")); // extraction toujours effective
            Assert.AreEqual(DepositState.Extracting, deposit.State); // jamais Epuise
        }

        [Test]
        public void Tick_ScalesByYieldMultiplier()
        {
            var deposit = new Deposit("iron", 1000f);
            var service = new ExtractionService();
            service.BeginExtraction(deposit);

            var extractor = new BuildingInstance(Guid.NewGuid(), "extractor-iron", 0, 0, isStartingShelter: true);
            var outputBuffer = new Inventory();

            service.Tick(deposit, extractor, outputBuffer, deltaSimTime: 1f, yieldMultiplier: 0.2f); // 5 * 0.2 = 1 unité/s

            Assert.AreEqual(1f, outputBuffer.GetQuantity("iron"));
        }

        [Test]
        public void Tick_WithZeroYield_ExtractsNothing()
        {
            var deposit = new Deposit("iron", 1000f);
            var service = new ExtractionService();
            service.BeginExtraction(deposit);

            var extractor = new BuildingInstance(Guid.NewGuid(), "extractor-iron", 0, 0, isStartingShelter: true);
            var outputBuffer = new Inventory();

            service.Tick(deposit, extractor, outputBuffer, deltaSimTime: 5f, yieldMultiplier: 0f); // aucun travailleur

            Assert.AreEqual(0f, outputBuffer.GetQuantity("iron"));
            Assert.AreEqual(1000f, deposit.RemainingQuantity);
        }
    }
}
