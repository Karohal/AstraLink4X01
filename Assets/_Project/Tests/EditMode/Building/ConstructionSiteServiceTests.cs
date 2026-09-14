using System;
using System.Collections.Generic;
using Game.Building;
using Game.Colonists;
using Game.FogOfWar;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Building
{
    public class ConstructionSiteServiceTests
    {
        private static BuildingDefinition CreateDefinition(float duration)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            definition.Initialize("warehouse", "Warehouse", new ResourceAmount[0], duration);
            return definition;
        }

        [Test]
        public void Tick_WithoutAssignedColonist_DoesNotProgress()
        {
            var building = new BuildingInstance(Guid.NewGuid(), "warehouse", 0, 0);
            var definition = CreateDefinition(10f);
            var service = new ConstructionSiteService();

            service.Tick(building, definition, planet: null, allColonists: new List<Colonist>(), deltaSimTime: 5f);

            Assert.AreEqual(0f, building.ConstructionProgress); // FR-043
            Assert.AreEqual(BuildingState.UnderConstruction, building.State);
        }

        [Test]
        public void Tick_WithAssignedColonist_ProgressesAndCompletes()
        {
            var building = new BuildingInstance(Guid.NewGuid(), "warehouse", 3, 4);
            var definition = CreateDefinition(10f);
            var worker = new Colonist(Guid.NewGuid(), "Worker", Gender.Male, "default")
            {
                CurrentAssignment = new Assignment(AssignmentType.Construction, building.Id)
            };
            var service = new ConstructionSiteService();
            BuildingConstructedEvent raised = null;
            service.BuildingCompleted += e => raised = e;

            service.Tick(building, definition, planet: null, allColonists: new[] { worker }, deltaSimTime: 6f);
            Assert.AreEqual(6f, building.ConstructionProgress);
            Assert.AreEqual(BuildingState.UnderConstruction, building.State);

            service.Tick(building, definition, planet: null, allColonists: new[] { worker }, deltaSimTime: 6f);
            Assert.AreEqual(BuildingState.Operational, building.State); // FR-042
            Assert.IsNotNull(raised);
            Assert.AreEqual(3, raised.X);
            Assert.AreEqual(4, raised.Y);
        }
    }
}
