using System;
using Game.Economy;

namespace Game.Logistics
{
    // Paramètres du cycle pour un Tick donné : durées de trajet (dépendantes de la distance et du
    // terrain traversé, cf. Game.Procedural.TerrainRouting/TravelTimeCalculator — calculées par
    // l'appelant, ce service reste indépendant de Game.Procedural) et de chargement/déchargement
    // (valeurs d'équilibrage de contenu, amenées à diminuer avec bâtiments/technologies), plus la
    // charge maximale transportable pour la ressource concernée (masse/volume, FR peaufinage
    // US4 pt.2 précédent).
    public readonly struct TransportCycleConfig
    {
        public readonly float TravelToSourceDuration;
        public readonly float LoadingDuration;
        public readonly float TravelToDestinationDuration;
        public readonly float UnloadingDuration;
        public readonly float MaxLoadKg;

        public TransportCycleConfig(float travelToSourceDuration, float loadingDuration,
            float travelToDestinationDuration, float unloadingDuration, float maxLoadKg)
        {
            TravelToSourceDuration = travelToSourceDuration;
            LoadingDuration = loadingDuration;
            TravelToDestinationDuration = travelToDestinationDuration;
            UnloadingDuration = unloadingDuration;
            MaxLoadKg = maxLoadKg;
        }
    }

    public interface ITransportService
    {
        TransportTask Assign(Guid sourceBuildingId, Guid destinationBuildingId, string resourceId, Guid? colonistId, Guid? vehicleId); // FR-013

        // FR-012/FR-013/FR-014 + cycle complet (peaufinage US4 pt.2) : rien ne bouge sans
        // transporteur assigné ; le chargement n'a lieu qu'une fois du stock disponible à la
        // source (le transporteur patiente sinon plutôt que de faire un aller-retour à vide).
        void Tick(TransportTask task, Inventory source, Inventory destination, TransportCycleConfig config, float deltaSimTime);
    }

    public sealed class TransportService : ITransportService
    {
        private const int MaxPhaseTransitionsPerTick = 64; // garde-fou anti-boucle si une durée de phase est nulle

        public TransportTask Assign(Guid sourceBuildingId, Guid destinationBuildingId, string resourceId, Guid? colonistId, Guid? vehicleId)
        {
            return new TransportTask(Guid.NewGuid(), resourceId, sourceBuildingId, destinationBuildingId)
            {
                AssignedColonistId = colonistId,
                AssignedVehicleId = vehicleId
            };
        }

        public void Tick(TransportTask task, Inventory source, Inventory destination, TransportCycleConfig config, float deltaSimTime)
        {
            if (task == null || !task.IsAssigned || deltaSimTime <= 0f) return; // FR-014

            var remaining = deltaSimTime;
            var iterations = 0;

            while (remaining > 0f && iterations < MaxPhaseTransitionsPerTick)
            {
                iterations++;

                if (task.Phase == TransportCyclePhase.Loading && source.GetQuantity(task.ResourceId) <= 0f)
                    break; // rien à charger : le transporteur patiente au point de collecte (FR-014)

                var phaseDuration = GetPhaseDuration(task.Phase, config);
                if (phaseDuration <= 0f)
                {
                    CompletePhase(task, source, destination, config);
                    continue; // durée nulle : transition immédiate, sans consommer de temps de simulation
                }

                var timeLeftInPhase = phaseDuration - task.PhaseProgress;
                if (remaining < timeLeftInPhase)
                {
                    task.AdvancePhaseProgress(remaining);
                    remaining = 0f;
                }
                else
                {
                    remaining -= timeLeftInPhase;
                    task.AdvancePhaseProgress(timeLeftInPhase);
                    CompletePhase(task, source, destination, config);
                }
            }
        }

        private static float GetPhaseDuration(TransportCyclePhase phase, TransportCycleConfig config)
        {
            return phase switch
            {
                TransportCyclePhase.TravelingToSource => config.TravelToSourceDuration,
                TransportCyclePhase.Loading => config.LoadingDuration,
                TransportCyclePhase.TravelingToDestination => config.TravelToDestinationDuration,
                TransportCyclePhase.Unloading => config.UnloadingDuration,
                _ => 0f
            };
        }

        private static void CompletePhase(TransportTask task, Inventory source, Inventory destination, TransportCycleConfig config)
        {
            switch (task.Phase)
            {
                case TransportCyclePhase.TravelingToSource:
                    task.EnterPhase(TransportCyclePhase.Loading);
                    break;

                case TransportCyclePhase.Loading:
                    var available = source.GetQuantity(task.ResourceId);
                    var amount = Math.Min(available, config.MaxLoadKg);
                    if (amount > 0f && source.TryRemove(task.ResourceId, amount))
                        task.SetCarriedQuantity(amount); // FR-012 : la ressource quitte le site de production au chargement
                    task.EnterPhase(TransportCyclePhase.TravelingToDestination);
                    break;

                case TransportCyclePhase.TravelingToDestination:
                    task.EnterPhase(TransportCyclePhase.Unloading);
                    break;

                case TransportCyclePhase.Unloading:
                    if (task.CarriedQuantity > 0f)
                    {
                        destination.TryAdd(task.ResourceId, task.CarriedQuantity); // FR-013
                        task.SetCarriedQuantity(0f);
                    }
                    task.EnterPhase(TransportCyclePhase.TravelingToSource); // le cycle recommence automatiquement
                    break;
            }
        }
    }
}
