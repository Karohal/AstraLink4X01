using System;

namespace Game.Building
{
    // Équipement d'extraction portatif (FR-051/FR-052), distinct d'un extracteur/pompe fixe
    // (BuildingInstance) : pas de chantier, pas de coût, fourni en 5 exemplaires dans l'inventaire
    // du Module de survie. Placé/déplacé directement sur un gisement compatible (eau/pierre/bois),
    // jamais détruit/reconstruit — cf. Game.Economy.IMultiPurposeExtractorService pour les règles
    // de placement et l'extraction elle-même.
    public sealed class MultiPurposeExtractor
    {
        public Guid Id { get; }
        public int? TargetX { get; private set; }
        public int? TargetY { get; private set; }

        public MultiPurposeExtractor(Guid id)
        {
            Id = id;
        }

        public bool IsPlaced => TargetX.HasValue && TargetY.HasValue;

        // Place ou déplace l'exemplaire (FR-052) : aucune validation ici, la conformité du gisement
        // cible est du ressort de IMultiPurposeExtractorService.CanPlaceOn.
        public void PlaceOn(int x, int y)
        {
            TargetX = x;
            TargetY = y;
        }

        // Retour dans l'inventaire du Module de survie (pas d'usage direct en Phase 1 puisque le
        // joueur déplace toujours vers un nouveau gisement plutôt que de "ranger" l'exemplaire, mais
        // gardé pour la symétrie/testabilité et un futur bouton "Récupérer").
        public void ReturnToInventory()
        {
            TargetX = null;
            TargetY = null;
        }
    }
}
