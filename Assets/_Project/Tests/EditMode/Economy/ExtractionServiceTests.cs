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
    }
}
