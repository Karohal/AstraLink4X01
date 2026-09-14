using Game.Core;

namespace Game.Logistics
{
    // FR-031 tranche US4 : traduction TransportTask <-> TransportTaskSnapshot. CapaciteParCycle
    // n'est pas persistée (elle se déduit du catalogue/du type de véhicule au chargement).
    public static class TransportTaskSnapshotMapper
    {
        public static TransportTaskSnapshot ToSnapshot(TransportTask task)
        {
            return new TransportTaskSnapshot
            {
                Id = task.Id,
                ResourceId = task.ResourceId,
                SourceBuildingId = task.SourceBuildingId,
                DestinationBuildingId = task.DestinationBuildingId,
                AssignedColonistId = task.AssignedColonistId,
                AssignedVehicleId = task.AssignedVehicleId
            };
        }

        public static TransportTask FromSnapshot(TransportTaskSnapshot snapshot, float capacityPerCycle)
        {
            return new TransportTask(snapshot.Id, snapshot.ResourceId, snapshot.SourceBuildingId, snapshot.DestinationBuildingId, capacityPerCycle)
            {
                AssignedColonistId = snapshot.AssignedColonistId,
                AssignedVehicleId = snapshot.AssignedVehicleId
            };
        }
    }
}
