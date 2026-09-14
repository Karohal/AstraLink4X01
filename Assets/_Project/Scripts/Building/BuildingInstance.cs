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

    public sealed class BuildingInstance
    {
        public Guid Id { get; }
        public string DefinitionId { get; }
        public int X { get; }
        public int Y { get; }
        public bool IsStartingShelter { get; }

        public BuildingState State { get; private set; }
        public float ConstructionProgress { get; private set; }
        public TransformationState TransformationState { get; set; } = TransformationState.Operational;

        // Nom choisi par le joueur (fiche du bâtiment) ; null/vide tant qu'il n'a pas été renommé,
        // auquel cas l'affichage retombe sur BuildingDefinition.DisplayName (partagé par tous les
        // bâtiments du même type).
        public string CustomName { get; private set; }

        public BuildingInstance(Guid id, string definitionId, int x, int y, bool isStartingShelter = false)
        {
            Id = id;
            DefinitionId = definitionId;
            X = x;
            Y = y;
            IsStartingShelter = isStartingShelter;
            State = isStartingShelter ? BuildingState.Operational : BuildingState.UnderConstruction;
            ConstructionProgress = isStartingShelter ? 1f : 0f;
        }

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
