using System;
using Game.Economy;
using Game.Logistics;
using NUnit.Framework;

namespace Game.Tests.EditMode.Logistics
{
    public class TransportServiceTests
    {
        [Test]
        public void Tick_WithoutAssignment_DoesNotMoveResource()
        {
            var source = new Inventory();
            source.TryAdd("iron", 50f);
            var destination = new Inventory();

            var task = new TransportTask(Guid.NewGuid(), "iron", Guid.NewGuid(), Guid.NewGuid(), capacityPerCycle: 10f);
            var service = new TransportService();

            service.Tick(task, source, destination, deltaSimTime: 1f);

            Assert.AreEqual(50f, source.GetQuantity("iron")); // FR-014
            Assert.AreEqual(0f, destination.GetQuantity("iron"));
        }

        [Test]
        public void Tick_WithAssignment_MovesResourceProgressively()
        {
            var source = new Inventory();
            source.TryAdd("iron", 50f);
            var destination = new Inventory();

            var service = new TransportService();
            var task = service.Assign(Guid.NewGuid(), Guid.NewGuid(), "iron", 10f, Guid.NewGuid(), null);

            service.Tick(task, source, destination, deltaSimTime: 1f);

            Assert.AreEqual(40f, source.GetQuantity("iron"));
            Assert.AreEqual(10f, destination.GetQuantity("iron")); // FR-012/FR-013
        }

        [Test]
        public void Tick_InsufficientCapacity_AccumulatesAtSource()
        {
            var source = new Inventory();
            source.TryAdd("iron", 100f);
            var destination = new Inventory();

            var service = new TransportService();
            var task = service.Assign(Guid.NewGuid(), Guid.NewGuid(), "iron", 5f, Guid.NewGuid(), null);

            service.Tick(task, source, destination, deltaSimTime: 1f);

            Assert.AreEqual(95f, source.GetQuantity("iron"));
            Assert.AreEqual(5f, destination.GetQuantity("iron"));
        }
    }
}
