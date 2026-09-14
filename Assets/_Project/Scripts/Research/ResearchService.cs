using System;

namespace Game.Research
{
    public sealed class Technology
    {
        public string Id { get; }
        public string TargetResourceId { get; }
        public float ProgressRequired { get; }
        public float ProgressCurrent { get; private set; }

        public Technology(string id, string targetResourceId, float progressRequired)
        {
            Id = id;
            TargetResourceId = targetResourceId;
            ProgressRequired = progressRequired;
        }

        public bool IsUnlocked => ProgressCurrent >= ProgressRequired;

        public void AddProgress(float amount)
        {
            if (amount <= 0f || IsUnlocked) return;
            ProgressCurrent = Math.Min(ProgressRequired, ProgressCurrent + amount);
        }

        // Réservé au rechargement d'une sauvegarde (FR-031).
        public void RestoreProgress(float progress)
        {
            ProgressCurrent = progress;
        }
    }

    public interface IResearchService
    {
        void ContributeProgress(Technology technology, float amount); // FR-008
        bool IsUnlocked(Technology technology);
    }

    public sealed class ResearchService : IResearchService
    {
        public void ContributeProgress(Technology technology, float amount) => technology?.AddProgress(amount);

        public bool IsUnlocked(Technology technology) => technology != null && technology.IsUnlocked;
    }
}
