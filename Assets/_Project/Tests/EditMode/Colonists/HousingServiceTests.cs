using System;
using System.Collections.Generic;
using Game.Colonists;
using NUnit.Framework;

namespace Game.Tests.EditMode.Colonists
{
    public class HousingServiceTests
    {
        private static Colonist CreateColonist(Gender gender)
        {
            return new Colonist(Guid.NewGuid(), "Test", gender, "human");
        }

        [Test]
        public void TryFormCouple_WithAvailableOppositeGenderPair_AssignsBothToHousing()
        {
            var housingId = Guid.NewGuid();
            var male = CreateColonist(Gender.Male);
            var female = CreateColonist(Gender.Female);
            var colonists = new List<Colonist> { male, female };

            var service = new HousingService();
            var formed = service.TryFormCouple(housingId, colonists);

            Assert.IsTrue(formed);
            Assert.AreEqual(housingId, male.HousingId);
            Assert.AreEqual(housingId, female.HousingId);
        }

        [Test]
        public void TryFormCouple_WithoutAvailableOppositeGenderPair_ReturnsFalse()
        {
            var housingId = Guid.NewGuid();
            var colonists = new List<Colonist> { CreateColonist(Gender.Male), CreateColonist(Gender.Male) };

            var service = new HousingService();
            var formed = service.TryFormCouple(housingId, colonists);

            Assert.IsFalse(formed);
            Assert.IsNull(colonists[0].HousingId);
        }

        [Test]
        public void TryFormCouple_WhenCoupleAlreadyOccupiesHousing_NoThirdAdultAssigned()
        {
            var housingId = Guid.NewGuid();
            var husband = CreateColonist(Gender.Male);
            var wife = CreateColonist(Gender.Female);
            husband.HousingId = housingId;
            wife.HousingId = housingId;

            var extraMale = CreateColonist(Gender.Male);
            var extraFemale = CreateColonist(Gender.Female);
            var colonists = new List<Colonist> { husband, wife, extraMale, extraFemale };

            var service = new HousingService();
            var formed = service.TryFormCouple(housingId, colonists);

            Assert.IsFalse(formed); // exclusivité : le logement a déjà un couple
            Assert.IsNull(extraMale.HousingId);
            Assert.IsNull(extraFemale.HousingId);
        }

        [Test]
        public void RemoveResident_ClearsHousingId()
        {
            var colonist = CreateColonist(Gender.Male);
            colonist.HousingId = Guid.NewGuid();

            new HousingService().RemoveResident(colonist);

            Assert.IsNull(colonist.HousingId);
        }
    }
}
