using System;
using System.Collections.Generic;
using System.Linq;
using Game.Building;
using Game.Colonists;
using Game.Economy;
using Game.FogOfWar;
using Game.Logistics;
using Game.Procedural;
using Game.Research;
using UnityEngine;

namespace Game.Core
{
    // Logique de partie Phase 1 (US1-US4 + peaufinages), extraite pour être réutilisable par
    // plusieurs vues (Phase1GameView notamment) sans dépendre d'un rendu particulier. Orchestration
    // pure (Principe II) : relie les services Game.<Module> déjà testés en EditMode ; aucune règle
    // métier n'est réimplémentée ici.
    //
    // Miroir logique de Phase1TestHarness (scène de debug Phase1_MVP, volontairement laissée
    // inchangée pour ne rien risquer sur cet outil de test qui fonctionne déjà) : une passe future
    // pourrait unifier les deux si Phase1_MVP est un jour dépréciée.
    public sealed class Phase1GameController
    {
        private readonly BuildingDefinition _shelterDefinition;
        private readonly BuildingDefinition _storageDefinition;
        private readonly BuildingDefinition _cisternDefinition;
        private readonly BuildingDefinition _extractorDefinition;
        private readonly BuildingDefinition _pumpDefinition;
        private readonly BuildingDefinition _housingDefinition;

        private readonly ResourceDefinition _materialsResource;
        private readonly ResourceDefinition _woodResource;
        private readonly ResourceDefinition _stoneResource;
        private readonly ResourceDefinition _waterResource;

        private readonly TechnologyDefinition _woodTechnologyDefinition;
        private readonly TechnologyDefinition _stoneTechnologyDefinition;
        private readonly TechnologyDefinition _waterTechnologyDefinition;

        private readonly JobDefinition _researcherJobDefinition;
        private readonly EthnicityDefinition _startingEthnicity;

        private readonly TransporterDefinition _colonistTransporterDefinition;
        private readonly TerrainSpeedCatalog _terrainSpeedCatalog;

        private readonly int _seed;
        private readonly int _width;
        private readonly int _height;
        private readonly int _startingRevealRadius;
        private readonly float _startingMaterials;
        private readonly float _loadingDurationSeconds;
        private readonly float _unloadingDurationSeconds;
        private readonly float _secondsPerGameYear;

        private readonly IPlanetGenerationService _planetGenerationService;
        private readonly IFogOfWarService _fogOfWarService;
        private readonly IColonistIdentityService _identityService;
        private readonly IAssignmentService _assignmentService;
        private readonly IGameBootstrapService _bootstrapService;
        private readonly IBuildingPlacementService _placementService;
        private readonly IConstructionSiteService _constructionSiteService;
        private readonly IExtractionService _extractionService;
        private readonly IResearchService _researchService;
        private readonly ResearcherJobBinding _researcherJobBinding;
        private readonly ITransportService _transportService;
        private readonly ISimulationClock _clock;
        private readonly ISaveLoadService _saveLoadService;
        private readonly FogOfWarConstructionListener _fogListener;
        private readonly IHousingService _housingService;
        private readonly IHousingBirthService _housingBirthService;
        private readonly IMultiPurposeExtractorService _multiPurposeExtractorService;

        public Planet Planet { get; private set; }
        public BuildingInstance Shelter { get; private set; }
        public readonly List<BuildingInstance> Buildings = new List<BuildingInstance>();
        public readonly Dictionary<Guid, BuildingDefinition> DefinitionsById = new Dictionary<Guid, BuildingDefinition>();
        public readonly Dictionary<Guid, Inventory> ExtractorOutputBuffers = new Dictionary<Guid, Inventory>();
        public readonly List<Colonist> Colonists = new List<Colonist>();
        public readonly List<TransportTask> TransportTasks = new List<TransportTask>();
        public Inventory Warehouse { get; private set; } // trésor de matériaux de construction (FR-005/FR-006), pas un site de stockage
        public readonly Dictionary<Guid, Inventory> StorageInventoriesByBuildingId = new Dictionary<Guid, Inventory>();
        public Guid? SelectedDestinationBuildingId;

        public Dictionary<string, TechnologyDefinition> TechnologyDefinitionsByResourceId { get; private set; }
        public Dictionary<string, Technology> TechnologiesByResourceId { get; private set; }
        public Dictionary<string, string> ResourceDisplayNamesById { get; private set; }
        public Dictionary<string, ResourceDefinition> ResourceDefinitionsById { get; private set; }
        public string SelectedResearchResourceId;
        private JobSlot _researcherSlot;
        public readonly Dictionary<Guid, JobSlot> OperatorSlotsByBuildingId = new Dictionary<Guid, JobSlot>();
        public readonly Dictionary<Guid, HousingCohabitation> HousingCohabitationsByBuildingId = new Dictionary<Guid, HousingCohabitation>();

        // Inventaire du Module de survie (FR-050/FR-051) : 5 exemplaires fournis au démarrage,
        // initialement non placés (IsPlaced == false) ; leur production rejoint directement
        // Warehouse (cf. IMultiPurposeExtractorService.Tick).
        public readonly List<MultiPurposeExtractor> MultiPurposeExtractors = new List<MultiPurposeExtractor>();
        public const int MultiPurposeExtractorCount = 5;

        public bool IsReady { get; private set; }

        // Ressources de base (bois/pierre/eau) : pas de verrou technologique à la construction
        // (contrairement aux futurs minerais avancés) — seul un gisement compatible est requis.
        private HashSet<string> _baseResourceIds;

        public int SelectedX = -1;
        public int SelectedY = -1;
        public BuildingDefinition SelectedBuildToPlace;

        // Orientation choisie pour le prochain bâtiment posé (FR-054), cycle 0°/90°/180°/270° via
        // RotatePendingBuild ; conservée d'un placement à l'autre tant que le joueur ne l'annule pas
        // explicitement (CancelBuildSelection), pour poser plusieurs exemplaires identiques de suite.
        public BuildingRotation PendingBuildRotation = BuildingRotation.Deg0;

        public string LastMessage = string.Empty;
        public string SaveSlotName = "phase1-game";

        // Crédits Galactiques (CG), monnaie officielle du jeu : solde affiché partout (header,
        // panel Gestion), mais placeholder inerte pour l'instant — aucune mécanique ne le fait
        // encore varier (ni revenus, ni dépenses). Une tâche dédiée définira ce qui en génère/en
        // consomme ; volontairement pas branché sur les ressources existantes pour ne pas inventer
        // une règle économique non spécifiée.
        public float GalacticCredits;

        // Mode d'assignation appliqué aux nouveaux colons (bootstrap, ou enfant atteignant sa
        // majorité) — ne modifie jamais un colon déjà existant dont le mode a été choisi
        // individuellement via sa fiche (réglage lu uniquement au moment de la création/majorité).
        public AssignmentMode DefaultAssignmentMode = AssignmentMode.Manual;

        // Colon en attente d'une assignation manuelle à un bâtiment précis (bouton "Assigner" de sa
        // fiche) : le prochain clic sur un bâtiment dans la vue valide l'assignation.
        public Guid? PendingAssignmentColonistId;

        // Extracteur multifonction en attente de placement/déplacement (bouton "Placer"/"Déplacer"
        // du panneau du Module de survie, FR-052) : le prochain clic sur une case dans la vue
        // valide le placement.
        public Guid? PendingMultiPurposeExtractorId;

        private readonly System.Random _random = new System.Random();

        public BuildingDefinition ShelterDefinition => _shelterDefinition;
        public BuildingDefinition StorageDefinition => _storageDefinition;
        public BuildingDefinition CisternDefinition => _cisternDefinition;
        public BuildingDefinition ExtractorDefinition => _extractorDefinition;
        public BuildingDefinition PumpDefinition => _pumpDefinition;
        public BuildingDefinition HousingDefinition => _housingDefinition;
        public ResourceDefinition WoodResource => _woodResource;
        public ResourceDefinition StoneResource => _stoneResource;
        public ResourceDefinition WaterResource => _waterResource;

        private sealed class AssignableTarget : IAssignable
        {
            public Guid Id { get; }
            public AssignmentType Type { get; }

            public AssignableTarget(Guid id, AssignmentType type)
            {
                Id = id;
                Type = type;
            }
        }

        public Phase1GameController(Phase1GameContent content, int seed, int width, int height,
            int startingRevealRadius, float startingMaterials, float loadingDurationSeconds, float unloadingDurationSeconds,
            float secondsPerGameYear)
        {
            _shelterDefinition = content.ShelterDefinition;
            _storageDefinition = content.StorageDefinition;
            _cisternDefinition = content.CisternDefinition;
            _extractorDefinition = content.ExtractorDefinition;
            _pumpDefinition = content.PumpDefinition;
            _housingDefinition = content.HousingDefinition;
            _materialsResource = content.MaterialsResource;
            _woodResource = content.WoodResource;
            _stoneResource = content.StoneResource;
            _waterResource = content.WaterResource;
            _woodTechnologyDefinition = content.WoodTechnologyDefinition;
            _stoneTechnologyDefinition = content.StoneTechnologyDefinition;
            _waterTechnologyDefinition = content.WaterTechnologyDefinition;
            _researcherJobDefinition = content.ResearcherJobDefinition;
            _startingEthnicity = content.StartingEthnicity;
            _colonistTransporterDefinition = content.ColonistTransporterDefinition;
            _terrainSpeedCatalog = content.TerrainSpeedCatalog;

            _seed = seed;
            _width = width;
            _height = height;
            _startingRevealRadius = startingRevealRadius;
            _startingMaterials = startingMaterials;
            _loadingDurationSeconds = loadingDurationSeconds;
            _unloadingDurationSeconds = unloadingDurationSeconds;
            _secondsPerGameYear = secondsPerGameYear;

            _planetGenerationService = new PlanetGenerationService();
            _fogOfWarService = new FogOfWarService();
            _identityService = new ColonistIdentityService();
            _assignmentService = new AssignmentService();
            _bootstrapService = new GameBootstrapService(_planetGenerationService, _fogOfWarService, _identityService);
            _placementService = new BuildingPlacementService();
            _constructionSiteService = new ConstructionSiteService();
            _fogListener = new FogOfWarConstructionListener(_fogOfWarService);
            _constructionSiteService.BuildingCompleted += _fogListener.OnBuildingConstructed;
            _extractionService = new ExtractionService();
            _researchService = new ResearchService();
            _researcherJobBinding = new ResearcherJobBinding(_researchService, new FlatResearchYieldProvider());
            _transportService = new TransportService();
            _housingService = new HousingService();
            _housingBirthService = new HousingBirthService(_identityService);
            _multiPurposeExtractorService = new MultiPurposeExtractorService();
            _clock = new SimulationClock();
            _clock.OnTick += Tick;
            _saveLoadService = new SaveLoadService(Application.persistentDataPath);
        }

        private bool HasRequiredContent()
        {
            return _shelterDefinition != null && _storageDefinition != null && _cisternDefinition != null &&
                   _extractorDefinition != null && _pumpDefinition != null && _housingDefinition != null &&
                   _materialsResource != null && _woodResource != null && _stoneResource != null && _waterResource != null &&
                   _woodTechnologyDefinition != null && _stoneTechnologyDefinition != null && _waterTechnologyDefinition != null &&
                   _researcherJobDefinition != null && _startingEthnicity != null &&
                   _colonistTransporterDefinition != null && _terrainSpeedCatalog != null;
        }

        public void Update(float realDeltaTime) => _clock?.Tick(realDeltaTime);

        public void Pause() => _clock.Pause();
        public void Resume() => _clock.Resume();
        public void SetSpeed(float multiplier) => _clock.SetSpeed(multiplier);
        public bool IsPaused => _clock.IsPaused;
        public float SpeedMultiplier => _clock.SpeedMultiplier;

        public void NewGame()
        {
            IsReady = false;
            GalacticCredits = 0f;

            if (!HasRequiredContent())
            {
                Debug.LogError("Phase1GameController : références de contenu manquantes. Vérifiez le Phase1GameContent assigné dans l'Inspector.");
                return;
            }

            TechnologyDefinitionsByResourceId = new Dictionary<string, TechnologyDefinition>
            {
                [_woodTechnologyDefinition.TargetResourceId] = _woodTechnologyDefinition,
                [_stoneTechnologyDefinition.TargetResourceId] = _stoneTechnologyDefinition,
                [_waterTechnologyDefinition.TargetResourceId] = _waterTechnologyDefinition
            };

            ResourceDisplayNamesById = new Dictionary<string, string>
            {
                [_materialsResource.Id] = _materialsResource.DisplayName,
                [_woodResource.Id] = _woodResource.DisplayName,
                [_stoneResource.Id] = _stoneResource.DisplayName,
                [_waterResource.Id] = _waterResource.DisplayName
            };

            ResourceDefinitionsById = new Dictionary<string, ResourceDefinition>
            {
                [_materialsResource.Id] = _materialsResource,
                [_woodResource.Id] = _woodResource,
                [_stoneResource.Id] = _stoneResource,
                [_waterResource.Id] = _waterResource
            };

            _baseResourceIds = new HashSet<string> { _woodResource.Id, _stoneResource.Id, _waterResource.Id };

            var config = new BootstrapConfig
            {
                Seed = _seed,
                Width = _width,
                Height = _height,
                ResourceIds = new[] { _woodResource.Id, _stoneResource.Id, _waterResource.Id },
                InfiniteResourceIds = new[] { _woodResource.Id, _waterResource.Id }, // le bois et l'eau ne s'épuisent jamais ; seule la pierre est finie
                WaterResourceId = _waterResource.Id, // chaque case d'eau reçoit un gisement d'eau
                StartingEthnicity = _startingEthnicity,
                StartingColonistCount = 6,
                StartingRevealRadius = _startingRevealRadius,
                ShelterDefinitionId = _shelterDefinition.Id,
                StartingJobIdsToRandomize = new[]
                {
                    _researcherJobDefinition.Id, JobDefinition.WoodcutterJobId, JobDefinition.MinerJobId, JobDefinition.MasonJobId
                }
            };

            var result = _bootstrapService.Bootstrap(config);
            Planet = result.Planet;
            Shelter = result.StartingShelter;

            Colonists.Clear();
            Colonists.AddRange(result.Colonists);
            foreach (var colonist in Colonists)
            {
                // Âge de départ plausible (18-45 ans) pour que la colonne "Âge" ait un sens dès le
                // début — les colons de départ n'ont pas de date de naissance connue.
                colonist.AgeSeconds = (18f + (float)_random.NextDouble() * 27f) * _secondsPerGameYear;
                colonist.AssignmentMode = DefaultAssignmentMode;
            }

            Buildings.Clear();
            Buildings.Add(Shelter);
            DefinitionsById.Clear();
            DefinitionsById[Shelter.Id] = _shelterDefinition;

            ExtractorOutputBuffers.Clear();
            TransportTasks.Clear();
            OperatorSlotsByBuildingId.Clear();
            StorageInventoriesByBuildingId.Clear();
            HousingCohabitationsByBuildingId.Clear();
            SelectedDestinationBuildingId = null;
            PendingAssignmentColonistId = null;

            MultiPurposeExtractors.Clear();
            for (var i = 0; i < MultiPurposeExtractorCount; i++)
                MultiPurposeExtractors.Add(new MultiPurposeExtractor(Guid.NewGuid()));

            Warehouse = new Inventory();
            Warehouse.SetQuantity(_materialsResource.Id, _startingMaterials);

            TechnologiesByResourceId = new Dictionary<string, Technology>
            {
                [_woodResource.Id] = new Technology(_woodTechnologyDefinition.Id, _woodResource.Id, _woodTechnologyDefinition.ProgressRequired),
                [_stoneResource.Id] = new Technology(_stoneTechnologyDefinition.Id, _stoneResource.Id, _stoneTechnologyDefinition.ProgressRequired),
                [_waterResource.Id] = new Technology(_waterTechnologyDefinition.Id, _waterResource.Id, _waterTechnologyDefinition.ProgressRequired)
            };
            SelectedResearchResourceId = _woodResource.Id;

            _researcherSlot = new JobSlot(Guid.NewGuid(), _researcherJobDefinition.Id, Shelter.Id);

            SelectedX = SelectedY = -1;
            SelectedBuildToPlace = null;
            LastMessage = "Nouvelle partie démarrée.";
            IsReady = true;
        }

        private void Tick(float deltaSimTime)
        {
            if (!IsReady) return;

            // Vieillissement général (enfants et adultes) : source unique de vérité pour
            // Colonist.AgeSeconds — HousingBirthService se contente de le LIRE pour détecter la
            // majorité, il ne l'incrémente plus lui-même.
            foreach (var colonist in Colonists)
                colonist.AgeSeconds += deltaSimTime;

            foreach (var building in Buildings)
            {
                if (building.State != BuildingState.UnderConstruction) continue;
                if (!DefinitionsById.TryGetValue(building.Id, out var definition)) continue;
                _constructionSiteService.Tick(building, definition, Planet, Colonists, deltaSimTime);
            }

            foreach (var building in Buildings)
            {
                if (!DefinitionsById.TryGetValue(building.Id, out var definition)) continue;
                if (definition != _extractorDefinition && definition != _pumpDefinition) continue;
                if (!building.IsOperational) continue;
                if (!Planet.TryGetZone(building.DepositX, building.DepositY, out var zone) || zone.Deposit == null) continue;

                if (zone.Deposit.State != DepositState.Extracting && zone.Deposit.State != DepositState.Depleted)
                    _extractionService.BeginExtraction(zone.Deposit);

                if (!ExtractorOutputBuffers.TryGetValue(building.Id, out var buffer))
                {
                    buffer = new Inventory();
                    ExtractorOutputBuffers[building.Id] = buffer;
                }

                var workers = GetAssignedWorkers(building.Id);
                var buildingYield = ExtractorYield.ComputeBuildingYield(workers, GetExtractionJobId(zone.Deposit.ResourceId));

                _extractionService.Tick(zone.Deposit, building, buffer, deltaSimTime, buildingYield);
            }

            if (TechnologiesByResourceId.TryGetValue(SelectedResearchResourceId, out var activeTechnology))
                _researcherJobBinding.Tick(Colonists, new[] { _researcherSlot }, activeTechnology, deltaSimTime);

            foreach (var extractor in MultiPurposeExtractors)
                _multiPurposeExtractorService.Tick(extractor, Planet, Warehouse, deltaSimTime);

            foreach (var building in Buildings)
            {
                if (!DefinitionsById.TryGetValue(building.Id, out var definition) || !definition.IsHousing) continue;
                if (!building.IsOperational) continue;

                // Assignation automatique (pas de sélection manuelle par le joueur, cf.
                // IHousingService.TryFormCouple) : dès que le logement n'a aucun adulte, forme un
                // couple avec les deux premiers colons disponibles de sexe opposé.
                _housingService.TryFormCouple(building.Id, Colonists);

                if (!HousingCohabitationsByBuildingId.TryGetValue(building.Id, out var cohabitation))
                {
                    cohabitation = new HousingCohabitation();
                    HousingCohabitationsByBuildingId[building.Id] = cohabitation;
                }

                var residents = Colonists.Where(c => c.HousingId == building.Id).ToList();
                var birthResult = _housingBirthService.Tick(building.Id, definition, cohabitation, residents, Colonists,
                    _startingEthnicity, deltaSimTime, _secondsPerGameYear);

                if (birthResult.Newborn != null)
                    Colonists.Add(birthResult.Newborn);

                // Un enfant qui vient d'atteindre sa majorité "arrive" dans la colonie active :
                // reçoit le mode d'assignation par défaut courant (jamais appliqué rétroactivement
                // à un colon déjà adulte).
                foreach (var grownUp in birthResult.GrownUpChildren)
                    grownUp.AssignmentMode = DefaultAssignmentMode;
            }

            foreach (var task in TransportTasks)
            {
                if (!ExtractorOutputBuffers.TryGetValue(task.SourceBuildingId, out var source)) continue;
                if (!StorageInventoriesByBuildingId.TryGetValue(task.DestinationBuildingId, out var destination)) continue;
                if (!TryBuildTransportCycleConfig(task, out var config)) continue;

                _transportService.Tick(task, source, destination, config, deltaSimTime);
            }
        }

        public void TryBuild(int x, int y, float offsetX = 0.5f, float offsetY = 0.5f, BuildingRotation rotation = BuildingRotation.Deg0)
        {
            if (SelectedBuildToPlace == null)
            {
                LastMessage = "Sélectionnez un type de bâtiment à construire.";
                return;
            }

            if (!Planet.TryGetZone(x, y, out var zone))
            {
                LastMessage = "Zone invalide.";
                return;
            }

            var isPump = SelectedBuildToPlace == _pumpDefinition;
            var isExtractorLike = SelectedBuildToPlace == _extractorDefinition || isPump;

            // FR-055 : la pompe ne cible plus le gisement de sa propre case (sauf nappe phréatique)
            // mais celui d'une case d'eau adjacente — la validation détaillée (adjacence, nappe,
            // jamais sur l'eau) est déléguée à IBuildingPlacementService.CanBuild ci-dessous ; ce
            // bloc ne gère plus que le cas de l'extracteur classique (gisement sur sa propre case).
            if (isExtractorLike && !isPump)
            {
                if (zone.Deposit == null)
                {
                    LastMessage = "Aucun gisement sur cette zone.";
                    return;
                }

                if (zone.Deposit.ResourceId == _waterResource.Id)
                {
                    LastMessage = "L'eau se pompe : utilisez une pompe, pas un extracteur.";
                    return;
                }

                // Bois/pierre/eau sont des ressources de base : aucun verrou technologique requis
                // pour construire dessus. Un futur minerai avancé resterait soumis à FR-009.
                if (!_baseResourceIds.Contains(zone.Deposit.ResourceId))
                {
                    if (!TechnologiesByResourceId.TryGetValue(zone.Deposit.ResourceId, out var technology) ||
                        !_extractionService.CanBuildExtractor(zone.Deposit, technology))
                    {
                        LastMessage = $"Technologie d'extraction non débloquée pour « {zone.Deposit.ResourceId} » (FR-009).";
                        return;
                    }
                }
            }

            if (!_placementService.CanBuild(Planet, SelectedBuildToPlace, x, y, Warehouse, out var missing))
            {
                if (missing.Contains("cannot-build-on-water"))
                    LastMessage = "Une pompe ne peut pas être construite directement sur une case d'eau (FR-055) : ciblez une case adjacente.";
                else if (missing.Contains("no-adjacent-water"))
                    LastMessage = "Aucune case d'eau adjacente ni nappe phréatique ici : la pompe ne peut pas s'y approvisionner.";
                else
                    LastMessage = "Construction refusée, ressources manquantes : " + string.Join(", ", missing);
                return;
            }

            var building = _placementService.Build(Planet, SelectedBuildToPlace, x, y, Warehouse, offsetX, offsetY, rotation);
            Buildings.Add(building);
            DefinitionsById[building.Id] = SelectedBuildToPlace;

            if (isExtractorLike)
                ExtractorOutputBuffers[building.Id] = new Inventory();

            if (SelectedBuildToPlace == _storageDefinition || SelectedBuildToPlace == _cisternDefinition)
                StorageInventoriesByBuildingId[building.Id] = new Inventory();

            LastMessage = $"{SelectedBuildToPlace.DisplayName} en chantier à ({x},{y}). Assignez un colon pour démarrer le chantier.";
        }

        public void AssignToConstruction(Colonist colonist)
        {
            var building = GetSelectedBuilding();
            if (building == null || building.State != BuildingState.UnderConstruction)
            {
                LastMessage = "Sélectionnez d'abord un chantier.";
                return;
            }

            _assignmentService.AssignManually(colonist, new AssignableTarget(building.Id, AssignmentType.Construction));
            LastMessage = $"{colonist.Name} assigné au chantier.";
        }

        // Bouton "Construire" sur un chantier : cherche automatiquement le meilleur Maçon
        // disponible parmi les colons en mode d'assignation automatique, sans retirer la possibilité
        // d'assigner manuellement (les deux options coexistent, au choix du joueur).
        public void TryAutoAssignMason(BuildingInstance building)
        {
            if (building.State != BuildingState.UnderConstruction)
            {
                LastMessage = "Ce bâtiment n'est pas en chantier.";
                return;
            }

            var candidate = Colonists
                .Where(c => !c.IsChild && c.AssignmentMode == AssignmentMode.Automatic && c.IsUnemployed)
                .OrderByDescending(c => c.GetOrCreateSkill(JobDefinition.MasonJobId).Value)
                .FirstOrDefault();

            if (candidate == null)
            {
                LastMessage = "Aucun colon disponible en mode automatique pour la construction.";
                return;
            }

            _assignmentService.AssignManually(candidate, new AssignableTarget(building.Id, AssignmentType.Construction));
            LastMessage = $"{candidate.Name} (Maçon) assigné automatiquement au chantier.";
        }

        public void AssignToResearch(Colonist colonist)
        {
            _assignmentService.AssignManually(colonist, new AssignableTarget(_researcherSlot.Id, AssignmentType.Job));
            LastMessage = $"{colonist.Name} assigné à la recherche (« {SelectedResearchResourceId} »).";
        }

        // Poste d'exploitation d'un extracteur/pompe : jusqu'à ExtractorYield.MaxWorkerSlots colons
        // simultanés (un JobSlot partagé par bâtiment, comme le poste de chercheur).
        public void AssignToExtraction(Colonist colonist)
        {
            var building = GetSelectedBuilding();
            if (building == null || !DefinitionsById.TryGetValue(building.Id, out var def) ||
                (def != _extractorDefinition && def != _pumpDefinition))
            {
                LastMessage = "Sélectionnez un extracteur ou une pompe pour y affecter un travailleur.";
                return;
            }

            if (!OperatorSlotsByBuildingId.TryGetValue(building.Id, out var slot))
            {
                Planet.TryGetZone(building.DepositX, building.DepositY, out var zone);
                var jobId = zone?.Deposit != null ? GetExtractionJobId(zone.Deposit.ResourceId) : JobDefinition.MinerJobId;
                slot = new JobSlot(Guid.NewGuid(), jobId, building.Id);
                OperatorSlotsByBuildingId[building.Id] = slot;
            }

            var currentWorkerCount = GetAssignedWorkers(building.Id).Count;
            if (currentWorkerCount >= ExtractorYield.MaxWorkerSlots)
            {
                LastMessage = $"Effectif maximum atteint ({ExtractorYield.MaxWorkerSlots} postes).";
                return;
            }

            _assignmentService.AssignManually(colonist, new AssignableTarget(slot.Id, AssignmentType.Job));
            LastMessage = $"{colonist.Name} affecté à l'exploitation ({currentWorkerCount + 1}/{ExtractorYield.MaxWorkerSlots}).";
        }

        // Bûcheron pour le bois, Mineur pour tout le reste (pierre, eau, futurs minerais).
        private string GetExtractionJobId(string resourceId) =>
            resourceId == _woodResource.Id ? JobDefinition.WoodcutterJobId : JobDefinition.MinerJobId;

        public List<Colonist> GetAssignedWorkers(Guid buildingId)
        {
            if (!OperatorSlotsByBuildingId.TryGetValue(buildingId, out var slot)) return new List<Colonist>();
            return Colonists.Where(c =>
                c.CurrentAssignment != null &&
                c.CurrentAssignment.Type == AssignmentType.Job &&
                c.CurrentAssignment.TargetId == slot.Id).ToList();
        }

        // Compétence pertinente pour trier les candidats compatibles avec ce bâtiment (pas utilisée
        // pour l'assignation elle-même, cf. TryAssignColonistToBuilding) ; null si ce bâtiment n'a
        // aucun poste disponible (stockage, logement...).
        private string ResolveRelevantSkillJobId(BuildingInstance building, BuildingDefinition definition)
        {
            if (building.State == BuildingState.UnderConstruction) return JobDefinition.MasonJobId;

            if (definition == _extractorDefinition || definition == _pumpDefinition)
            {
                Planet.TryGetZone(building.DepositX, building.DepositY, out var zone);
                return zone?.Deposit != null ? GetExtractionJobId(zone.Deposit.ResourceId) : JobDefinition.MinerJobId;
            }

            if (building.Id == Shelter.Id) return JobDefinition.ResearcherJobId;

            return null;
        }

        // Liste filtrée du bouton "Assigner un colon" de la fiche bâtiment : uniquement les colons
        // compatibles (adultes), triés par compétence pertinente décroissante — vide si ce bâtiment
        // n'a pas de poste à pourvoir.
        public List<Colonist> GetAssignableColonists(BuildingInstance building)
        {
            if (!DefinitionsById.TryGetValue(building.Id, out var definition)) return new List<Colonist>();

            var relevantJobId = ResolveRelevantSkillJobId(building, definition);
            if (relevantJobId == null) return new List<Colonist>();

            return Colonists.Where(c => !c.IsChild)
                .OrderByDescending(c => c.GetOrCreateSkill(relevantJobId).Value)
                .ToList();
        }

        public void AssignToTransport(Colonist colonist)
        {
            var building = GetSelectedBuilding();
            if (building == null || !DefinitionsById.TryGetValue(building.Id, out var def) ||
                (def != _extractorDefinition && def != _pumpDefinition))
            {
                LastMessage = "Sélectionnez un extracteur ou une pompe pour organiser son transport.";
                return;
            }

            if (!Planet.TryGetZone(building.DepositX, building.DepositY, out var zone) || zone.Deposit == null)
            {
                LastMessage = "Gisement introuvable pour ce bâtiment.";
                return;
            }

            if (!SelectedDestinationBuildingId.HasValue ||
                !TryGetStorageBuilding(SelectedDestinationBuildingId.Value, out var destinationBuilding, out var destinationDef))
            {
                LastMessage = "Sélectionnez d'abord une destination : ciblez un entrepôt/une citerne construit(e) et opérationnel(le).";
                return;
            }

            var isWaterResource = zone.Deposit.ResourceId == _waterResource.Id;
            var destinationAcceptsResource = isWaterResource ? destinationDef == _cisternDefinition : destinationDef == _storageDefinition;
            if (!destinationAcceptsResource)
            {
                LastMessage = $"{destinationDef.DisplayName} ne peut pas recevoir « {ResourceDisplayName(zone.Deposit.ResourceId)} ».";
                return;
            }

            var task = TransportTasks.FirstOrDefault(t => t.SourceBuildingId == building.Id && t.DestinationBuildingId == destinationBuilding.Id);
            if (task == null)
            {
                task = _transportService.Assign(building.Id, destinationBuilding.Id, zone.Deposit.ResourceId, colonist.Id, null);
                TransportTasks.Add(task);
            }
            else
            {
                task.AssignedColonistId = colonist.Id;
            }

            _assignmentService.AssignManually(colonist, new AssignableTarget(task.Id, AssignmentType.Transport));
            LastMessage = $"{colonist.Name} assigné au transport vers {destinationDef.DisplayName} ({destinationBuilding.X},{destinationBuilding.Y}).";
        }

        // Bouton "Placer"/"Déplacer" de la fiche d'un Extracteur multifonction (fenêtre d'inventaire
        // du Module de survie, cf. FR-050/FR-051/FR-052) : place/déplace directement, pas de
        // chantier ni de destruction/reconstruction.
        public bool TryPlaceMultiPurposeExtractor(MultiPurposeExtractor extractor, int x, int y)
        {
            if (extractor == null) return false;

            var otherExtractors = MultiPurposeExtractors.Where(e => e.Id != extractor.Id).ToList();
            if (!_multiPurposeExtractorService.CanPlaceOn(Planet, _baseResourceIds, otherExtractors, x, y, out var reason))
            {
                LastMessage = reason switch
                {
                    "invalid-zone" => "Zone invalide ou non révélée pour un Extracteur multifonction.",
                    "incompatible-deposit" => "Un Extracteur multifonction ne peut être placé que sur un gisement d'eau, de pierre ou de bois.",
                    "deposit-occupied" => "Un autre Extracteur multifonction est déjà placé sur ce gisement.",
                    _ => "Placement refusé."
                };
                return false;
            }

            _multiPurposeExtractorService.PlaceOn(extractor, Planet, _baseResourceIds, otherExtractors, x, y);
            LastMessage = $"Extracteur multifonction placé en ({x},{y}).";
            return true;
        }

        public bool TryGetStorageBuilding(Guid buildingId, out BuildingInstance building, out BuildingDefinition definition)
        {
            building = Buildings.FirstOrDefault(b => b.Id == buildingId);
            definition = null;
            if (building == null || building.State != BuildingState.Operational) { building = null; return false; }
            if (!DefinitionsById.TryGetValue(buildingId, out definition)) return false;
            if (definition != _storageDefinition && definition != _cisternDefinition) { definition = null; return false; }
            return true;
        }

        public bool TryBuildTransportCycleConfig(TransportTask task, out TransportCycleConfig config)
        {
            config = default;
            var sourceBuilding = Buildings.FirstOrDefault(b => b.Id == task.SourceBuildingId);
            var destinationBuilding = Buildings.FirstOrDefault(b => b.Id == task.DestinationBuildingId);
            if (sourceBuilding == null || destinationBuilding == null) return false;

            config = BuildTransportCycleConfig(task.ResourceId, sourceBuilding.X, sourceBuilding.Y, destinationBuilding.X, destinationBuilding.Y);
            return true;
        }

        public TransportCycleConfig BuildTransportCycleConfig(string resourceId, int sourceX, int sourceY, int destinationX, int destinationY)
        {
            ResourceDefinitionsById.TryGetValue(resourceId, out var resourceDefinition);
            var maxLoadKg = TransportCapacityCalculator.ComputeMaxLoadKg(resourceDefinition, _colonistTransporterDefinition);

            var distanceTiles = TravelTimeCalculator.ComputeDistanceTiles(sourceX, sourceY, destinationX, destinationY);
            var speedModifier = TerrainRouting.ComputeAverageSpeedModifier(Planet, sourceX, sourceY, destinationX, destinationY, _terrainSpeedCatalog);
            var travelDuration = TravelTimeCalculator.ComputeTravelDuration(distanceTiles, _colonistTransporterDefinition.SpeedTilesPerSecond, speedModifier);

            return new TransportCycleConfig(travelDuration, _loadingDurationSeconds, travelDuration, _unloadingDurationSeconds, maxLoadKg);
        }

        public void Unassign(Colonist colonist)
        {
            _assignmentService.Unassign(colonist);
            LastMessage = $"{colonist.Name} libéré.";
        }

        public void SetColonistBiography(Colonist colonist, string text) => _identityService.SetBiography(colonist, text);

        public void SetColonistName(Colonist colonist, string newName) => _identityService.Rename(colonist, newName);

        public void SetBuildingName(BuildingInstance building, string newName) => building.Rename(newName);

        public void SetAssignmentMode(Colonist colonist, AssignmentMode mode) => _assignmentService.SetAssignmentMode(colonist, mode);

        // Santé globale de la colonie (panel Gestion) : moyenne simple sur tous les colons, enfants
        // compris (Colonist.Health n'a pas de règle différente pour eux à ce stade).
        public float ComputeAveragePopulationHealth() =>
            Colonists.Count == 0 ? 0f : Colonists.Average(c => c.Health);

        // Taux de naissance estimé (panel Gestion) : nombre de naissances attendues sur la
        // prochaine année de jeu, calculé à partir des couples actuellement éligibles (cohabitation
        // continue ≥ 1 an, cf. HousingBirthService) — pas un historique réel, faute de suivi
        // persistant des naissances passées.
        public float EstimateExpectedBirthsPerYear()
        {
            var total = 0f;

            foreach (var building in Buildings)
            {
                if (!DefinitionsById.TryGetValue(building.Id, out var definition) || !definition.IsHousing) continue;
                if (!HousingCohabitationsByBuildingId.TryGetValue(building.Id, out var cohabitation)) continue;
                if (cohabitation.ContinuousDuration < _secondsPerGameYear) continue;
                if (definition.BirthAttemptIntervalSeconds <= 0f) continue;

                var residents = Colonists.Where(c => c.HousingId == building.Id).ToList();
                var adults = residents.Where(c => !c.IsChild).ToList();
                var isCouple = adults.Count == 2 && adults[0].Gender != adults[1].Gender;
                if (!isCouple) continue;

                var childCount = residents.Count(c => c.IsChild);
                if (childCount >= definition.MaxChildResidents) continue;

                var attemptsPerYear = _secondsPerGameYear / definition.BirthAttemptIntervalSeconds;
                total += attemptsPerYear * definition.BirthAttemptSuccessChance;
            }

            return total;
        }

        // Résumé de l'occupation actuelle d'un colon (colonne "Spécialisation" du tableau, et fiche
        // détaillée) : dérivé de son affectation courante, pas un champ dédié.
        public string DescribeSpecialization(Colonist colonist)
        {
            if (colonist.IsChild) return "Enfant";
            if (colonist.CurrentAssignment == null) return "Sans emploi";

            switch (colonist.CurrentAssignment.Type)
            {
                case AssignmentType.Job:
                    if (_researcherSlot != null && colonist.CurrentAssignment.TargetId == _researcherSlot.Id) return "Chercheur";
                    var operatorSlot = OperatorSlotsByBuildingId.Values.FirstOrDefault(slot => slot.Id == colonist.CurrentAssignment.TargetId);
                    if (operatorSlot != null)
                        return operatorSlot.JobId == JobDefinition.WoodcutterJobId ? "Bûcheron" : "Mineur";
                    return "Emploi";
                case AssignmentType.Construction: return "Maçon";
                case AssignmentType.Transport: return "Transport";
                case AssignmentType.Training: return "Formation";
                default: return colonist.CurrentAssignment.Type.ToString();
            }
        }

        // Étape 1 du bouton "Assigner" de la fiche colon : arme le prochain clic sur un bâtiment
        // dans la vue (cf. TryAssignColonistToBuilding, appelé par la vue au clic).
        public void BeginManualAssignment(Colonist colonist)
        {
            PendingAssignmentColonistId = colonist.Id;
            LastMessage = $"Cliquez un bâtiment dans la vue pour y assigner {colonist.Name} (Échap pour annuler).";
        }

        public void CancelPendingAssignment()
        {
            if (!PendingAssignmentColonistId.HasValue) return;
            PendingAssignmentColonistId = null;
            LastMessage = "Assignation manuelle annulée.";
        }

        // Étape 1 du bouton "Placer"/"Déplacer" d'un Extracteur multifonction : arme le prochain
        // clic sur une case dans la vue (cf. TryPlaceMultiPurposeExtractor, appelé par la vue au
        // clic).
        public void BeginMultiPurposeExtractorPlacement(MultiPurposeExtractor extractor)
        {
            PendingMultiPurposeExtractorId = extractor.Id;
            LastMessage = "Cliquez une case dans la vue pour y placer l'Extracteur multifonction (Échap pour annuler).";
        }

        public void CancelPendingMultiPurposeExtractorPlacement()
        {
            if (!PendingMultiPurposeExtractorId.HasValue) return;
            PendingMultiPurposeExtractorId = null;
            LastMessage = "Placement d'Extracteur multifonction annulé.";
        }

        // Lien direct colon -> bâtiment choisi par le joueur (bouton "Assigner"), plutôt que les
        // raccourcis contextuels existants (AssignToConstruction/AssignToExtraction/AssignToResearch,
        // qui visent le bâtiment actuellement sélectionné). Détermine le type de poste selon le
        // bâtiment ciblé.
        public void TryAssignColonistToBuilding(Colonist colonist, BuildingInstance building)
        {
            if (!DefinitionsById.TryGetValue(building.Id, out var definition))
            {
                LastMessage = "Bâtiment introuvable.";
                return;
            }

            if (building.State == BuildingState.UnderConstruction)
            {
                _assignmentService.AssignManually(colonist, new AssignableTarget(building.Id, AssignmentType.Construction));
                LastMessage = $"{colonist.Name} assigné au chantier de {definition.DisplayName}.";
                return;
            }

            if (definition == _extractorDefinition || definition == _pumpDefinition)
            {
                if (!OperatorSlotsByBuildingId.TryGetValue(building.Id, out var slot))
                {
                    Planet.TryGetZone(building.DepositX, building.DepositY, out var zone);
                    var jobId = zone?.Deposit != null ? GetExtractionJobId(zone.Deposit.ResourceId) : JobDefinition.MinerJobId;
                    slot = new JobSlot(Guid.NewGuid(), jobId, building.Id);
                    OperatorSlotsByBuildingId[building.Id] = slot;
                }

                var currentWorkerCount = GetAssignedWorkers(building.Id).Count;
                if (currentWorkerCount >= ExtractorYield.MaxWorkerSlots)
                {
                    LastMessage = $"Effectif maximum atteint sur {definition.DisplayName} ({ExtractorYield.MaxWorkerSlots} postes).";
                    return;
                }

                _assignmentService.AssignManually(colonist, new AssignableTarget(slot.Id, AssignmentType.Job));
                LastMessage = $"{colonist.Name} affecté à {definition.DisplayName}.";
                return;
            }

            if (building.Id == Shelter.Id)
            {
                _assignmentService.AssignManually(colonist, new AssignableTarget(_researcherSlot.Id, AssignmentType.Job));
                LastMessage = $"{colonist.Name} assigné à la recherche (« {SelectedResearchResourceId} »).";
                return;
            }

            LastMessage = $"{definition.DisplayName} n'a pas de poste disponible pour l'instant.";
        }

        public void RecycleSelectedBuilding()
        {
            var building = GetSelectedBuilding();
            if (building == null || building.IsStartingShelter)
            {
                LastMessage = "Sélectionnez un bâtiment recyclable (pas le Module de survie).";
                return;
            }

            if (!DefinitionsById.TryGetValue(building.Id, out var definition))
            {
                LastMessage = "Définition introuvable pour ce bâtiment.";
                return;
            }

            _placementService.Recycle(Planet, building, definition, Warehouse);
            RemoveBuildingBookkeeping(building);

            SelectedX = SelectedY = -1;
            LastMessage = $"{definition.DisplayName} recyclé : {definition.RecycleRefundRatio:P0} du coût remboursé.";
        }

        public void CancelSelectedConstruction()
        {
            var building = GetSelectedBuilding();
            if (building == null || building.IsStartingShelter)
            {
                LastMessage = "Sélectionnez un chantier annulable.";
                return;
            }

            if (!DefinitionsById.TryGetValue(building.Id, out var definition))
            {
                LastMessage = "Définition introuvable pour ce bâtiment.";
                return;
            }

            if (!_placementService.TryCancelConstruction(Planet, building, definition, Warehouse))
            {
                LastMessage = "Ce chantier a déjà commencé : utilisez Recycler (remboursement partiel) à la place.";
                return;
            }

            RemoveBuildingBookkeeping(building);

            SelectedX = SelectedY = -1;
            LastMessage = $"{definition.DisplayName} annulé, ressources intégralement remboursées.";
        }

        public void CancelBuildSelection()
        {
            SelectedBuildToPlace = null;
            PendingBuildRotation = BuildingRotation.Deg0;
            LastMessage = "Sélection de construction annulée.";
        }

        // Bouton/touche "Pivoter" pendant le placement (FR-054) : cycle 0° -> 90° -> 180° -> 270° -> 0°.
        public void RotatePendingBuild()
        {
            PendingBuildRotation = PendingBuildRotation switch
            {
                BuildingRotation.Deg0 => BuildingRotation.Deg90,
                BuildingRotation.Deg90 => BuildingRotation.Deg180,
                BuildingRotation.Deg180 => BuildingRotation.Deg270,
                _ => BuildingRotation.Deg0
            };
        }

        private void RemoveBuildingBookkeeping(BuildingInstance building)
        {
            Buildings.Remove(building);
            DefinitionsById.Remove(building.Id);
            ExtractorOutputBuffers.Remove(building.Id);
            StorageInventoriesByBuildingId.Remove(building.Id);
            if (SelectedDestinationBuildingId == building.Id) SelectedDestinationBuildingId = null;

            if (HousingCohabitationsByBuildingId.Remove(building.Id))
            {
                // Logement détruit : les résidents (couple + enfants) sont mis à la rue plutôt que
                // de rester rattachés à un bâtiment qui n'existe plus.
                foreach (var resident in Colonists.Where(c => c.HousingId == building.Id).ToList())
                    _housingService.RemoveResident(resident);
            }

            var staleTaskIds = TransportTasks
                .Where(t => t.SourceBuildingId == building.Id || t.DestinationBuildingId == building.Id)
                .Select(t => t.Id).ToList();
            TransportTasks.RemoveAll(t => staleTaskIds.Contains(t.Id));

            OperatorSlotsByBuildingId.TryGetValue(building.Id, out var staleOperatorSlot);
            OperatorSlotsByBuildingId.Remove(building.Id);

            foreach (var colonist in Colonists)
            {
                if (colonist.CurrentAssignment == null) continue;
                var targetsRemovedBuilding =
                    (colonist.CurrentAssignment.Type == AssignmentType.Construction && colonist.CurrentAssignment.TargetId == building.Id) ||
                    (colonist.CurrentAssignment.Type == AssignmentType.Transport && staleTaskIds.Contains(colonist.CurrentAssignment.TargetId)) ||
                    (staleOperatorSlot != null && colonist.CurrentAssignment.Type == AssignmentType.Job && colonist.CurrentAssignment.TargetId == staleOperatorSlot.Id);
                if (targetsRemovedBuilding) _assignmentService.Unassign(colonist);
            }
        }

        public BuildingInstance GetSelectedBuilding()
        {
            if (SelectedX < 0 || !Planet.TryGetZone(SelectedX, SelectedY, out var zone) || !zone.BuildingId.HasValue)
                return null;

            return Buildings.FirstOrDefault(b => b.Id == zone.BuildingId.Value);
        }

        public void SaveGame()
        {
            var snapshot = new GameStateSnapshot
            {
                Planet = PlanetSnapshotMapper.ToSnapshot(Planet)
            };

            foreach (var building in Buildings)
                snapshot.Buildings.Add(BuildingSnapshotMapper.ToSnapshot(building));

            foreach (var technology in TechnologiesByResourceId.Values)
                snapshot.Technologies.Add(TechnologySnapshotMapper.ToSnapshot(technology));

            foreach (var task in TransportTasks)
                snapshot.TransportTasks.Add(TransportTaskSnapshotMapper.ToSnapshot(task));

            _saveLoadService.Save(snapshot, SaveSlotName);
            LastMessage = $"Partie sauvegardée ({SaveSlotName}).";
        }

        public void LoadGame()
        {
            GameStateSnapshot snapshot;
            try
            {
                snapshot = _saveLoadService.Load(SaveSlotName);
            }
            catch (UnsupportedSaveVersionException ex)
            {
                LastMessage = ex.Message;
                return;
            }

            if (snapshot == null)
            {
                LastMessage = "Aucune sauvegarde trouvée pour ce slot.";
                return;
            }

            Planet = PlanetSnapshotMapper.FromSnapshot(snapshot.Planet);

            Buildings.Clear();
            DefinitionsById.Clear();
            ExtractorOutputBuffers.Clear();
            StorageInventoriesByBuildingId.Clear();
            SelectedDestinationBuildingId = null;
            foreach (var buildingSnapshot in snapshot.Buildings)
            {
                var building = BuildingSnapshotMapper.FromSnapshot(buildingSnapshot);
                Buildings.Add(building);

                var definition = ResolveDefinition(buildingSnapshot.DefinitionId);
                if (definition != null) DefinitionsById[building.Id] = definition;
                if (definition == _extractorDefinition || definition == _pumpDefinition) ExtractorOutputBuffers[building.Id] = new Inventory();
                if (definition == _storageDefinition || definition == _cisternDefinition) StorageInventoriesByBuildingId[building.Id] = new Inventory();
                if (building.IsStartingShelter) Shelter = building;
            }

            foreach (var technologySnapshot in snapshot.Technologies)
            {
                var definition = TechnologyDefinitionsByResourceId.Values.FirstOrDefault(d => d.Id == technologySnapshot.Id);
                if (definition == null) continue;
                TechnologiesByResourceId[definition.TargetResourceId] =
                    TechnologySnapshotMapper.FromSnapshot(technologySnapshot, definition.TargetResourceId, definition.ProgressRequired);
            }

            TransportTasks.Clear();
            foreach (var taskSnapshot in snapshot.TransportTasks)
            {
                var sourceExists = Buildings.Any(b => b.Id == taskSnapshot.SourceBuildingId);
                var destinationExists = Buildings.Any(b => b.Id == taskSnapshot.DestinationBuildingId);
                if (!sourceExists || !destinationExists) continue;

                TransportTasks.Add(TransportTaskSnapshotMapper.FromSnapshot(taskSnapshot));
            }

            _researcherSlot = new JobSlot(Guid.NewGuid(), _researcherJobDefinition.Id, Shelter.Id);

            SelectedX = SelectedY = -1;
            LastMessage = "Partie chargée (colons et contenu des entrepôts/citernes/trésor conservés en mémoire, non couverts par le schéma actuel — cf. US6/T051).";
        }

        private BuildingDefinition ResolveDefinition(string definitionId)
        {
            if (definitionId == _shelterDefinition.Id) return _shelterDefinition;
            if (definitionId == _storageDefinition.Id) return _storageDefinition;
            if (definitionId == _cisternDefinition.Id) return _cisternDefinition;
            if (definitionId == _extractorDefinition.Id) return _extractorDefinition;
            if (definitionId == _pumpDefinition.Id) return _pumpDefinition;
            if (definitionId == _housingDefinition.Id) return _housingDefinition;
            return null;
        }

        public string ResourceDisplayName(string resourceId)
        {
            return ResourceDisplayNamesById != null && ResourceDisplayNamesById.TryGetValue(resourceId, out var name) ? name : resourceId;
        }

        public Dictionary<string, float> AggregateStorageQuantities(BuildingDefinition storageKind)
        {
            var totals = new Dictionary<string, float>();
            foreach (var kvp in DefinitionsById)
            {
                if (kvp.Value != storageKind) continue;
                if (!StorageInventoriesByBuildingId.TryGetValue(kvp.Key, out var inventory)) continue;

                foreach (var quantity in inventory.Quantities)
                    totals[quantity.Key] = totals.TryGetValue(quantity.Key, out var existing) ? existing + quantity.Value : quantity.Value;
            }

            return totals;
        }

        public string DescribeTransportStatus(BuildingInstance sourceBuilding)
        {
            var task = TransportTasks.FirstOrDefault(t => t.SourceBuildingId == sourceBuilding.Id);
            if (task == null)
                return "Transport: aucune tâche (ciblez une destination puis assignez un colon).";

            var destinationLabel = "destination introuvable";
            if (DefinitionsById.TryGetValue(task.DestinationBuildingId, out var destinationDef))
            {
                var destinationBuilding = Buildings.FirstOrDefault(b => b.Id == task.DestinationBuildingId);
                destinationLabel = destinationBuilding != null
                    ? $"{destinationDef.DisplayName} ({destinationBuilding.X},{destinationBuilding.Y})"
                    : destinationDef.DisplayName;
            }

            var phaseDuration = TryBuildTransportCycleConfig(task, out var config) ? GetConfiguredPhaseDuration(task.Phase, config) : 0f;
            return $"Transport -> {destinationLabel} : {DescribePhase(task.Phase)} ({task.PhaseProgress:0.0}/{phaseDuration:0.0}s), charge {task.CarriedQuantity:0.0} kg";
        }

        public static float GetConfiguredPhaseDuration(TransportCyclePhase phase, TransportCycleConfig config)
        {
            return phase switch
            {
                TransportCyclePhase.TravelingToSource => config.TravelToSourceDuration,
                TransportCyclePhase.Loading => config.LoadingDuration,
                TransportCyclePhase.TravelingToDestination => config.TravelToDestinationDuration,
                TransportCyclePhase.Unloading => config.UnloadingDuration,
                _ => 0f
            };
        }

        public static string DescribePhase(TransportCyclePhase phase)
        {
            return phase switch
            {
                TransportCyclePhase.TravelingToSource => "trajet vers l'extracteur/la pompe",
                TransportCyclePhase.Loading => "chargement",
                TransportCyclePhase.TravelingToDestination => "trajet retour",
                TransportCyclePhase.Unloading => "déchargement",
                _ => phase.ToString()
            };
        }
    }
}
