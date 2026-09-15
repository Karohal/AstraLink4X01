using System;
using System.Collections.Generic;
using Game.Economy;
using Game.Procedural;

namespace Game.Building
{
    public interface IBuildingPlacementService
    {
        bool CanBuild(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory, out List<string> missingResources);

        // offsetX/offsetY (0..1, défaut centré) et rotation : positionnement libre dans la case et
        // orientation choisis par le joueur avant validation (FR-054), purement cosmétiques.
        BuildingInstance Build(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory,
            float offsetX = 0.5f, float offsetY = 0.5f, BuildingRotation rotation = BuildingRotation.Deg0);

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

            if (!zone.IsBuildable)
            {
                // FR-055 : plus aucune exception liée à l'eau ici (une pompe ne se construit jamais
                // directement sur l'eau) — cf. TryResolveDepositTarget pour la règle d'adjacence.
                missingResources.Add("invalid-zone");
                return false;
            }

            if (!TryResolveDepositTarget(planet, definition, zone, x, y, out _, out _, out var depositReason))
            {
                missingResources.Add(depositReason);
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
        public BuildingInstance Build(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory,
            float offsetX = 0.5f, float offsetY = 0.5f, BuildingRotation rotation = BuildingRotation.Deg0)
        {
            if (!CanBuild(planet, definition, x, y, inventory, out var missing))
                throw new InvalidOperationException($"Cannot build {definition.Id}: {string.Join(",", missing)}");

            foreach (var cost in definition.Cost)
                inventory.TryRemove(cost.ResourceId, cost.Quantity);

            planet.TryGetZone(x, y, out var zone);
            TryResolveDepositTarget(planet, definition, zone, x, y, out var depositX, out var depositY, out _);
            var building = new BuildingInstance(Guid.NewGuid(), definition.Id, x, y,
                depositX: depositX, depositY: depositY, offsetX: offsetX, offsetY: offsetY, rotation: rotation);
            zone.BuildingId = building.Id;

            return building;
        }

        // FR-055 : pour un bâtiment classique, le gisement exploité est toujours celui de sa propre
        // case (aucune contrainte ici). Pour un bâtiment ExtraitEauAdjacente (pompe) : jamais
        // directement sur une case d'eau ; accepté soit sur une case à gisement nappe phréatique
        // (exploite son propre gisement), soit sur une case constructible adjacente à une case
        // d'eau (exploite alors le gisement de cette case voisine, renvoyé via depositX/depositY).
        private static bool TryResolveDepositTarget(Planet planet, BuildingDefinition definition, Zone zone, int x, int y,
            out int depositX, out int depositY, out string reason)
        {
            depositX = x;
            depositY = y;
            reason = null;

            if (!definition.ExtractsAdjacentWater) return true;

            if (zone.Terrain == TerrainType.Water)
            {
                reason = "cannot-build-on-water";
                return false;
            }

            if (zone.Deposit != null) return true; // nappe phréatique : gisement sur la case cible elle-même

            if (TryFindAdjacentWaterZone(planet, x, y, out var adjacentX, out var adjacentY))
            {
                depositX = adjacentX;
                depositY = adjacentY;
                return true;
            }

            reason = "no-adjacent-water";
            return false;
        }

        // 4-connexité (Nord/Sud/Est/Ouest) : aucun pathfinding/diagonale requis pour cette
        // vérification d'adjacence simple en Phase 1.
        private static readonly (int Dx, int Dy)[] CardinalOffsets = { (0, 1), (0, -1), (1, 0), (-1, 0) };

        private static bool TryFindAdjacentWaterZone(Planet planet, int x, int y, out int foundX, out int foundY)
        {
            foreach (var offset in CardinalOffsets)
            {
                if (planet.TryGetZone(x + offset.Dx, y + offset.Dy, out var neighbor) &&
                    neighbor.Terrain == TerrainType.Water && neighbor.Deposit != null)
                {
                    foundX = x + offset.Dx;
                    foundY = y + offset.Dy;
                    return true;
                }
            }

            foundX = 0;
            foundY = 0;
            return false;
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
