using System;

namespace Game.Logistics
{
    // Durée d'un trajet entre deux cases de la grille : distance euclidienne divisée par la
    // vitesse effective du transporteur (TransporterDefinition.SpeedTilesPerSecond, modulée par le
    // modificateur moyen de terrain traversé, cf. Game.Procedural.TerrainRouting). Ce service reste
    // indépendant de Game.Procedural : l'appelant lui fournit directement le modificateur déjà
    // calculé.
    public static class TravelTimeCalculator
    {
        public static float ComputeDistanceTiles(int x0, int y0, int x1, int y1)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static float ComputeTravelDuration(float distanceTiles, float baseSpeedTilesPerSecond, float terrainSpeedModifier)
        {
            var effectiveSpeed = baseSpeedTilesPerSecond * terrainSpeedModifier;
            if (effectiveSpeed <= 0f) return float.PositiveInfinity; // terrain effectivement infranchissable

            return distanceTiles / effectiveSpeed;
        }
    }
}
