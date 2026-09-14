using System;

namespace Game.Logistics
{
    // Cycle complet d'un transporteur (FR peaufinage US4 pt.2) : trajet aller vers la source,
    // chargement, trajet retour vers la destination, déchargement, puis reprise automatique du
    // cycle. Chaque phase a sa propre durée (fournie par l'appelant via TransportCycleConfig, cf.
    // TransportService.Tick — trajets dépendants du terrain/de la distance, chargement/déchargement
    // fixes et améliorables ultérieurement par le contenu).
    public enum TransportCyclePhase
    {
        TravelingToSource,
        Loading,
        TravelingToDestination,
        Unloading
    }

    public sealed class TransportTask
    {
        public Guid Id { get; }
        public string ResourceId { get; }
        public Guid SourceBuildingId { get; }
        public Guid DestinationBuildingId { get; }
        public Guid? AssignedColonistId { get; set; }
        public Guid? AssignedVehicleId { get; set; }

        public TransportCyclePhase Phase { get; private set; } = TransportCyclePhase.TravelingToSource;
        public float PhaseProgress { get; private set; } // secondes écoulées dans la phase actuelle
        public float CarriedQuantity { get; private set; }

        public TransportTask(Guid id, string resourceId, Guid sourceBuildingId, Guid destinationBuildingId)
        {
            Id = id;
            ResourceId = resourceId;
            SourceBuildingId = sourceBuildingId;
            DestinationBuildingId = destinationBuildingId;
        }

        public bool IsAssigned => AssignedColonistId.HasValue || AssignedVehicleId.HasValue; // FR-013

        internal void AdvancePhaseProgress(float deltaTime) => PhaseProgress += deltaTime;

        internal void EnterPhase(TransportCyclePhase phase)
        {
            Phase = phase;
            PhaseProgress = 0f;
        }

        internal void SetCarriedQuantity(float quantity) => CarriedQuantity = Math.Max(0f, quantity);

        // Réservé au rechargement d'une sauvegarde (FR-031) : impose directement un état déjà
        // connu sans rejouer les transitions normales du cycle.
        public void RestoreState(TransportCyclePhase phase, float phaseProgress, float carriedQuantity)
        {
            Phase = phase;
            PhaseProgress = phaseProgress;
            CarriedQuantity = carriedQuantity;
        }
    }
}
