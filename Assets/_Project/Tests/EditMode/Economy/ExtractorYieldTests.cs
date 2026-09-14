using System;
using System.Collections.Generic;
using Game.Colonists;
using Game.Economy;
using NUnit.Framework;

namespace Game.Tests.EditMode.Economy
{
    public class ExtractorYieldTests
    {
        private const string JobId = "extraction-worker";

        private static Colonist CreateColonist(float skillValue, float health)
        {
            var colonist = new Colonist(Guid.NewGuid(), "Worker", Gender.Male, "human") { Health = health };
            colonist.GetOrCreateSkill(JobId).Value = skillValue;
            return colonist;
        }

        [Test]
        public void ComputeIndividualYield_IsAverageOfSkillAndHealth()
        {
            var colonist = CreateColonist(skillValue: 0.6f, health: 0.8f);

            Assert.AreEqual(0.7f, ExtractorYield.ComputeIndividualYield(colonist, JobId), 0.0001f);
        }

        [Test]
        public void ComputeBuildingYield_FiveFullyStaffedWorkersAtFullYield_Is100Percent()
        {
            var workers = new List<Colonist>();
            for (var i = 0; i < 5; i++)
                workers.Add(CreateColonist(skillValue: 1f, health: 1f));

            Assert.AreEqual(1f, ExtractorYield.ComputeBuildingYield(workers, JobId), 0.0001f);
        }

        [Test]
        public void ComputeBuildingYield_UnderstaffedBuilding_DividesByMaxSlotsNotWorkerCount()
        {
            var workers = new List<Colonist>
            {
                CreateColonist(skillValue: 0.3f, health: 0.48f), // (0.3+0.48)/2 = 0.39
                CreateColonist(skillValue: 0.5f, health: 0.44f)  // (0.5+0.44)/2 = 0.47
            };

            // (0.39 + 0.47) / 5 postes max = 0.172, pas /2 travailleurs présents
            Assert.AreEqual(0.172f, ExtractorYield.ComputeBuildingYield(workers, JobId), 0.001f);
        }

        [Test]
        public void ComputeBuildingYield_NoWorkers_IsZero()
        {
            Assert.AreEqual(0f, ExtractorYield.ComputeBuildingYield(new List<Colonist>(), JobId));
        }
    }
}
