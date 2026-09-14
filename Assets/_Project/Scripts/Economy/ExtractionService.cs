using System;
using Game.Building;
using Game.Procedural;
using Game.Research;

namespace Game.Economy
{
    public interface IExtractionService
    {
        bool CanBuildExtractor(Deposit deposit, Technology technology); // FR-009
        void BeginExtraction(Deposit deposit); // appelé quand l'extracteur devient opérationnel

        // FR-010/FR-011 : extrait dans le buffer de sortie du site (FR-012, US4) plutôt que dans un
        // stock global directement — la ressource n'atteint le stockage que via un transport assigné.
        // yieldMultiplier : rendement global du bâtiment (0..1+, cf. rendement compétence/santé des
        // colons assignés) appliqué au taux de base ; 1 = rendement plein par défaut.
        void Tick(Deposit deposit, BuildingInstance extractor, Inventory outputBuffer, float deltaSimTime, float yieldMultiplier = 1f);
    }

    public sealed class ExtractionService : IExtractionService
    {
        private const float ExtractionRatePerSecond = 5f; // valeur d'équilibrage provisoire

        public bool CanBuildExtractor(Deposit deposit, Technology technology)
        {
            return deposit != null && technology != null && technology.IsUnlocked &&
                   deposit.State == DepositState.TechnologyLocked;
        }

        public void BeginExtraction(Deposit deposit)
        {
            deposit.UnlockTechnology();
            deposit.StartExtraction();
        }

        public void Tick(Deposit deposit, BuildingInstance extractor, Inventory outputBuffer, float deltaSimTime, float yieldMultiplier = 1f)
        {
            if (deposit == null || extractor == null || !extractor.IsOperational) return;
            if (deposit.State != DepositState.Extracting) return;
            if (yieldMultiplier <= 0f) return; // aucun travailleur (ou rendement nul) -> aucune extraction

            var amount = Math.Min(ExtractionRatePerSecond * yieldMultiplier * deltaSimTime, deposit.RemainingQuantity);
            if (amount <= 0f) return;

            deposit.Extract(amount); // FR-011 : Extract() gère l'épuisement
            outputBuffer.TryAdd(deposit.ResourceId, amount); // FR-012 : s'accumule au site de production
        }
    }
}
