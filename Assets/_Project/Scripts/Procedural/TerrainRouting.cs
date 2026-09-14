using System;
using System.Collections.Generic;

namespace Game.Procedural
{
    // Vitesse de transport moyenne le long d'un trajet en ligne droite entre deux zones (Phase 1 :
    // aucun système de pathfinding, la ligne de Bresenham approxime le trajet) : chaque case
    // traversée module la vitesse selon son terrain (TerrainSpeedCatalog, valeurs d'équilibrage de
    // contenu).
    public static class TerrainRouting
    {
        public static float ComputeAverageSpeedModifier(Planet planet, int x0, int y0, int x1, int y1, TerrainSpeedCatalog catalog)
        {
            if (planet == null || catalog == null) return 1f;

            var total = 0f;
            var count = 0;

            foreach (var (x, y) in WalkLine(x0, y0, x1, y1))
            {
                if (!planet.TryGetZone(x, y, out var zone)) continue;
                total += catalog.GetSpeedModifier(zone.Terrain);
                count++;
            }

            return count > 0 ? total / count : 1f;
        }

        // Algorithme de Bresenham : les cases traversées par la ligne droite entre deux points.
        internal static IEnumerable<(int x, int y)> WalkLine(int x0, int y0, int x1, int y1)
        {
            var dx = Math.Abs(x1 - x0);
            var dy = -Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx + dy;

            var x = x0;
            var y = y0;
            while (true)
            {
                yield return (x, y);
                if (x == x1 && y == y1) yield break;

                var e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }
    }
}
