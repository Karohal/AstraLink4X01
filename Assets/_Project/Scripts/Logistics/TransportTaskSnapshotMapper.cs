using System;
using Game.Core;

namespace Game.Logistics
{
    // FR-031 tranche US4 : traduction TransportTask <-> TransportTaskSnapshot. La configuration du
    // cycle (durées de trajet/chargement, capacité) n'est pas persistée — elle dépend du contenu et
    // de la position des bâtiments, recalculée par l'appelant au chargement ; seul l'état
    // dynamique du cycle (phase, progression, charge transportée) l'est.
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
                AssignedVehicleId = task.AssignedVehicleId,
                Phase = task.Phase.ToString(),
                PhaseProgress = task.PhaseProgress,
                CarriedQuantity = task.CarriedQuantity
            };
        }

        public static TransportTask FromSnapshot(TransportTaskSnapshot snapshot)
        {
            var task = new TransportTask(snapshot.Id, snapshot.ResourceId, snapshot.SourceBuildingId, snapshot.DestinationBuildingId)
            {
                AssignedColonistId = snapshot.AssignedColonistId,
                AssignedVehicleId = snapshot.AssignedVehicleId
            };

            var phase = (TransportCyclePhase)Enum.Parse(typeof(TransportCyclePhase), snapshot.Phase);
            task.RestoreState(phase, snapshot.PhaseProgress, snapshot.CarriedQuantity);

            return task;
        }
    }
}
