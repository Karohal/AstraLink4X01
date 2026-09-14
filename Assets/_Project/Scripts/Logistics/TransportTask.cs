using System;

namespace Game.Logistics
{
    public sealed class TransportTask
    {
        public Guid Id { get; }
        public string ResourceId { get; }
        public Guid SourceBuildingId { get; }
        public Guid DestinationBuildingId { get; }
        public float CapacityPerCycle { get; }
        public Guid? AssignedColonistId { get; set; }
        public Guid? AssignedVehicleId { get; set; }

        public TransportTask(Guid id, string resourceId, Guid sourceBuildingId, Guid destinationBuildingId, float capacityPerCycle)
        {
            Id = id;
            ResourceId = resourceId;
            SourceBuildingId = sourceBuildingId;
            DestinationBuildingId = destinationBuildingId;
            CapacityPerCycle = capacityPerCycle;
        }

        public bool IsAssigned => AssignedColonistId.HasValue || AssignedVehicleId.HasValue; // FR-013
    }
}
