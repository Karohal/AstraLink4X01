using System;
using System.Collections.Generic;
using Game.Building;
using Game.Colonists;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Colonists
{
    public class HousingBirthServiceTests
    {
        private const float OneYearSeconds = 60f; // valeur de test arbitraire, cf. "secondsPerGameYear"

        private static EthnicityDefinition CreateEthnicity()
        {
            var ethnicity = ScriptableObject.CreateInstance<EthnicityDefinition>();
            ethnicity.Initialize("human", new[] { "Alex" }, new[] { "Aria" }, new[] { "Voss" });
            return ethnicity;
        }

        private static BuildingDefinition CreateHousingDefinition(float birthIntervalSeconds, float birthSuccessChance, int maxChildren = 2)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            definition.Initialize("basic-shelter", "Abri basique", new ResourceAmount[0], 10f,
                isHousing: true, maxAdultResidents: 2, maxChildResidents: maxChildren,
                birthAttemptIntervalSeconds: birthIntervalSeconds, birthAttemptSuccessChance: birthSuccessChance);
            return definition;
        }

        private static (Colonist husband, Colonist wife) CreateCouple(Guid housingId)
        {
            var husband = new Colonist(Guid.NewGuid(), "Husband", Gender.Male, "human") { HousingId = housingId };
            var wife = new Colonist(Guid.NewGuid(), "Wife", Gender.Female, "human") { HousingId = housingId };
            return (husband, wife);
        }

        [Test]
        public void Tick_CoupleUnderOneYear_NoBirthAttempt()
        {
            var housingId = Guid.NewGuid();
            var (husband, wife) = CreateCouple(housingId);
            var residents = new List<Colonist> { husband, wife };
            var definition = CreateHousingDefinition(birthIntervalSeconds: 0f, birthSuccessChance: 1f);
            var cohabitation = new HousingCohabitation();

            var service = new HousingBirthService(new ColonistIdentityService());
            var result = service.Tick(housingId, definition, cohabitation, residents, residents, CreateEthnicity(),
                deltaSimTime: OneYearSeconds - 1f, secondsPerGameYear: OneYearSeconds);

            Assert.IsNull(result.Newborn); // moins d'un an de cohabitation continue (FR-047)
        }

        [Test]
        public void Tick_CoupleOverOneYear_SuccessfulAttempt_CreatesNewbornChildInSameHousing()
        {
            var housingId = Guid.NewGuid();
            var (husband, wife) = CreateCouple(housingId);
            var residents = new List<Colonist> { husband, wife };
            var definition = CreateHousingDefinition(birthIntervalSeconds: 0f, birthSuccessChance: 1f); // succès garanti
            var cohabitation = new HousingCohabitation();

            var service = new HousingBirthService(new ColonistIdentityService());
            // Un premier tick pour dépasser le seuil d'un an, la tentative a lieu dans le même tick.
            var result = service.Tick(housingId, definition, cohabitation, residents, residents, CreateEthnicity(),
                deltaSimTime: OneYearSeconds + 1f, secondsPerGameYear: OneYearSeconds);

            Assert.IsNotNull(result.Newborn);
            Assert.IsTrue(result.Newborn.IsChild);
            Assert.AreEqual(housingId, result.Newborn.HousingId);
            Assert.AreEqual(0f, result.Newborn.AgeSeconds);
        }

        [Test]
        public void Tick_FailedAttempt_NoNewborn_ButKeepsCohabitationContinuous()
        {
            var housingId = Guid.NewGuid();
            var (husband, wife) = CreateCouple(housingId);
            var residents = new List<Colonist> { husband, wife };
            var definition = CreateHousingDefinition(birthIntervalSeconds: 0f, birthSuccessChance: 0f); // échec garanti
            var cohabitation = new HousingCohabitation();

            var service = new HousingBirthService(new ColonistIdentityService());
            var result = service.Tick(housingId, definition, cohabitation, residents, residents, CreateEthnicity(),
                deltaSimTime: OneYearSeconds + 1f, secondsPerGameYear: OneYearSeconds);

            Assert.IsNull(result.Newborn);
            Assert.Greater(cohabitation.ContinuousDuration, 0f); // pas un échec définitif : nouvelle tentative plus tard (pas de reset)
        }

        [Test]
        public void Tick_NotACouple_ResetsContinuousDuration()
        {
            var housingId = Guid.NewGuid();
            var husband = new Colonist(Guid.NewGuid(), "Husband", Gender.Male, "human") { HousingId = housingId };
            var residents = new List<Colonist> { husband }; // seul, pas de couple
            var definition = CreateHousingDefinition(birthIntervalSeconds: 0f, birthSuccessChance: 1f);
            var cohabitation = new HousingCohabitation();

            var service = new HousingBirthService(new ColonistIdentityService());
            var result = service.Tick(housingId, definition, cohabitation, residents, residents, CreateEthnicity(),
                deltaSimTime: OneYearSeconds + 1f, secondsPerGameYear: OneYearSeconds);

            Assert.IsNull(result.Newborn);
            Assert.AreEqual(0f, cohabitation.ContinuousDuration);
        }

        [Test]
        public void Tick_ChildReachingMajority_LeavesHousing_BecomesAdultWithInitialSkill()
        {
            var housingId = Guid.NewGuid();
            var (husband, wife) = CreateCouple(housingId);
            var child = new Colonist(Guid.NewGuid(), "Child", Gender.Male, "human")
            {
                HousingId = housingId,
                IsChild = true,
                // Le service ne fait plus vieillir les colons lui-même (cf. Phase1GameController) :
                // on simule ici un âge déjà porté au-delà de la majorité par l'appelant.
                AgeSeconds = 18f * OneYearSeconds + 0.5f
            };
            var residents = new List<Colonist> { husband, wife, child };
            var definition = CreateHousingDefinition(birthIntervalSeconds: 999f, birthSuccessChance: 0f);
            var cohabitation = new HousingCohabitation();

            var service = new HousingBirthService(new ColonistIdentityService());
            var result = service.Tick(housingId, definition, cohabitation, residents, residents, CreateEthnicity(),
                deltaSimTime: 1f, secondsPerGameYear: OneYearSeconds);

            Assert.AreEqual(1, result.GrownUpChildren.Count);
            Assert.IsFalse(child.IsChild);
            Assert.IsNull(child.HousingId); // quitte automatiquement le logement à sa majorité
            Assert.AreEqual(0.25f, child.Skills[Game.Colonists.JobDefinition.MinerJobId].Value, 0.001f);
            Assert.AreEqual(0.25f, child.Skills[Game.Colonists.JobDefinition.WoodcutterJobId].Value, 0.001f);
            Assert.AreEqual(0.25f, child.Skills[Game.Colonists.JobDefinition.MasonJobId].Value, 0.001f);
        }
    }
}
