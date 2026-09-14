using Game.Economy;
using Game.Logistics;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Logistics
{
    public class TransportCapacityCalculatorTests
    {
        private static ResourceDefinition CreateResource(float densityKgPerCubicMeter)
        {
            var resource = ScriptableObject.CreateInstance<ResourceDefinition>();
            resource.Initialize("test-resource", "Test Resource", densityKgPerCubicMeter: densityKgPerCubicMeter);
            return resource;
        }

        private static TransporterDefinition CreateColonistTransporter()
        {
            var transporter = ScriptableObject.CreateInstance<TransporterDefinition>();
            transporter.Initialize("colonist", "Colon", massCapacityKg: 12f, volumeCapacityCubicMeters: 0.012f);
            return transporter;
        }

        [Test]
        public void ComputeMaxLoadKg_Water_MassAndVolumeLimitsCoincide()
        {
            var water = CreateResource(1000f); // densité de l'eau
            var transporter = CreateColonistTransporter();

            Assert.AreEqual(12f, TransportCapacityCalculator.ComputeMaxLoadKg(water, transporter), 0.001f);
        }

        [Test]
        public void ComputeMaxLoadKg_LightBulkyResource_IsVolumeLimited()
        {
            var wood = CreateResource(650f); // densité du bois
            var transporter = CreateColonistTransporter();

            // 0.012 m³ * 650 kg/m³ = 7.8 kg < 12 kg de capacité massique -> le volume plafonne en premier
            Assert.AreEqual(7.8f, TransportCapacityCalculator.ComputeMaxLoadKg(wood, transporter), 0.001f);
        }

        [Test]
        public void ComputeMaxLoadKg_HeavyDenseResource_IsMassLimited()
        {
            var uranium = CreateResource(19050f); // densité de l'uranium
            var transporter = CreateColonistTransporter();

            // 0.012 m³ * 19050 kg/m³ = 228.6 kg, bien au-delà des 12 kg de capacité massique -> masse plafonne
            Assert.AreEqual(12f, TransportCapacityCalculator.ComputeMaxLoadKg(uranium, transporter), 0.001f);
        }
    }
}
