using System;
using Game.Procedural;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Procedural
{
    public class TerrainRoutingTests
    {
        private static Planet CreatePlanetWithColumns(int width, int height, Func<int, TerrainType> terrainForColumn)
        {
            var zones = new Zone[width, height];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                zones[x, y] = new Zone(new GridPosition(x, y), terrainForColumn(x), isBuildable: true);

            return new Planet(Guid.NewGuid(), 0, width, height, zones);
        }

        private static TerrainSpeedCatalog CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<TerrainSpeedCatalog>();
            catalog.Initialize(new[]
            {
                new TerrainSpeedEntry { Terrain = TerrainType.Plains, SpeedModifier = 1f },
                new TerrainSpeedEntry { Terrain = TerrainType.Mountains, SpeedModifier = 0.4f }
            });
            return catalog;
        }

        [Test]
        public void ComputeAverageSpeedModifier_SameStartAndEnd_ReturnsSingleTileModifier()
        {
            var planet = CreatePlanetWithColumns(5, 1, _ => TerrainType.Mountains);
            var catalog = CreateCatalog();

            var modifier = TerrainRouting.ComputeAverageSpeedModifier(planet, 2, 0, 2, 0, catalog);

            Assert.AreEqual(0.4f, modifier, 0.001f);
        }

        [Test]
        public void ComputeAverageSpeedModifier_AveragesModifiersAcrossTraversedTerrain()
        {
            // Colonnes 0-4 en plaine (x1), colonnes 5-9 en montagne (x0.4) : un trajet horizontal
            // pur de (0,0) à (9,0) traverse les deux à parts égales.
            var planet = CreatePlanetWithColumns(10, 1, x => x < 5 ? TerrainType.Plains : TerrainType.Mountains);
            var catalog = CreateCatalog();

            var modifier = TerrainRouting.ComputeAverageSpeedModifier(planet, 0, 0, 9, 0, catalog);

            Assert.AreEqual(0.7f, modifier, 0.001f); // (5*1 + 5*0.4) / 10
        }

        [Test]
        public void ComputeAverageSpeedModifier_UnknownTerrain_FallsBackToDefaultModifier()
        {
            var planet = CreatePlanetWithColumns(3, 1, _ => TerrainType.Water); // absent du catalogue
            var catalog = CreateCatalog();

            var modifier = TerrainRouting.ComputeAverageSpeedModifier(planet, 0, 0, 2, 0, catalog);

            Assert.AreEqual(1f, modifier, 0.001f); // valeur par défaut du catalogue
        }
    }
}
