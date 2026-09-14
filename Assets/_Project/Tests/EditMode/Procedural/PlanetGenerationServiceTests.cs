using System;
using Game.Procedural;
using NUnit.Framework;

namespace Game.Tests.EditMode.Procedural
{
    public class PlanetGenerationServiceTests
    {
        [Test]
        public void Generate_ProducesGridOfRequestedSize()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(1, 10, 8, new[] { "iron" });

            Assert.AreEqual(10, planet.Width);
            Assert.AreEqual(8, planet.Height);
            Assert.IsTrue(planet.TryGetZone(0, 0, out _));
            Assert.IsFalse(planet.TryGetZone(10, 0, out _));
        }

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            var service = new PlanetGenerationService();
            var planetA = service.Generate(42, 20, 20, new[] { "iron" });
            var planetB = service.Generate(42, 20, 20, new[] { "iron" });

            for (var x = 0; x < 20; x++)
            for (var y = 0; y < 20; y++)
                Assert.AreEqual(planetA.Zones[x, y].Terrain, planetB.Zones[x, y].Terrain);
        }

        [Test]
        public void Generate_AllZonesStartUnrevealed()
        {
            var service = new PlanetGenerationService();
            var planet = service.Generate(1, 5, 5, Array.Empty<string>());

            foreach (var zone in planet.Zones)
                Assert.IsFalse(zone.IsRevealed);
        }
    }
}
