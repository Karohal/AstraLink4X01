using System;
using System.Collections.Generic;

namespace Game.Colonists
{
    public interface IAssignable
    {
        Guid Id { get; }
        AssignmentType Type { get; }
    }

    public interface IAssignmentService
    {
        // Disponible dès les fondations : réutilisé par US2 (chantier), US3 (chercheur) et US4
        // (transport) avant même que SetAssignmentMode/RunAutomaticAssignmentPass (US6, FR-035)
        // n'existent.
        void AssignManually(Colonist colonist, IAssignable target);

        void Unassign(Colonist colonist);
        void SetAssignmentMode(Colonist colonist, AssignmentMode mode);
        void RunAutomaticAssignmentPass(IEnumerable<Colonist> colonists, IEnumerable<IAssignable> openTargets);
    }

    public sealed class AssignmentService : IAssignmentService
    {
        public void AssignManually(Colonist colonist, IAssignable target)
        {
            if (colonist == null) throw new ArgumentNullException(nameof(colonist));
            if (target == null) throw new ArgumentNullException(nameof(target));

            colonist.CurrentAssignment = new Assignment(target.Type, target.Id);
        }

        public void Unassign(Colonist colonist)
        {
            colonist.CurrentAssignment = null;
        }

        public void SetAssignmentMode(Colonist colonist, AssignmentMode mode)
        {
            colonist.AssignmentMode = mode; // FR-035
        }

        public void RunAutomaticAssignmentPass(IEnumerable<Colonist> colonists, IEnumerable<IAssignable> openTargets)
        {
            var targets = new Queue<IAssignable>(openTargets);
            foreach (var colonist in colonists)
            {
                if (colonist.AssignmentMode != AssignmentMode.Automatic) continue;
                if (!colonist.IsUnemployed) continue;
                if (targets.Count == 0) break;

                AssignManually(colonist, targets.Dequeue());
            }
        }
    }
}
