using System;
using System.Collections.Generic;

namespace Game.Colonists
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum AssignmentMode
    {
        Manual,
        Automatic
    }

    public enum AssignmentType
    {
        Job,
        Transport,
        Construction,
        Training
    }

    public sealed class Assignment
    {
        public AssignmentType Type { get; }
        public Guid TargetId { get; }

        public Assignment(AssignmentType type, Guid targetId)
        {
            Type = type;
            TargetId = targetId;
        }
    }

    public sealed class Colonist
    {
        private const int MaxBiographyLength = 300;

        private readonly Dictionary<string, SkillLevel> _skills = new Dictionary<string, SkillLevel>();

        public Guid Id { get; }
        public string Name { get; private set; }
        public Gender Gender { get; }
        public string EthnicityId { get; }
        public string Biography { get; private set; } = string.Empty;
        public float Health { get; set; } = 1f;
        public AssignmentMode AssignmentMode { get; set; } = AssignmentMode.Manual;
        public Assignment CurrentAssignment { get; set; }
        public Guid? HousingId { get; set; }

        public Colonist(Guid id, string name, Gender gender, string ethnicityId)
        {
            Id = id;
            Name = name;
            Gender = gender;
            EthnicityId = ethnicityId;
        }

        public bool IsUnemployed => CurrentAssignment == null;

        public IReadOnlyDictionary<string, SkillLevel> Skills => _skills;

        public SkillLevel GetOrCreateSkill(string jobId)
        {
            if (!_skills.TryGetValue(jobId, out var skill))
            {
                skill = new SkillLevel(jobId);
                _skills[jobId] = skill;
            }

            return skill;
        }

        public void SetName(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return; // un nom vide n'est pas appliqué
            Name = newName;
        }

        public void SetBiography(string text)
        {
            text ??= string.Empty;
            // Le blocage à 300 caractères se fait côté saisie UI (FR-041) ; troncature de sécurité ici
            // pour tout appel qui contournerait l'UI (chargement de sauvegarde, tests).
            Biography = text.Length > MaxBiographyLength ? text.Substring(0, MaxBiographyLength) : text;
        }
    }
}
