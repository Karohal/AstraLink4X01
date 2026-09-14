using System;

namespace Game.Colonists
{
    public interface IColonistIdentityService
    {
        Colonist CreateColonist(EthnicityDefinition ethnicity, Gender? gender = null);
        void Rename(Colonist colonist, string newName);
        void SetBiography(Colonist colonist, string text);
    }

    public sealed class ColonistIdentityService : IColonistIdentityService
    {
        private readonly Random _random;

        public ColonistIdentityService(Random random = null)
        {
            _random = random ?? new Random();
        }

        public Colonist CreateColonist(EthnicityDefinition ethnicity, Gender? gender = null)
        {
            if (ethnicity == null) throw new ArgumentNullException(nameof(ethnicity));

            var resolvedGender = gender ?? (_random.NextDouble() < 0.5 ? Gender.Male : Gender.Female);
            var firstNames = resolvedGender == Gender.Male ? ethnicity.MaleFirstNames : ethnicity.FemaleFirstNames;
            var firstName = PickRandom(firstNames) ?? "Colon";
            var lastName = PickRandom(ethnicity.LastNames);
            var fullName = string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

            return new Colonist(Guid.NewGuid(), fullName, resolvedGender, ethnicity.Id);
        }

        public void Rename(Colonist colonist, string newName)
        {
            colonist.SetName(newName);
        }

        public void SetBiography(Colonist colonist, string text)
        {
            colonist.SetBiography(text);
        }

        private string PickRandom(string[] values)
        {
            if (values == null || values.Length == 0) return null;
            return values[_random.Next(values.Length)];
        }
    }
}
