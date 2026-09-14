using System;
using Game.Economy;

namespace Game.Logistics
{
    public interface ITransportService
    {
        TransportTask Assign(Guid sourceBuildingId, Guid destinationBuildingId, string resourceId, float capacityPerCycle, Guid? colonistId, Guid? vehicleId); // FR-013
        void Tick(TransportTask task, Inventory source, Inventory destination, float deltaSimTime); // FR-012/FR-014
    }

    public sealed class TransportService : ITransportService
    {
        public TransportTask Assign(Guid sourceBuildingId, Guid destinationBuildingId, string resourceId, float capacityPerCycle, Guid? colonistId, Guid? vehicleId)
        {
            return new TransportTask(Guid.NewGuid(), resourceId, sourceBuildingId, destinationBuildingId, capacityPerCycle)
            {
                AssignedColonistId = colonistId,
                AssignedVehicleId = vehicleId
            };
        }

        public void Tick(TransportTask task, Inventory source, Inventory destination, float deltaSimTime)
        {
            if (task == null || !task.IsAssigned) return; // FR-014 : rien sans transport assigné

            var available = source.GetQuantity(task.ResourceId);
            if (available <= 0f) return;

            var amountToMove = Math.Min(available, task.CapacityPerCycle * deltaSimTime);
            if (amountToMove <= 0f) return;

            if (source.TryRemove(task.ResourceId, amountToMove))
                destination.TryAdd(task.ResourceId, amountToMove); // FR-012/FR-013
        }
    }
}
