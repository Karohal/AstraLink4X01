using System;
using System.Collections.Generic;
using System.Linq;
using Game.Building;

namespace Game.Colonists
{
    public sealed class HousingBirthResult
    {
        public Colonist Newborn; // null si aucune naissance ce tick
        public readonly List<Colonist> GrownUpChildren = new List<Colonist>(); // ont atteint la majorité ce tick
    }

    public interface IHousingBirthService
    {
        // FR-047 : fait progresser la cohabitation d'un couple (remise à zéro si le couple ne
        // l'occupe plus à deux), tente une naissance après un an de cohabitation continue —
        // tentatives répétées dans le temps (BuildingDefinition.BirthAttemptIntervalSeconds/
        // BirthAttemptSuccessChance) plutôt qu'un essai unique, pour disperser les naissances —
        // et fait quitter le logement aux enfants ayant atteint leur majorité (18 ans), qui
        // deviennent des colons adultes disponibles. Indépendant des ressources/de la trésorerie de
        // la colonie (FR-049). Ne fait PAS vieillir les colons lui-même : Colonist.AgeSeconds est
        // incrémenté une seule fois pour tous (enfants et adultes) par l'appelant (Phase1GameController),
        // ce service se contente de lire l'âge déjà à jour pour détecter la majorité.
        HousingBirthResult Tick(Guid housingBuildingId, BuildingDefinition definition, HousingCohabitation cohabitation,
            List<Colonist> residents, IReadOnlyList<Colonist> allColonists, EthnicityDefinition ethnicity,
            float deltaSimTime, float secondsPerGameYear);
    }

    public sealed class HousingBirthService : IHousingBirthService
    {
        // FR-048 : au-delà de 10% d'écart, favorise le genre sous-représenté dans la colonie.
        private const float GenderRebalanceThreshold = 0.1f;

        // "Rendement initial de 25%, cohérent avec le plafond d'un colon sans éducation formelle" :
        // valeur de contenu/équilibrage, appliquée aux mêmes métiers que les colons de départ.
        private const float YoungAdultInitialSkillValue = 0.25f;

        private readonly IColonistIdentityService _identityService;
        private readonly Random _random;

        public HousingBirthService(IColonistIdentityService identityService, Random random = null)
        {
            _identityService = identityService;
            _random = random ?? new Random();
        }

        public HousingBirthResult Tick(Guid housingBuildingId, BuildingDefinition definition, HousingCohabitation cohabitation,
            List<Colonist> residents, IReadOnlyList<Colonist> allColonists, EthnicityDefinition ethnicity,
            float deltaSimTime, float secondsPerGameYear)
        {
            var result = new HousingBirthResult();

            var adults = residents.Where(c => !c.IsChild).ToList();
            var isCouple = adults.Count == 2 && adults[0].Gender != adults[1].Gender;

            if (isCouple)
            {
                cohabitation.AdvanceContinuous(deltaSimTime);

                var oneYear = secondsPerGameYear;
                var childCount = residents.Count(c => c.IsChild);
                var hasRoomForChild = childCount < definition.MaxChildResidents;

                if (cohabitation.ContinuousDuration >= oneYear && hasRoomForChild)
                {
                    cohabitation.AdvanceSinceLastBirthAttempt(deltaSimTime);

                    if (cohabitation.TimeSinceLastBirthAttempt >= definition.BirthAttemptIntervalSeconds)
                    {
                        cohabitation.ResetSinceLastBirthAttempt();

                        if (_random.NextDouble() < definition.BirthAttemptSuccessChance)
                        {
                            var gender = PickNewbornGender(allColonists);
                            var newborn = _identityService.CreateColonist(ethnicity, gender);
                            newborn.IsChild = true;
                            newborn.AgeSeconds = 0f;
                            newborn.HousingId = housingBuildingId;
                            result.Newborn = newborn;
                        }
                    }
                }
            }
            else
            {
                cohabitation.ResetContinuous(); // pas (ou plus) de couple à deux : le décompte repart de zéro (FR-047)
            }

            foreach (var child in residents.Where(c => c.IsChild).ToList())
            {
                if (child.AgeSeconds < 18f * secondsPerGameYear) continue;

                child.IsChild = false;
                child.HousingId = null; // quitte automatiquement le logement à sa majorité
                InitializeYoungAdultSkills(child);
                result.GrownUpChildren.Add(child);
            }

            return result;
        }

        private static void InitializeYoungAdultSkills(Colonist colonist)
        {
            colonist.GetOrCreateSkill(JobDefinition.ResearcherJobId).Value = YoungAdultInitialSkillValue;
            colonist.GetOrCreateSkill(JobDefinition.WoodcutterJobId).Value = YoungAdultInitialSkillValue;
            colonist.GetOrCreateSkill(JobDefinition.MinerJobId).Value = YoungAdultInitialSkillValue;
            colonist.GetOrCreateSkill(JobDefinition.MasonJobId).Value = YoungAdultInitialSkillValue;
        }

        private Gender PickNewbornGender(IReadOnlyList<Colonist> allColonists)
        {
            var maleCount = allColonists.Count(c => c.Gender == Gender.Male);
            var femaleCount = allColonists.Count(c => c.Gender == Gender.Female);
            var total = maleCount + femaleCount;
            if (total == 0) return _random.NextDouble() < 0.5 ? Gender.Male : Gender.Female;

            var maleRatio = (float)maleCount / total;
            if (maleRatio - 0.5f > GenderRebalanceThreshold) return Gender.Female;
            if (0.5f - maleRatio > GenderRebalanceThreshold) return Gender.Male;

            return _random.NextDouble() < 0.5 ? Gender.Male : Gender.Female;
        }
    }
}
