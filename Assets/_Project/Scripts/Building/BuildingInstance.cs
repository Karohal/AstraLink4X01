using System;

namespace Game.Building
{
    // Nommé "BuildingInstance" (et non "Building") pour éviter toute collision entre le nom du
    // type et celui du namespace Game.Building lorsqu'il est référencé depuis un autre module
    // (Game.Economy, Game.Procedural, ...) : en C#, un import ou un alias ne suffit pas à lever
    // cette ambiguïté, seul un nom de type distinct le fait de façon fiable.
    public enum BuildingState
    {
        UnderConstruction,
        Operational
    }

    public enum TransformationState
    {
        Operational,
        Paused
    }

    // Orientation choisie par le joueur avant validation du placement (FR-054) : purement
    // cosmétique en Phase 1, aucune règle de jeu n'en dépend.
    public enum BuildingRotation
    {
        Deg0 = 0,
        Deg90 = 90,
        Deg180 = 180,
        Deg270 = 270
    }

    public sealed class BuildingInstance
    {
        public Guid Id { get; }
        public string DefinitionId { get; }
        public int X { get; }
        public int Y { get; }
        public bool IsStartingShelter { get; }

        // Coordonnées de la case portant le gisement réellement exploité par ce bâtiment (FR-055).
        // Identiques à (X, Y) pour tout bâtiment classique (dont un extracteur fixe standard) ;
        // diffèrent uniquement pour une pompe adjacente à l'eau, qui exploite le gisement d'une
        // case voisine plutôt que le sien — cf. BuildingPlacementService, seul point qui les fixe.
        public int DepositX { get; }
        public int DepositY { get; }

        // Positionnement libre dans la case et rotation (FR-054), purement cosmétiques : ne
        // participent à aucune règle de jeu (coût, chantier, adjacence, rendement...).
        public float OffsetX { get; }
        public float OffsetY { get; }
        public BuildingRotation Rotation { get; }

        public BuildingState State { get; private set; }
        public float ConstructionProgress { get; private set; }
        public TransformationState TransformationState { get; set; } = TransformationState.Operational;

        // Nom choisi par le joueur (fiche du bâtiment) ; null/vide tant qu'il n'a pas été renommé,
        // auquel cas l'affichage retombe sur BuildingDefinition.DisplayName (partagé par tous les
        // bâtiments du même type).
        public string CustomName { get; private set; }

        public BuildingInstance(Guid id, string definitionId, int x, int y, bool isStartingShelter = false,
            int? depositX = null, int? depositY = null,
            float offsetX = 0.5f, float offsetY = 0.5f, BuildingRotation rotation = BuildingRotation.Deg0)
        {
            Id = id;
            DefinitionId = definitionId;
            X = x;
            Y = y;
            IsStartingShelter = isStartingShelter;
            DepositX = depositX ?? x;
            DepositY = depositY ?? y;
            OffsetX = Clamp01(offsetX);
            OffsetY = Clamp01(offsetY);
            Rotation = rotation;
            State = isStartingShelter ? BuildingState.Operational : BuildingState.UnderConstruction;
            ConstructionProgress = isStartingShelter ? 1f : 0f;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : (value > 1f ? 1f : value);

        public bool IsOperational => State == BuildingState.Operational;

        // FR-042/FR-043 : ne progresse que si l'appelant fournit un delta (colon assigné au chantier).
        public void AdvanceConstruction(float amount, float requiredDuration)
        {
            if (State != BuildingState.UnderConstruction || amount <= 0f) return;

            ConstructionProgress += amount;
            if (ConstructionProgress >= requiredDuration)
            {
                ConstructionProgress = requiredDuration;
                State = BuildingState.Operational;
            }
        }

        // Réservé au rechargement d'une sauvegarde (FR-031) : impose directement un état déjà connu
        // sans rejouer les règles de progression normales.
        public void RestoreState(BuildingState state, float constructionProgress)
        {
            State = state;
            ConstructionProgress = constructionProgress;
        }

        public void Rename(string newName)
        {
            CustomName = newName;
        }
    }
}
