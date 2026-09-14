using System;
using Game.Economy;
using Game.Logistics;
using NUnit.Framework;

namespace Game.Tests.EditMode.Logistics
{
    public class TransportServiceTests
    {
        private static TransportCycleConfig CreateConfig(float travel = 2f, float loadUnload = 5f, float maxLoadKg = 10f)
        {
            return new TransportCycleConfig(travel, loadUnload, travel, loadUnload, maxLoadKg);
        }

        [Test]
        public void Tick_WithoutAssignment_DoesNotMoveResourceOrProgressPhase()
        {
            var source = new Inventory();
            source.TryAdd("iron", 50f);
            var destination = new Inventory();

            var task = new TransportTask(Guid.NewGuid(), "iron", Guid.NewGuid(), Guid.NewGuid());
            var service = new TransportService();

            service.Tick(task, source, destination, CreateConfig(), deltaSimTime: 100f);

            Assert.AreEqual(50f, source.GetQuantity("iron")); // FR-014
            Assert.AreEqual(0f, destination.GetQuantity("iron"));
            Assert.AreEqual(TransportCyclePhase.TravelingToSource, task.Phase);
        }

        [Test]
        public void Tick_Loading_WaitsAtSourceUntilStockAvailable_NoEmptyRoundTrip()
        {
            var source = new Inventory(); // rien à charger
            var destination = new Inventory();

            var service = new TransportService();
            var task = service.Assign(Guid.NewGuid(), Guid.NewGuid(), "iron", Guid.NewGuid(), null);
            var config = CreateConfig(travel: 2f, loadUnload: 5f);

            // Trajet aller (2s) atteint, puis chargement bloqué faute de stock malgré un temps largement suffisant.
            service.Tick(task, source, destination, config, deltaSimTime: 20f);

            Assert.AreEqual(TransportCyclePhase.Loading, task.Phase); // patiente, ne poursuit pas à vide (FR-014)
            Assert.AreEqual(0f, task.CarriedQuantity);
        }

        [Test]
        public void Tick_FullCycle_TravelLoadTravelUnload_MovesResourceAndReturnsToTravelingToSource()
        {
            var source = new Inventory();
            source.TryAdd("iron", 100f);
            var destination = new Inventory();

            var service = new TransportService();
            var task = service.Assign(Guid.NewGuid(), Guid.NewGuid(), "iron", Guid.NewGuid(), null);
            var config = CreateConfig(travel: 2f, loadUnload: 5f, maxLoadKg: 10f); // cycle total = 2+5+2+5 = 14s

            service.Tick(task, source, destination, config, deltaSimTime: 14f);

            Assert.AreEqual(90f, source.GetQuantity("iron")); // chargé une fois (FR-012)
            Assert.AreEqual(10f, destination.GetQuantity("iron")); // livré au bout du cycle complet (FR-013)
            Assert.AreEqual(TransportCyclePhase.TravelingToSource, task.Phase); // le cycle recommence automatiquement
            Assert.AreEqual(0f, task.CarriedQuantity);
        }

        [Test]
        public void Tick_PartWayThroughLoading_DoesNotMoveResourceYet()
        {
            var source = new Inventory();
            source.TryAdd("iron", 100f);
            var destination = new Inventory();

            var service = new TransportService();
            var task = service.Assign(Guid.NewGuid(), Guid.NewGuid(), "iron", Guid.NewGuid(), null);
            var config = CreateConfig(travel: 2f, loadUnload: 5f, maxLoadKg: 10f);

            service.Tick(task, source, destination, config, deltaSimTime: 3f); // 2s trajet + 1s de chargement (sur 5)

            Assert.AreEqual(TransportCyclePhase.Loading, task.Phase);
            Assert.AreEqual(100f, source.GetQuantity("iron")); // rien retiré avant la fin du chargement
            Assert.AreEqual(0f, destination.GetQuantity("iron"));
        }

        [Test]
        public void Tick_MultipleCyclesWithinOneTick_RepeatsAutomatically()
        {
            var source = new Inventory();
            source.TryAdd("iron", 100f);
            var destination = new Inventory();

            var service = new TransportService();
            var task = service.Assign(Guid.NewGuid(), Guid.NewGuid(), "iron", Guid.NewGuid(), null);
            var config = CreateConfig(travel: 2f, loadUnload: 5f, maxLoadKg: 10f); // cycle = 14s

            service.Tick(task, source, destination, config, deltaSimTime: 28f); // deux cycles complets

            Assert.AreEqual(80f, source.GetQuantity("iron"));
            Assert.AreEqual(20f, destination.GetQuantity("iron")); // reprise automatique du cycle (pt.2)
            Assert.AreEqual(TransportCyclePhase.TravelingToSource, task.Phase);
        }
    }
}
