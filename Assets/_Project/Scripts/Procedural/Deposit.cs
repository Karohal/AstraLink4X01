using System;

namespace Game.Procedural
{
    // Créé ici (Foundational) plutôt qu'en US3 : Zone le référence structurellement dès T004,
    // avant même que la recherche/l'extraction (US3) n'existent. US3 (ExtractionService) pilote
    // ensuite ses transitions d'état.
    public enum DepositState
    {
        TechnologyLocked,
        ReadyForExtractor,
        Extracting,
        Depleted
    }

    public sealed class Deposit
    {
        public string ResourceId { get; }
        public float RemainingQuantity { get; private set; }
        public DepositState State { get; private set; }

        public Deposit(string resourceId, float initialQuantity)
        {
            ResourceId = resourceId;
            RemainingQuantity = initialQuantity;
            State = DepositState.TechnologyLocked;
        }

        public void UnlockTechnology()
        {
            if (State == DepositState.TechnologyLocked)
                State = DepositState.ReadyForExtractor;
        }

        public void StartExtraction()
        {
            if (State == DepositState.ReadyForExtractor)
                State = DepositState.Extracting;
        }

        public void Extract(float amount)
        {
            if (State != DepositState.Extracting || amount <= 0f) return;

            RemainingQuantity = Math.Max(0f, RemainingQuantity - amount);
            if (RemainingQuantity <= 0f)
                State = DepositState.Depleted;
        }
    }
}
