using System;
using System.Collections.Generic;
using System.Linq;
using Game.Colonists;
using Game.FogOfWar;
using Game.Procedural;

namespace Game.Building
{
    public interface IConstructionSiteService
    {
        // ProgresChantier n'avance que si un colon a une Affectation de type Construction ciblant
        // ce bâtiment ; transition EnChantier -> Operationnel une fois DureeChantier atteinte
        // (FR-042/FR-043). L'événement de dissipation du brouillard de guerre (FR-004) se
        // déclenche ici, à la fin du chantier — cf. spec.md US1 Acceptance Scenario 2.
        void Tick(BuildingInstance building, BuildingDefinition definition, Planet planet, IEnumerable<Colonist> allColonists, float deltaSimTime);

        event Action<BuildingConstructedEvent> BuildingCompleted;
    }

    public sealed class ConstructionSiteService : IConstructionSiteService
    {
        public event Action<BuildingConstructedEvent> BuildingCompleted;

        public void Tick(BuildingInstance building, BuildingDefinition definition, Planet planet, IEnumerable<Colonist> allColonists, float deltaSimTime)
        {
            if (building.State != BuildingState.UnderConstruction) return;

            var hasWorker = allColonists.Any(c =>
                c.CurrentAssignment != null &&
                c.CurrentAssignment.Type == AssignmentType.Construction &&
                c.CurrentAssignment.TargetId == building.Id);

            if (!hasWorker) return; // FR-043 : chantier à l'arrêt sans colon assigné

            building.AdvanceConstruction(deltaSimTime, definition.ConstructionDuration);

            if (building.State == BuildingState.Operational)
                BuildingCompleted?.Invoke(new BuildingConstructedEvent(planet, building.X, building.Y, definition.FogRadius));
        }
    }
}
