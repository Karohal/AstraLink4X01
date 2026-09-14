namespace Game.Colonists
{
    // État de cohabitation d'un couple dans un logement (FR-047) : la durée continue repart à zéro
    // dès que le couple cesse d'occuper le logement à deux (rupture, décès non géré en Phase 1...),
    // pas seulement en cas d'échec de tentative de naissance.
    public sealed class HousingCohabitation
    {
        public float ContinuousDuration { get; private set; }
        public float TimeSinceLastBirthAttempt { get; private set; }

        public void AdvanceContinuous(float deltaTime) => ContinuousDuration += deltaTime;

        public void ResetContinuous()
        {
            ContinuousDuration = 0f;
            TimeSinceLastBirthAttempt = 0f;
        }

        public void AdvanceSinceLastBirthAttempt(float deltaTime) => TimeSinceLastBirthAttempt += deltaTime;

        public void ResetSinceLastBirthAttempt() => TimeSinceLastBirthAttempt = 0f;

        // Réservé au rechargement d'une sauvegarde (FR-031, non câblé pour cette fonctionnalité à
        // ce stade — cf. limite connue des colons non persistés, US6/T051).
        public void RestoreState(float continuousDuration, float timeSinceLastBirthAttempt)
        {
            ContinuousDuration = continuousDuration;
            TimeSinceLastBirthAttempt = timeSinceLastBirthAttempt;
        }
    }
}
