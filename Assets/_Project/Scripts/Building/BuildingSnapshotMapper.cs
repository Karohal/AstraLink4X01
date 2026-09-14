using System;
using Game.Core;

namespace Game.Building
{
    // FR-031 tranche US2 : traduction BuildingInstance <-> BuildingSnapshot.
    public static class BuildingSnapshotMapper
    {
        public static BuildingSnapshot ToSnapshot(BuildingInstance building)
        {
            return new BuildingSnapshot
            {
                Id = building.Id,
                DefinitionId = building.DefinitionId,
                X = building.X,
                Y = building.Y,
                State = building.State.ToString(),
                ConstructionProgress = building.ConstructionProgress,
                IsStartingShelter = building.IsStartingShelter
            };
        }

        public static BuildingInstance FromSnapshot(BuildingSnapshot snapshot)
        {
            var building = new BuildingInstance(snapshot.Id, snapshot.DefinitionId, snapshot.X, snapshot.Y, snapshot.IsStartingShelter);
            var state = (BuildingState)Enum.Parse(typeof(BuildingState), snapshot.State);
            building.RestoreState(state, snapshot.ConstructionProgress);
            return building;
        }
    }
}
