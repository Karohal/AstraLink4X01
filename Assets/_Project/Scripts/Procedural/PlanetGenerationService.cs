using System;
using System.Collections.Generic;

namespace Game.Procedural
{
    public interface IPlanetGenerationService
    {
        // infiniteResourceIds : ressources durables (ex: bois) dont le gisement ne s'épuise jamais.
        // waterResourceId : si fourni, chaque case d'eau reçoit systématiquement un gisement de
        // cette ressource (FR-001), en plus des gisements terrestres classiques ("nappe
        // phréatique" si waterResourceId figure aussi dans resourceIds).
        Planet Generate(int seed, int width, int height, IReadOnlyList<string> resourceIds,
            IReadOnlyList<string> infiniteResourceIds = null, string waterResourceId = null,
            float waterMinQuantity = 150f, float waterMaxQuantity = 500f);
    }

    // Grille carrée (research.md §2) : simple à générer, sérialiser et interroger pour le rayon
    // de brouillard de guerre.
    public sealed class PlanetGenerationService : IPlanetGenerationService
    {
        private const float WaterChance = 0.12f;
        private const float DepositChance = 0.06f;
        private const float DepositMinQuantity = 200f;
        private const float DepositMaxQuantity = 1000f;

        public Planet Generate(int seed, int width, int height, IReadOnlyList<string> resourceIds,
            IReadOnlyList<string> infiniteResourceIds = null, string waterResourceId = null,
            float waterMinQuantity = 150f, float waterMaxQuantity = 500f)
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
                    if (isWater)
                    {
                        if (!string.IsNullOrEmpty(waterResourceId))
                        {
                            var quantity = waterMinQuantity + (float)random.NextDouble() * (waterMaxQuantity - waterMinQuantity);
                            var isWaterInfinite = infiniteResourceIds != null && Contains(infiniteResourceIds, waterResourceId);
                            deposit = new Deposit(waterResourceId, quantity, isWaterInfinite); // gisement sous la case d'eau
                        }
                    }
                    else if (isBuildable && resourceIds != null && resourceIds.Count > 0 && random.NextDouble() < DepositChance)
                    {
                        var resourceId = resourceIds[random.Next(resourceIds.Count)];
                        var quantity = DepositMinQuantity + (float)random.NextDouble() * (DepositMaxQuantity - DepositMinQuantity);
                        var isInfinite = infiniteResourceIds != null && Contains(infiniteResourceIds, resourceId);
                        deposit = new Deposit(resourceId, quantity, isInfinite); // "nappe phréatique" si resourceId == waterResourceId
                    }

                    zones[x, y] = new Zone(new GridPosition(x, y), terrain, isBuildable, deposit);
                }
            }

            return new Planet(Guid.NewGuid(), seed, width, height, zones);
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
                if (values[i] == value)
                    return true;
            return false;
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
