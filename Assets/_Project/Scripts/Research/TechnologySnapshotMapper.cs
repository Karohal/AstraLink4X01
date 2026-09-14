using Game.Core;

namespace Game.Research
{
    // FR-031 tranche US3 : traduction Technology <-> TechnologySnapshot (l'état des gisements est
    // déjà couvert par PlanetSnapshotMapper puisqu'un Deposit vit dans une Zone).
    public static class TechnologySnapshotMapper
    {
        public static TechnologySnapshot ToSnapshot(Technology technology)
        {
            return new TechnologySnapshot
            {
                Id = technology.Id,
                Progress = technology.ProgressCurrent,
                Unlocked = technology.IsUnlocked
            };
        }

        public static Technology FromSnapshot(TechnologySnapshot snapshot, string targetResourceId, float progressRequired)
        {
            var technology = new Technology(snapshot.Id, targetResourceId, progressRequired);
            technology.RestoreProgress(snapshot.Progress);
            return technology;
        }
    }
}
