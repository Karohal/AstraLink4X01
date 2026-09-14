using System;
using Game.Building;
using Game.Colonists;
using Game.Economy;
using Game.Logistics;
using Game.Procedural;
using Game.Research;

namespace Game.Core
{
    // Regroupe les références de contenu (ScriptableObjects) nécessaires à Phase1GameController,
    // pour éviter de dupliquer une longue liste de paramètres entre les vues qui l'utilisent.
    // La scène de debug historique (Phase1TestHarness/Phase1_MVP) reste autonome et inchangée ;
    // ce contenu est partagé par les nouvelles vues (Phase1GameView notamment).
    [Serializable]
    public sealed class Phase1GameContent
    {
        public BuildingDefinition ShelterDefinition;
        public BuildingDefinition StorageDefinition;
        public BuildingDefinition CisternDefinition;
        public BuildingDefinition ExtractorDefinition;
        public BuildingDefinition PumpDefinition;
        public BuildingDefinition HousingDefinition; // "Abri basique" — premier type de logement (FR-047)

        public ResourceDefinition MaterialsResource;
        public ResourceDefinition WoodResource;
        public ResourceDefinition StoneResource;
        public ResourceDefinition WaterResource;

        public TechnologyDefinition WoodTechnologyDefinition;
        public TechnologyDefinition StoneTechnologyDefinition;
        public TechnologyDefinition WaterTechnologyDefinition;

        public JobDefinition ResearcherJobDefinition;
        public EthnicityDefinition StartingEthnicity;

        public TransporterDefinition ColonistTransporterDefinition;
        public TerrainSpeedCatalog TerrainSpeedCatalog;

        public bool IsComplete()
        {
            return ShelterDefinition != null && StorageDefinition != null && CisternDefinition != null &&
                   ExtractorDefinition != null && PumpDefinition != null && HousingDefinition != null &&
                   MaterialsResource != null && WoodResource != null && StoneResource != null && WaterResource != null &&
                   WoodTechnologyDefinition != null && StoneTechnologyDefinition != null && WaterTechnologyDefinition != null &&
                   ResearcherJobDefinition != null && StartingEthnicity != null &&
                   ColonistTransporterDefinition != null && TerrainSpeedCatalog != null;
        }
    }
}
