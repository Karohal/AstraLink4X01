using System;
using Game.Procedural;

namespace Game.FogOfWar
{
    public interface IFogOfWarService
    {
        bool IsRevealed(Planet planet, int x, int y);
        void RevealAround(Planet planet, int x, int y, int radius);
    }

    public sealed class FogOfWarService : IFogOfWarService
    {
        public bool IsRevealed(Planet planet, int x, int y)
        {
            return planet != null && planet.TryGetZone(x, y, out var zone) && zone.IsRevealed;
        }

        // Disque de tuiles (pas un carré) autour du point, per FR-004.
        public void RevealAround(Planet planet, int x, int y, int radius)
        {
            if (planet == null) throw new ArgumentNullException(nameof(planet));

            var radiusSquared = radius * radius;
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy > radiusSquared) continue;

                    if (planet.TryGetZone(x + dx, y + dy, out var zone))
                        zone.Reveal();
                }
            }
        }
    }
}
