using Game.Logistics;
using NUnit.Framework;

namespace Game.Tests.EditMode.Logistics
{
    public class TravelTimeCalculatorTests
    {
        [Test]
        public void ComputeDistanceTiles_StraightHorizontalLine_ReturnsExactDistance()
        {
            Assert.AreEqual(5f, TravelTimeCalculator.ComputeDistanceTiles(0, 0, 5, 0), 0.001f);
        }

        [Test]
        public void ComputeDistanceTiles_DiagonalLine_ReturnsEuclideanDistance()
        {
            Assert.AreEqual(5f, TravelTimeCalculator.ComputeDistanceTiles(0, 0, 3, 4), 0.001f); // triangle 3-4-5
        }

        [Test]
        public void ComputeTravelDuration_DividesDistanceByEffectiveSpeed()
        {
            // 10 tuiles à 2 tuiles/s de base, terrain x0.5 -> vitesse effective 1 tuile/s -> 10s
            Assert.AreEqual(10f, TravelTimeCalculator.ComputeTravelDuration(10f, 2f, 0.5f), 0.001f);
        }

        [Test]
        public void ComputeTravelDuration_ZeroEffectiveSpeed_ReturnsPositiveInfinity()
        {
            Assert.AreEqual(float.PositiveInfinity, TravelTimeCalculator.ComputeTravelDuration(10f, 2f, 0f));
        }
    }
}
