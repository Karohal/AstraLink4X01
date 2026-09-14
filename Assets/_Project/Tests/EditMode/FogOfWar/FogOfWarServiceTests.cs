using System;
using Game.FogOfWar;
using Game.Procedural;
using NUnit.Framework;

namespace Game.Tests.EditMode.FogOfWar
{
    public class FogOfWarServiceTests
    {
        private static Planet CreatePlanet(int size)
        {
            var zones = new Zone[size, size];
            for (var x = 0; x < size; x++)
            for (var y = 0; y < size; y++)
                zones[x, y] = new Zone(new GridPosition(x, y), TerrainType.Plains, true);

            return new Planet(Guid.NewGuid(), 0, size, size, zones);
        }

        [Test]
        public void IsRevealed_DefaultsToFalse()
        {
            var planet = CreatePlanet(10);
            var service = new FogOfWarService();

            Assert.IsFalse(service.IsRevealed(planet, 5, 5));
        }

        [Test]
        public void RevealAround_RevealsZonesWithinRadius()
        {
            var planet = CreatePlanet(10);
            var service = new FogOfWarService();

            service.RevealAround(planet, 5, 5, 2);

            Assert.IsTrue(service.IsRevealed(planet, 5, 5));
            Assert.IsTrue(service.IsRevealed(planet, 6, 5));
        }

        [Test]
        public void RevealAround_DoesNotRevealZonesOutsideRadius()
        {
            var planet = CreatePlanet(10);
            var service = new FogOfWarService();

            service.RevealAround(planet, 5, 5, 1);

            Assert.IsFalse(service.IsRevealed(planet, 8, 8));
        }
    }
}
