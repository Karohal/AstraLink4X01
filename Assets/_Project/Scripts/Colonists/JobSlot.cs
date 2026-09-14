using System;

namespace Game.Colonists
{
    // Concept non détaillé explicitement dans data-model.md (« PosteEmploi[] ») mais nécessaire
    // pour distinguer un poste précis (ex: le poste de chercheur d'un bâtiment donné) de la simple
    // chaîne de caractères JobDefinition.Id, afin qu'un colon puisse cibler une Affectation.
    public sealed class JobSlot
    {
        public Guid Id { get; }
        public string JobId { get; }
        public Guid BuildingId { get; }

        public JobSlot(Guid id, string jobId, Guid buildingId)
        {
            Id = id;
            JobId = jobId;
            BuildingId = buildingId;
        }
    }
}
