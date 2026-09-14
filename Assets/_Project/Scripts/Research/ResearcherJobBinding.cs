using System;
using System.Collections.Generic;
using System.Linq;
using Game.Colonists;

namespace Game.Research
{
    public interface IResearchYieldProvider
    {
        float ComputeResearchYield(Colonist researcher);
    }

    // Rendement provisoire fixe tant que US6 (compétence/santé réelles) n'est pas implémentée ;
    // remplacé par ISkillProgressionService.ComputeYield en US6 (T050).
    public sealed class FlatResearchYieldProvider : IResearchYieldProvider
    {
        private readonly float _flatYield;

        public FlatResearchYieldProvider(float flatYield = 1f)
        {
            _flatYield = flatYield;
        }

        public float ComputeResearchYield(Colonist researcher) => _flatYield;
    }

    // FR-008/FR-017 : un colon assigné au métier de chercheur alimente la recherche à chaque tick.
    public sealed class ResearcherJobBinding
    {
        private readonly IResearchService _researchService;
        private readonly IResearchYieldProvider _yieldProvider;

        public ResearcherJobBinding(IResearchService researchService, IResearchYieldProvider yieldProvider)
        {
            _researchService = researchService;
            _yieldProvider = yieldProvider;
        }

        public void Tick(IEnumerable<Colonist> colonists, IEnumerable<JobSlot> researcherSlots, Technology activeTechnology, float deltaSimTime)
        {
            if (activeTechnology == null) return;

            var slotIds = new HashSet<Guid>(researcherSlots.Select(s => s.Id));
            var researchers = colonists.Where(c =>
                c.CurrentAssignment != null &&
                c.CurrentAssignment.Type == AssignmentType.Job &&
                slotIds.Contains(c.CurrentAssignment.TargetId));

            foreach (var researcher in researchers)
                _researchService.ContributeProgress(activeTechnology, _yieldProvider.ComputeResearchYield(researcher) * deltaSimTime);
        }
    }
}
