using System;
using System.Collections.Generic;
using Game.Building;
using Game.Colonists;
using Game.FogOfWar;

namespace Game.Procedural
{
    public sealed class BootstrapResult
    {
        public Planet Planet { get; }
        public BuildingInstance StartingShelter { get; }
        public List<Colonist> Colonists { get; }

        public BootstrapResult(Planet planet, BuildingInstance startingShelter, List<Colonist> colonists)
        {
            Planet = planet;
            StartingShelter = startingShelter;
            Colonists = colonists;
        }
    }

    public sealed class BootstrapConfig
    {
        public int Seed;
        public int Width = 64;
        public int Height = 64;
        public IReadOnlyList<string> ResourceIds;
        public EthnicityDefinition StartingEthnicity;
        public int StartingColonistCount = 6;
        public int StartingRevealRadius = 5;
        public string ShelterDefinitionId = "starting-shelter";
        public IReadOnlyList<string> StartingJobIdsToRandomize; // FR-021
    }

    public interface IGameBootstrapService
    {
        BootstrapResult Bootstrap(BootstrapConfig config);
    }

    // Point d'entrée de partie : place l'abri de secours initial et révèle la zone de départ
    // (FR-007/FR-003), puis dote la colonie en colons avec compétences aléatoires (FR-021) et
    // une répartition de genre ~50/50 (FR-046).
    public sealed class GameBootstrapService : IGameBootstrapService
    {
        private readonly IPlanetGenerationService _planetGenerationService;
        private readonly IFogOfWarService _fogOfWarService;
        private readonly IColonistIdentityService _identityService;
        private readonly Random _random;

        public GameBootstrapService(
            IPlanetGenerationService planetGenerationService,
            IFogOfWarService fogOfWarService,
            IColonistIdentityService identityService,
            Random random = null)
        {
            _planetGenerationService = planetGenerationService;
            _fogOfWarService = fogOfWarService;
            _identityService = identityService;
            _random = random ?? new Random();
        }

        public BootstrapResult Bootstrap(BootstrapConfig config)
        {
            var planet = _planetGenerationService.Generate(config.Seed, config.Width, config.Height, config.ResourceIds);

            var shelterX = config.Width / 2;
            var shelterY = config.Height / 2;
            var shelter = new BuildingInstance(Guid.NewGuid(), config.ShelterDefinitionId, shelterX, shelterY, isStartingShelter: true);

            _fogOfWarService.RevealAround(planet, shelterX, shelterY, config.StartingRevealRadius);

            var colonists = new List<Colonist>();
            var maleCount = config.StartingColonistCount / 2;
            for (var i = 0; i < config.StartingColonistCount; i++)
            {
                var gender = i < maleCount ? Gender.Male : Gender.Female;
                var colonist = _identityService.CreateColonist(config.StartingEthnicity, gender);

                if (config.StartingJobIdsToRandomize != null)
                {
                    foreach (var jobId in config.StartingJobIdsToRandomize)
                    {
                        var skill = colonist.GetOrCreateSkill(jobId);
                        skill.Value = (float)_random.NextDouble() * 0.3f;
                    }
                }

                colonists.Add(colonist);
            }

            return new BootstrapResult(planet, shelter, colonists);
        }
    }
}
