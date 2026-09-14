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
    }

    public sealed class BuildingPlacementService : IBuildingPlacementService
    {
        public bool CanBuild(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory, out List<string> missingResources)
        {
            missingResources = new List<string>();

            if (planet == null || !planet.TryGetZone(x, y, out var zone) || !zone.IsBuildable || !zone.IsRevealed)
            {
                missingResources.Add("invalid-zone");
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
    }
}
