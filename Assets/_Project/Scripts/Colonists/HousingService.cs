using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Colonists
{
    public interface IHousingService
    {
        // Formation automatique d'un couple (pas de sélection manuelle par le joueur) : si le
        // logement n'a encore aucun adulte, assigne simultanément les deux premiers colons adultes
        // disponibles (sans logement, non-enfants) de sexe opposé. Exclusivité : tant qu'un couple
        // occupe le logement, aucun autre adulte n'y est jamais assigné (le logement n'a jamais
        // exactement un seul adulte dans ce modèle).
        bool TryFormCouple(Guid housingBuildingId, IEnumerable<Colonist> allColonists);

        void RemoveResident(Colonist colonist);
    }

    public sealed class HousingService : IHousingService
    {
        public bool TryFormCouple(Guid housingBuildingId, IEnumerable<Colonist> allColonists)
        {
            var colonists = allColonists as IReadOnlyList<Colonist> ?? allColonists.ToList();

            var hasAdultAlready = colonists.Any(c => c.HousingId == housingBuildingId && !c.IsChild);
            if (hasAdultAlready) return false; // couple déjà formé : exclusivité

            var male = colonists.FirstOrDefault(c => !c.IsChild && c.HousingId == null && c.Gender == Gender.Male);
            var female = colonists.FirstOrDefault(c => !c.IsChild && c.HousingId == null && c.Gender == Gender.Female);
            if (male == null || female == null) return false; // aucune paire disponible pour l'instant

            male.HousingId = housingBuildingId;
            female.HousingId = housingBuildingId;
            return true;
        }

        public void RemoveResident(Colonist colonist)
        {
            colonist.HousingId = null;
        }
    }
}
