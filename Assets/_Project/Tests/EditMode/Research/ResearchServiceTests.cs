using Game.Research;
using NUnit.Framework;

namespace Game.Tests.EditMode.Research
{
    public class ResearchServiceTests
    {
        [Test]
        public void ContributeProgress_AccumulatesUntilUnlocked()
        {
            var technology = new Technology("iron-extraction", "iron", progressRequired: 10f);
            var service = new ResearchService();

            Assert.IsFalse(service.IsUnlocked(technology));

            service.ContributeProgress(technology, 6f);
            Assert.IsFalse(service.IsUnlocked(technology));

            service.ContributeProgress(technology, 6f);
            Assert.IsTrue(service.IsUnlocked(technology)); // FR-008
        }

        [Test]
        public void ContributeProgress_DoesNotExceedRequiredProgress()
        {
            var technology = new Technology("iron-extraction", "iron", progressRequired: 10f);
            var service = new ResearchService();

            service.ContributeProgress(technology, 100f);

            Assert.AreEqual(10f, technology.ProgressCurrent);
        }
    }
}
