namespace Game.Colonists
{
    public enum SkillProgressionSource
    {
        OnTheJob,
        University
    }

    public sealed class SkillLevel
    {
        public string JobId { get; }
        public float Value { get; set; }
        public SkillProgressionSource Source { get; set; } = SkillProgressionSource.OnTheJob;

        public SkillLevel(string jobId)
        {
            JobId = jobId;
        }
    }
}
