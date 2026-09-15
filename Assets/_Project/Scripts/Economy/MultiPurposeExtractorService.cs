using System;
using System.Collections.Generic;
using System.Linq;
using Game.Building;
using Game.Procedural;

namespace Game.Economy
{
    public interface IMultiPurposeExtractorService
    {
        // FR-051 : uniquement un gisement révélé dont la ressource fait partie de allowedResourceIds
        // (eau/pierre/bois, fournis par l'appelant plutôt que codés en dur ici — ce sont des
        // ResourceDefinition de contenu, pas une liste fixe au niveau du moteur). otherExtractors
        // évite d'empiler plusieurs exemplaires sur le même gisement (FR-051, bon sens de contenu :
        // rien dans la spec ne l'exige explicitement, mais rien ne prévoit non plus une extraction
        // cumulée à ce niveau).
        bool CanPlaceOn(Planet planet, IReadOnlyCollection<string> allowedResourceIds,
            IEnumerable<MultiPurposeExtractor> otherExtractors, int x, int y, out string reason);

        void PlaceOn(MultiPurposeExtractor extractor, Planet planet, IReadOnlyCollection<string> allowedResourceIds,
            IEnumerable<MultiPurposeExtractor> otherExtractors, int x, int y);

        // FR-052 : même opération que PlaceOn (pas de distinction placement initial / déplacement),
        // exposée séparément pour la lisibilité côté appelant (bouton "Déplacer" vs "Placer").
        void MoveTo(MultiPurposeExtractor extractor, Planet planet, IReadOnlyCollection<string> allowedResourceIds,
            IEnumerable<MultiPurposeExtractor> otherExtractors, int x, int y);

        // FR-052 : extrait directement dans l'inventaire fourni par l'appelant. Contrairement à
        // ExtractionService (FR-012, extraction vers un buffer de site nécessitant un transport),
        // l'Extracteur multifonction est un outil de secours rattaché au Module de survie : sa
        // production rejoint directement le stock global du joueur (Warehouse), sans étape de
        // transport dédiée — simplification assumée, non requise ni interdite par FR-051/FR-052.
        void Tick(MultiPurposeExtractor extractor, Planet planet, Inventory targetInventory, float deltaSimTime);
    }

    public sealed class MultiPurposeExtractorService : IMultiPurposeExtractorService
    {
        // Rendement volontairement inférieur à ExtractionService.ExtractionRatePerSecond (5f) :
        // outil de secours portatif, moins efficace qu'un extracteur/une pompe fixe dédiée. Valeur
        // d'équilibrage provisoire.
        private const float ExtractionRatePerSecond = 2f;

        public bool CanPlaceOn(Planet planet, IReadOnlyCollection<string> allowedResourceIds,
            IEnumerable<MultiPurposeExtractor> otherExtractors, int x, int y, out string reason)
        {
            reason = null;

            if (planet == null || !planet.TryGetZone(x, y, out var zone) || !zone.IsRevealed)
            {
                reason = "invalid-zone";
                return false;
            }

            if (zone.Deposit == null || allowedResourceIds == null || !allowedResourceIds.Contains(zone.Deposit.ResourceId))
            {
                reason = "incompatible-deposit"; // FR-051 : uniquement eau/pierre/bois
                return false;
            }

            if (otherExtractors != null && otherExtractors.Any(e => e.IsPlaced && e.TargetX == x && e.TargetY == y))
            {
                reason = "deposit-occupied";
                return false;
            }

            return true;
        }

        public void PlaceOn(MultiPurposeExtractor extractor, Planet planet, IReadOnlyCollection<string> allowedResourceIds,
            IEnumerable<MultiPurposeExtractor> otherExtractors, int x, int y)
        {
            if (extractor == null) throw new ArgumentNullException(nameof(extractor));
            if (!CanPlaceOn(planet, allowedResourceIds, otherExtractors, x, y, out var reason))
                throw new InvalidOperationException($"Cannot place multi-purpose extractor: {reason}");

            extractor.PlaceOn(x, y);
        }

        public void MoveTo(MultiPurposeExtractor extractor, Planet planet, IReadOnlyCollection<string> allowedResourceIds,
            IEnumerable<MultiPurposeExtractor> otherExtractors, int x, int y)
            => PlaceOn(extractor, planet, allowedResourceIds, otherExtractors, x, y);

        public void Tick(MultiPurposeExtractor extractor, Planet planet, Inventory targetInventory, float deltaSimTime)
        {
            if (extractor == null || !extractor.IsPlaced || targetInventory == null) return;
            if (!planet.TryGetZone(extractor.TargetX.Value, extractor.TargetY.Value, out var zone) || zone.Deposit == null) return;

            var deposit = zone.Deposit;
            if (deposit.State == DepositState.TechnologyLocked) deposit.UnlockTechnology();
            if (deposit.State == DepositState.ReadyForExtractor) deposit.StartExtraction();
            if (deposit.State != DepositState.Extracting) return;

            var amount = Math.Min(ExtractionRatePerSecond * deltaSimTime, deposit.RemainingQuantity);
            if (amount <= 0f) return;

            deposit.Extract(amount);
            targetInventory.TryAdd(deposit.ResourceId, amount);
        }
    }
}
