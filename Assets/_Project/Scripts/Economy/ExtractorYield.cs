using System.Collections.Generic;
using System.Linq;
using Game.Colonists;

namespace Game.Economy
{
    // Rendement d'un extracteur/pompe selon les colons qui y sont affectés : rendement individuel
    // = (compétence + santé) / 2 (deux valeurs 0..1) ; rendement du bâtiment = somme des rendements
    // individuels divisée par le nombre max de postes (un poste vide compte pour 0), jamais par le
    // nombre de colons réellement présents — un bâtiment sous-staffé sous-performe donc même si les
    // colons présents sont excellents. S'ajoute au système de compétence/santé (US6 à venir) sans
    // le remplacer : ComputeIndividualYield est la même formule que le futur ISkillProgressionService.
    public static class ExtractorYield
    {
        public const int MaxWorkerSlots = 5;

        public static float ComputeIndividualYield(Colonist colonist, string jobId)
        {
            if (colonist == null) return 0f;
            var skill = colonist.GetOrCreateSkill(jobId).Value;
            return (skill + colonist.Health) / 2f;
        }

        public static float ComputeBuildingYield(IEnumerable<Colonist> assignedWorkers, string jobId)
        {
            var sum = assignedWorkers?.Sum(c => ComputeIndividualYield(c, jobId)) ?? 0f;
            return sum / MaxWorkerSlots;
        }
    }
}
