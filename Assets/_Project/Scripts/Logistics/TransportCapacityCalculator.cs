using System;
using Game.Economy;

namespace Game.Logistics
{
    // Charge maximale d'un transporteur pour une ressource donnée : le premier des deux plafonds
    // atteint (masse ou volume) l'emporte — pas seulement l'un des deux. La quantité suivie par
    // Inventory est traitée comme une masse en kg ; le volume qu'occupe cette masse dépend de la
    // densité propre à chaque ressource (ResourceDefinition.DensityKgPerCubicMeter).
    public static class TransportCapacityCalculator
    {
        public static float ComputeMaxLoadKg(ResourceDefinition resource, TransporterDefinition transporter)
        {
            if (resource == null || transporter == null) return 0f;
            if (resource.DensityKgPerCubicMeter <= 0f) return transporter.MassCapacityKg;

            var massLimitKg = transporter.MassCapacityKg;
            var volumeLimitKg = transporter.VolumeCapacityCubicMeters * resource.DensityKgPerCubicMeter;

            return Math.Min(massLimitKg, volumeLimitKg);
        }
    }
}
