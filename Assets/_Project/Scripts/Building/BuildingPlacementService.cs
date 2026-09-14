using System;
using System.Collections.Generic;
using Game.Economy;
using Game.Procedural;

namespace Game.Building
{
    public interface IBuildingPlacementService
    {
        bool CanBuild(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory, out List<string> missingResources);
        BuildingInstance Build(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory);

        // Recyclage : détruit le bâtiment (libère la zone) et rembourse une fraction de son coût
        // initial (BuildingDefinition.RecycleRefundRatio, valeur d'équilibrage du catalogue de
        // contenu). Le cas d'usage visé est un extracteur/pompe dont le gisement est épuisé, mais
        // la règle n'est pas restreinte à ce seul cas.
        void Recycle(Planet planet, BuildingInstance building, BuildingDefinition definition, Inventory inventory);

        // Annule un chantier pas encore commencé (ProgresChantier == 0, aucun colon n'a encore
        // travaillé dessus) : contrairement à Recycle, remboursement intégral du coût puisque rien
        // n'a réellement été construit. Retourne false sans rien faire si le chantier a déjà
        // progressé (ou si le bâtiment est déjà opérationnel) — passer alors par Recycle.
        bool TryCancelConstruction(Planet planet, BuildingInstance building, BuildingDefinition definition, Inventory inventory);
    }

    public sealed class BuildingPlacementService : IBuildingPlacementService
    {
        public bool CanBuild(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory, out List<string> missingResources)
        {
            missingResources = new List<string>();

            if (planet == null || !planet.TryGetZone(x, y, out var zone) || !zone.IsRevealed)
            {
                missingResources.Add("invalid-zone");
                return false;
            }

            // Cas particulier de la pompe (FR-044) : seul type de bâtiment autorisé sur une case
            // d'eau, sinon la règle générale Zone.EstConstructible s'applique.
            var canPlaceOnTerrain = zone.IsBuildable || (definition.CanBuildOnWater && zone.Terrain == TerrainType.Water);
            if (!canPlaceOnTerrain)
            {
                missingResources.Add("invalid-zone");
                return false;
            }

            if (zone.BuildingId.HasValue)
            {
                missingResources.Add("zone-occupied");
                return false;
            }

            foreach (var cost in definition.Cost)
            {
                if (!inventory.HasAtLeast(cost.ResourceId, cost.Quantity))
                    missingResources.Add(cost.ResourceId); // FR-006
            }

            return missingResources.Count == 0;
        }

        // FR-005/FR-042 : déduit le coût et démarre le bâtiment en chantier (pas encore opérationnel).
        public BuildingInstance Build(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory)
        {
            if (!CanBuild(planet, definition, x, y, inventory, out var missing))
                throw new InvalidOperationException($"Cannot build {definition.Id}: {string.Join(",", missing)}");

            foreach (var cost in definition.Cost)
                inventory.TryRemove(cost.ResourceId, cost.Quantity);

            planet.TryGetZone(x, y, out var zone);
            var building = new BuildingInstance(Guid.NewGuid(), definition.Id, x, y);
            zone.BuildingId = building.Id;

            return building;
        }

        public void Recycle(Planet planet, BuildingInstance building, BuildingDefinition definition, Inventory inventory)
        {
            if (planet == null) throw new ArgumentNullException(nameof(planet));
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            foreach (var cost in definition.Cost)
            {
                var refund = cost.Quantity * definition.RecycleRefundRatio;
                if (refund > 0f) inventory.TryAdd(cost.ResourceId, refund);
            }

            if (planet.TryGetZone(building.X, building.Y, out var zone) && zone.BuildingId == building.Id)
                zone.BuildingId = null;
        }

        public bool TryCancelConstruction(Planet planet, BuildingInstance building, BuildingDefinition definition, Inventory inventory)
        {
            if (planet == null) throw new ArgumentNullException(nameof(planet));
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (building.State != BuildingState.UnderConstruction || building.ConstructionProgress > 0f)
                return false; // chantier déjà commencé/terminé : remboursement partiel via Recycle

            foreach (var cost in definition.Cost)
                inventory.TryAdd(cost.ResourceId, cost.Quantity); // remboursement intégral : rien n'a été construit

            if (planet.TryGetZone(building.X, building.Y, out var zone) && zone.BuildingId == building.Id)
                zone.BuildingId = null;

            return true;
        }
    }
}
