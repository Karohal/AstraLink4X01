using System;
using System.Collections.Generic;

namespace Game.Procedural
{
    public interface IPlanetGenerationService
    {
        Planet Generate(int seed, int width, int height, IReadOnlyList<string> resourceIds);
    }

    // Grille carrée (research.md §2) : simple à générer, sérialiser et interroger pour le rayon
    // de brouillard de guerre.
    public sealed class PlanetGenerationService : IPlanetGenerationService
    {
        private const float WaterChance = 0.12f;
        private const float DepositChance = 0.06f;
        private const float DepositMinQuantity = 200f;
        private const float DepositMaxQuantity = 1000f;

        public Planet Generate(int seed, int width, int height, IReadOnlyList<string> resourceIds)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            var random = new Random(seed);
            var zones = new Zone[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var isWater = random.NextDouble() < WaterChance;
                    var terrain = isWater ? TerrainType.Water : PickLandTerrain(random);
                    var isBuildable = !isWater;

                    Deposit deposit = null;
                    if (isBuildable && resourceIds != null && resourceIds.Count > 0 && random.NextDouble() < DepositChance)
                    {
                        var resourceId = resourceIds[random.Next(resourceIds.Count)];
                        var quantity = DepositMinQuantity + (float)random.NextDouble() * (DepositMaxQuantity - DepositMinQuantity);
                        deposit = new Deposit(resourceId, quantity);
                    }

                    zones[x, y] = new Zone(new GridPosition(x, y), terrain, isBuildable, deposit);
                }
            }

            return new Planet(Guid.NewGuid(), seed, width, height, zones);
        }

        private static TerrainType PickLandTerrain(Random random)
        {
            var roll = random.NextDouble();
            if (roll < 0.6) return TerrainType.Plains;
            if (roll < 0.85) return TerrainType.Hills;
            return TerrainType.Mountains;
        }
    }
}
