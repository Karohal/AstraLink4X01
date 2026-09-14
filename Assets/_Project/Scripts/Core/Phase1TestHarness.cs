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
    // Scène de test manuel pour dérouler quickstart.md scénarios 1 à 4 (US1-US4). Orchestration
    // pure (Principe II) : ce MonoBehaviour relie les services Game.<Module> déjà couverts par les
    // tests EditMode à une interface OnGUI minimale ; aucune règle métier n'est réimplémentée ici.
    //
    // Contenu : trois ressources concrètes (bois, pierre, eau) plutôt qu'un placeholder générique.
    // Bois/pierre sont extraits par un extracteur classique et stockés en entrepôt ; l'eau est
    // extraite par une pompe (constructible sur case d'eau ou nappe phréatique terrestre) et
    // stockée en citerne. Le bois est une ressource durable (gisement non épuisable).
    //
    // Limite connue : GameStateSnapshot n'a pas de section dédiée pour l'entrepôt/la citerne ni
    // pour les colons à ce stade (colons prévus en US6/T051) — Sauvegarder/Charger ne couvre donc
    // que planète/brouillard, bâtiments, technologies et transport, suffisant pour valider le
    // scénario 1 (fog of war) mais pas encore SC-010 dans son ensemble (prévu en Phase 13/T078).
    public sealed class Phase1TestHarness : MonoBehaviour
    {
        [Header("Contenu — bâtiments")]
        [SerializeField] private BuildingDefinition _shelterDefinition;
        [SerializeField] private BuildingDefinition _storageDefinition;
        [SerializeField] private BuildingDefinition _cisternDefinition;
        [SerializeField] private BuildingDefinition _extractorDefinition;
        [SerializeField] private BuildingDefinition _pumpDefinition;

        [Header("Contenu — ressources")]
        [SerializeField] private ResourceDefinition _materialsResource;
        [SerializeField] private ResourceDefinition _woodResource;
        [SerializeField] private ResourceDefinition _stoneResource;
        [SerializeField] private ResourceDefinition _waterResource;

        [Header("Contenu — technologies (une par ressource extractible)")]
        [SerializeField] private TechnologyDefinition _woodTechnologyDefinition;
        [SerializeField] private TechnologyDefinition _stoneTechnologyDefinition;
        [SerializeField] private TechnologyDefinition _waterTechnologyDefinition;

        [Header("Contenu — colons")]
        [SerializeField] private JobDefinition _researcherJobDefinition;
        [SerializeField] private EthnicityDefinition _startingEthnicity;

        [Header("Contenu — transport (US4 peaufinage)")]
        [SerializeField] private TransporterDefinition _colonistTransporterDefinition;
        [SerializeField] private TerrainSpeedCatalog _terrainSpeedCatalog;

        [Header("Génération de planète (réduite pour un test manuel lisible)")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private int _width = 20;
        [SerializeField] private int _height = 20;
        [SerializeField] private int _startingRevealRadius = 4;
        [SerializeField] private float _startingMaterials = 500f;

        // Temps de chargement/déchargement du cycle de transport (US4 peaufinage pt.2) : valeurs
        // d'équilibrage de départ, amenées à diminuer avec des améliorations de bâtiments/technologies.
        [SerializeField] private float _loadingDurationSeconds = 5f;
        [SerializeField] private float _unloadingDurationSeconds = 5f;

        private IPlanetGenerationService _planetGenerationService;
        private IFogOfWarService _fogOfWarService;
        private IColonistIdentityService _identityService;
        private IAssignmentService _assignmentService;
        private IGameBootstrapService _bootstrapService;
        private IBuildingPlacementService _placementService;
        private IConstructionSiteService _constructionSiteService;
        private IExtractionService _extractionService;
        private IResearchService _researchService;
        private ResearcherJobBinding _researcherJobBinding;
        private ITransportService _transportService;
        private ISimulationClock _clock;
        private ISaveLoadService _saveLoadService;
        private FogOfWarConstructionListener _fogListener;

        private Planet _planet;
        private BuildingInstance _shelter;
        private readonly List<BuildingInstance> _buildings = new List<BuildingInstance>();
        private readonly Dictionary<Guid, BuildingDefinition> _definitionsById = new Dictionary<Guid, BuildingDefinition>();
        private readonly Dictionary<Guid, Inventory> _extractorOutputBuffers = new Dictionary<Guid, Inventory>();
        private readonly List<Colonist> _colonists = new List<Colonist>();
        private readonly List<TransportTask> _transportTasks = new List<TransportTask>();
        private Inventory _warehouse; // trésor de matériaux de construction (FR-005/FR-006), pas un site de stockage
        private readonly Dictionary<Guid, Inventory> _storageInventoriesByBuildingId = new Dictionary<Guid, Inventory>(); // un Inventory par entrepôt/citerne réellement construit(e)
        private Guid? _selectedDestinationBuildingId; // entrepôt/citerne ciblé(e) pour le prochain transport assigné

        private Dictionary<string, TechnologyDefinition> _technologyDefinitionsByResourceId;
        private Dictionary<string, Technology> _technologiesByResourceId;
        private Dictionary<string, string> _resourceDisplayNamesById;
        private Dictionary<string, ResourceDefinition> _resourceDefinitionsById;
        private string _selectedResearchResourceId;
        private JobSlot _researcherSlot;
        private readonly Dictionary<Guid, JobSlot> _operatorSlotsByBuildingId = new Dictionary<Guid, JobSlot>();
        private bool _isReady;

        // Ressources de base (bois/pierre/eau) : pas de verrou technologique à la construction
        // (contrairement aux futurs minerais avancés) — seul un gisement compatible est requis.
        private HashSet<string> _baseResourceIds;

        private Vector2 _gridScroll;
        private Vector2 _colonistScroll;
        private int _selectedX = -1;
        private int _selectedY = -1;
        private BuildingDefinition _selectedBuildToPlace;
        private string _lastMessage = string.Empty;
        private string _saveSlotName = "phase1-demo";
        private GUIStyle _titleStyle;

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

        private void Awake()
        {
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
            _clock = new SimulationClock();
            _clock.OnTick += OnSimulationTick;
            _saveLoadService = new SaveLoadService(Application.persistentDataPath);
        }

        // Awake() a lieu dès l'ajout du composant (y compris hors Play Mode, via un outil
        // éditeur) : la partie ne démarre qu'à Start(), qui n'est appelé qu'à l'entrée en Play
        // Mode, une fois les références de contenu garanties assignées.
        private void Start()
        {
            NewGame();
        }

        private void Update()
        {
            _clock?.Tick(Time.deltaTime);
        }

        // Assignation directe (hors Inspector) pour l'outil éditeur Phase1DemoSceneBuilder : plus
        // fiable qu'un SerializedObject sur un composant tout juste ajouté à une scène non encore
        // sauvegardée.
        public void ConfigureContentForEditorTooling(
            BuildingDefinition shelterDefinition,
            BuildingDefinition storageDefinition,
            BuildingDefinition cisternDefinition,
            BuildingDefinition extractorDefinition,
            BuildingDefinition pumpDefinition,
            ResourceDefinition materialsResource,
            ResourceDefinition woodResource,
            ResourceDefinition stoneResource,
            ResourceDefinition waterResource,
            TechnologyDefinition woodTechnologyDefinition,
            TechnologyDefinition stoneTechnologyDefinition,
            TechnologyDefinition waterTechnologyDefinition,
            JobDefinition researcherJobDefinition,
            EthnicityDefinition startingEthnicity,
            TransporterDefinition colonistTransporterDefinition,
            TerrainSpeedCatalog terrainSpeedCatalog)
        {
            _shelterDefinition = shelterDefinition;
            _storageDefinition = storageDefinition;
            _cisternDefinition = cisternDefinition;
            _extractorDefinition = extractorDefinition;
            _pumpDefinition = pumpDefinition;
            _materialsResource = materialsResource;
            _woodResource = woodResource;
            _stoneResource = stoneResource;
            _waterResource = waterResource;
            _woodTechnologyDefinition = woodTechnologyDefinition;
            _stoneTechnologyDefinition = stoneTechnologyDefinition;
            _waterTechnologyDefinition = waterTechnologyDefinition;
            _researcherJobDefinition = researcherJobDefinition;
            _startingEthnicity = startingEthnicity;
            _colonistTransporterDefinition = colonistTransporterDefinition;
            _terrainSpeedCatalog = terrainSpeedCatalog;
        }

        private bool HasRequiredContent()
        {
            return _shelterDefinition != null && _storageDefinition != null && _cisternDefinition != null &&
                   _extractorDefinition != null && _pumpDefinition != null &&
                   _materialsResource != null && _woodResource != null && _stoneResource != null && _waterResource != null &&
                   _woodTechnologyDefinition != null && _stoneTechnologyDefinition != null && _waterTechnologyDefinition != null &&
                   _researcherJobDefinition != null && _startingEthnicity != null &&
                   _colonistTransporterDefinition != null && _terrainSpeedCatalog != null;
        }

        private void NewGame()
        {
            _isReady = false;

            if (!HasRequiredContent())
            {
                Debug.LogError("Phase1TestHarness : références de contenu manquantes ou scène obsolète (champs renommés depuis la dernière génération). Régénérez la scène via le menu AstraLink > Build Phase 1 Demo Scene.");
                return;
            }

            _technologyDefinitionsByResourceId = new Dictionary<string, TechnologyDefinition>
            {
                [_woodTechnologyDefinition.TargetResourceId] = _woodTechnologyDefinition,
                [_stoneTechnologyDefinition.TargetResourceId] = _stoneTechnologyDefinition,
                [_waterTechnologyDefinition.TargetResourceId] = _waterTechnologyDefinition
            };

            _resourceDisplayNamesById = new Dictionary<string, string>
            {
                [_materialsResource.Id] = _materialsResource.DisplayName,
                [_woodResource.Id] = _woodResource.DisplayName,
                [_stoneResource.Id] = _stoneResource.DisplayName,
                [_waterResource.Id] = _waterResource.DisplayName
            };

            _resourceDefinitionsById = new Dictionary<string, ResourceDefinition>
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
                StartingJobIdsToRandomize = new[] { _researcherJobDefinition.Id, JobDefinition.MinerJobId }
            };

            var result = _bootstrapService.Bootstrap(config);
            _planet = result.Planet;
            _shelter = result.StartingShelter;

            _colonists.Clear();
            _colonists.AddRange(result.Colonists);

            _buildings.Clear();
            _buildings.Add(_shelter);
            _definitionsById.Clear();
            _definitionsById[_shelter.Id] = _shelterDefinition;

            _extractorOutputBuffers.Clear();
            _transportTasks.Clear();
            _operatorSlotsByBuildingId.Clear();
            _storageInventoriesByBuildingId.Clear();
            _selectedDestinationBuildingId = null;

            _warehouse = new Inventory();
            _warehouse.SetQuantity(_materialsResource.Id, _startingMaterials);

            _technologiesByResourceId = new Dictionary<string, Technology>
            {
                [_woodResource.Id] = new Technology(_woodTechnologyDefinition.Id, _woodResource.Id, _woodTechnologyDefinition.ProgressRequired),
                [_stoneResource.Id] = new Technology(_stoneTechnologyDefinition.Id, _stoneResource.Id, _stoneTechnologyDefinition.ProgressRequired),
                [_waterResource.Id] = new Technology(_waterTechnologyDefinition.Id, _waterResource.Id, _waterTechnologyDefinition.ProgressRequired)
            };
            _selectedResearchResourceId = _woodResource.Id;

            _researcherSlot = new JobSlot(Guid.NewGuid(), _researcherJobDefinition.Id, _shelter.Id);

            _selectedX = _selectedY = -1;
            _selectedBuildToPlace = null;
            _lastMessage = "Nouvelle partie démarrée.";
            _isReady = true;
        }

        private void OnSimulationTick(float deltaSimTime)
        {
            if (!_isReady) return;

            foreach (var building in _buildings)
            {
                if (building.State != BuildingState.UnderConstruction) continue;
                if (!_definitionsById.TryGetValue(building.Id, out var definition)) continue;
                _constructionSiteService.Tick(building, definition, _planet, _colonists, deltaSimTime);
            }

            foreach (var building in _buildings)
            {
                if (!_definitionsById.TryGetValue(building.Id, out var definition)) continue;
                if (definition != _extractorDefinition && definition != _pumpDefinition) continue;
                if (!building.IsOperational) continue;
                if (!_planet.TryGetZone(building.X, building.Y, out var zone) || zone.Deposit == null) continue;

                // Bascule Locked/PretPourExtracteur -> EnExtraction dès que le bâtiment est
                // opérationnel (BeginExtraction() enchaîne les deux transitions) : aucune
                // dépendance à un verrou technologique ici, cf. TryBuild pour la validation de
                // construction (retirée pour les ressources de base, FR-009 conservé pour le reste).
                if (zone.Deposit.State != DepositState.Extracting && zone.Deposit.State != DepositState.Depleted)
                    _extractionService.BeginExtraction(zone.Deposit);

                if (!_extractorOutputBuffers.TryGetValue(building.Id, out var buffer))
                {
                    buffer = new Inventory();
                    _extractorOutputBuffers[building.Id] = buffer;
                }

                var workers = GetAssignedWorkers(building.Id);
                var buildingYield = ExtractorYield.ComputeBuildingYield(workers, JobDefinition.MinerJobId);

                _extractionService.Tick(zone.Deposit, building, buffer, deltaSimTime, buildingYield);
            }

            if (_technologiesByResourceId.TryGetValue(_selectedResearchResourceId, out var activeTechnology))
                _researcherJobBinding.Tick(_colonists, new[] { _researcherSlot }, activeTechnology, deltaSimTime);

            foreach (var task in _transportTasks)
            {
                if (!_extractorOutputBuffers.TryGetValue(task.SourceBuildingId, out var source)) continue;
                if (!_storageInventoriesByBuildingId.TryGetValue(task.DestinationBuildingId, out var destination)) continue;
                if (!TryBuildTransportCycleConfig(task, out var config)) continue;

                _transportService.Tick(task, source, destination, config, deltaSimTime);
            }
        }

        private void TryBuild(int x, int y)
        {
            if (_selectedBuildToPlace == null)
            {
                _lastMessage = "Sélectionnez un type de bâtiment à construire.";
                return;
            }

            if (!_planet.TryGetZone(x, y, out var zone))
            {
                _lastMessage = "Zone invalide.";
                return;
            }

            var isExtractorLike = _selectedBuildToPlace == _extractorDefinition || _selectedBuildToPlace == _pumpDefinition;
            if (isExtractorLike)
            {
                if (zone.Deposit == null)
                {
                    _lastMessage = "Aucun gisement sur cette zone.";
                    return;
                }

                var isWaterDeposit = zone.Deposit.ResourceId == _waterResource.Id;
                if (_selectedBuildToPlace == _pumpDefinition && !isWaterDeposit)
                {
                    _lastMessage = "Une pompe ne peut être construite que sur une case d'eau ou une nappe phréatique.";
                    return;
                }
                if (_selectedBuildToPlace == _extractorDefinition && isWaterDeposit)
                {
                    _lastMessage = "L'eau se pompe : utilisez une pompe, pas un extracteur.";
                    return;
                }

                // Bois/pierre/eau sont des ressources de base : aucun verrou technologique requis
                // pour construire dessus. Un futur minerai avancé resterait soumis à FR-009.
                if (!_baseResourceIds.Contains(zone.Deposit.ResourceId))
                {
                    if (!_technologiesByResourceId.TryGetValue(zone.Deposit.ResourceId, out var technology) ||
                        !_extractionService.CanBuildExtractor(zone.Deposit, technology))
                    {
                        _lastMessage = $"Technologie d'extraction non débloquée pour « {zone.Deposit.ResourceId} » (FR-009).";
                        return;
                    }
                }
            }

            if (!_placementService.CanBuild(_planet, _selectedBuildToPlace, x, y, _warehouse, out var missing))
            {
                _lastMessage = "Construction refusée, ressources manquantes : " + string.Join(", ", missing);
                return;
            }

            var building = _placementService.Build(_planet, _selectedBuildToPlace, x, y, _warehouse);
            _buildings.Add(building);
            _definitionsById[building.Id] = _selectedBuildToPlace;

            if (isExtractorLike)
                _extractorOutputBuffers[building.Id] = new Inventory();

            if (_selectedBuildToPlace == _storageDefinition || _selectedBuildToPlace == _cisternDefinition)
                _storageInventoriesByBuildingId[building.Id] = new Inventory();

            _lastMessage = $"{_selectedBuildToPlace.DisplayName} en chantier à ({x},{y}). Assignez un colon pour démarrer le chantier.";
        }

        private void AssignToConstruction(Colonist colonist)
        {
            var building = GetSelectedBuilding();
            if (building == null || building.State != BuildingState.UnderConstruction)
            {
                _lastMessage = "Sélectionnez d'abord un chantier sur la grille.";
                return;
            }

            _assignmentService.AssignManually(colonist, new AssignableTarget(building.Id, AssignmentType.Construction));
            _lastMessage = $"{colonist.Name} assigné au chantier.";
        }

        private void AssignToResearch(Colonist colonist)
        {
            _assignmentService.AssignManually(colonist, new AssignableTarget(_researcherSlot.Id, AssignmentType.Job));
            _lastMessage = $"{colonist.Name} assigné à la recherche (« {_selectedResearchResourceId} »).";
        }

        // Poste d'exploitation d'un extracteur/pompe : jusqu'à ExtractorYield.MaxWorkerSlots colons
        // simultanés (un JobSlot partagé par bâtiment, comme le poste de chercheur).
        private void AssignToExtraction(Colonist colonist)
        {
            var building = GetSelectedBuilding();
            if (building == null || !_definitionsById.TryGetValue(building.Id, out var def) ||
                (def != _extractorDefinition && def != _pumpDefinition))
            {
                _lastMessage = "Sélectionnez un extracteur ou une pompe pour y affecter un travailleur.";
                return;
            }

            if (!_operatorSlotsByBuildingId.TryGetValue(building.Id, out var slot))
            {
                slot = new JobSlot(Guid.NewGuid(), JobDefinition.MinerJobId, building.Id);
                _operatorSlotsByBuildingId[building.Id] = slot;
            }

            var currentWorkerCount = GetAssignedWorkers(building.Id).Count;
            if (currentWorkerCount >= ExtractorYield.MaxWorkerSlots)
            {
                _lastMessage = $"Effectif maximum atteint ({ExtractorYield.MaxWorkerSlots} postes).";
                return;
            }

            _assignmentService.AssignManually(colonist, new AssignableTarget(slot.Id, AssignmentType.Job));
            _lastMessage = $"{colonist.Name} affecté à l'exploitation ({currentWorkerCount + 1}/{ExtractorYield.MaxWorkerSlots}).";
        }

        private List<Colonist> GetAssignedWorkers(Guid buildingId)
        {
            if (!_operatorSlotsByBuildingId.TryGetValue(buildingId, out var slot)) return new List<Colonist>();
            return _colonists.Where(c =>
                c.CurrentAssignment != null &&
                c.CurrentAssignment.Type == AssignmentType.Job &&
                c.CurrentAssignment.TargetId == slot.Id).ToList();
        }

        private void AssignToTransport(Colonist colonist)
        {
            var building = GetSelectedBuilding();
            if (building == null || !_definitionsById.TryGetValue(building.Id, out var def) ||
                (def != _extractorDefinition && def != _pumpDefinition))
            {
                _lastMessage = "Sélectionnez un extracteur ou une pompe sur la grille pour organiser son transport.";
                return;
            }

            if (!_planet.TryGetZone(building.X, building.Y, out var zone) || zone.Deposit == null)
            {
                _lastMessage = "Gisement introuvable pour ce bâtiment.";
                return;
            }

            // Corrige un bug où toute tâche de transport était systématiquement liée à l'abri de
            // secours, indépendamment de l'entrepôt/citerne réellement construit(e) et ciblé(e) par
            // le joueur (bouton « Cibler comme destination » sur le bâtiment de stockage voulu).
            if (!_selectedDestinationBuildingId.HasValue ||
                !TryGetStorageBuilding(_selectedDestinationBuildingId.Value, out var destinationBuilding, out var destinationDef))
            {
                _lastMessage = "Sélectionnez d'abord une destination : bouton « Cibler comme destination » sur un entrepôt/une citerne construit(e) et opérationnel(le).";
                return;
            }

            var isWaterResource = zone.Deposit.ResourceId == _waterResource.Id;
            var destinationAcceptsResource = isWaterResource ? destinationDef == _cisternDefinition : destinationDef == _storageDefinition;
            if (!destinationAcceptsResource)
            {
                _lastMessage = $"{destinationDef.DisplayName} ne peut pas recevoir « {ResourceDisplayName(zone.Deposit.ResourceId)} ».";
                return;
            }

            var task = _transportTasks.FirstOrDefault(t => t.SourceBuildingId == building.Id && t.DestinationBuildingId == destinationBuilding.Id);
            if (task == null)
            {
                task = _transportService.Assign(building.Id, destinationBuilding.Id, zone.Deposit.ResourceId, colonist.Id, null);
                _transportTasks.Add(task);
            }
            else
            {
                task.AssignedColonistId = colonist.Id;
            }

            _assignmentService.AssignManually(colonist, new AssignableTarget(task.Id, AssignmentType.Transport));
            _lastMessage = $"{colonist.Name} assigné au transport vers {destinationDef.DisplayName} ({destinationBuilding.X},{destinationBuilding.Y}).";
        }

        private bool TryGetStorageBuilding(Guid buildingId, out BuildingInstance building, out BuildingDefinition definition)
        {
            building = _buildings.FirstOrDefault(b => b.Id == buildingId);
            definition = null;
            if (building == null || building.State != BuildingState.Operational) { building = null; return false; }
            if (!_definitionsById.TryGetValue(buildingId, out definition)) return false;
            if (definition != _storageDefinition && definition != _cisternDefinition) { definition = null; return false; }
            return true;
        }

        // Assemble la configuration du cycle de transport (trajets dépendants de la distance et du
        // terrain moyen traversé, chargement/déchargement fixes, charge maximale masse/volume) pour
        // le trajet source -> destination réel de cette tâche.
        private bool TryBuildTransportCycleConfig(TransportTask task, out TransportCycleConfig config)
        {
            config = default;
            var sourceBuilding = _buildings.FirstOrDefault(b => b.Id == task.SourceBuildingId);
            var destinationBuilding = _buildings.FirstOrDefault(b => b.Id == task.DestinationBuildingId);
            if (sourceBuilding == null || destinationBuilding == null) return false;

            config = BuildTransportCycleConfig(task.ResourceId, sourceBuilding.X, sourceBuilding.Y, destinationBuilding.X, destinationBuilding.Y);
            return true;
        }

        private TransportCycleConfig BuildTransportCycleConfig(string resourceId, int sourceX, int sourceY, int destinationX, int destinationY)
        {
            _resourceDefinitionsById.TryGetValue(resourceId, out var resourceDefinition);
            var maxLoadKg = TransportCapacityCalculator.ComputeMaxLoadKg(resourceDefinition, _colonistTransporterDefinition);

            var distanceTiles = TravelTimeCalculator.ComputeDistanceTiles(sourceX, sourceY, destinationX, destinationY);
            var speedModifier = TerrainRouting.ComputeAverageSpeedModifier(_planet, sourceX, sourceY, destinationX, destinationY, _terrainSpeedCatalog);
            var travelDuration = TravelTimeCalculator.ComputeTravelDuration(distanceTiles, _colonistTransporterDefinition.SpeedTilesPerSecond, speedModifier);

            // Même distance/terrain à l'aller (dépôt -> source) et au retour (source -> dépôt) : une
            // seule durée de trajet calculée, réutilisée pour les deux jambes du cycle.
            return new TransportCycleConfig(travelDuration, _loadingDurationSeconds, travelDuration, _unloadingDurationSeconds, maxLoadKg);
        }

        private void Unassign(Colonist colonist)
        {
            _assignmentService.Unassign(colonist);
            _lastMessage = $"{colonist.Name} libéré.";
        }

        // Recyclage (typiquement un extracteur/une pompe dont le gisement est épuisé) : détruit le
        // bâtiment sélectionné, rembourse une fraction de son coût (BuildingDefinition.RecycleRefundRatio)
        // et nettoie les références qui le ciblaient (chantier en cours, transport).
        private void RecycleSelectedBuilding()
        {
            var building = GetSelectedBuilding();
            if (building == null || building.IsStartingShelter)
            {
                _lastMessage = "Sélectionnez un bâtiment recyclable (pas l'abri de secours).";
                return;
            }

            if (!_definitionsById.TryGetValue(building.Id, out var definition))
            {
                _lastMessage = "Définition introuvable pour ce bâtiment.";
                return;
            }

            _placementService.Recycle(_planet, building, definition, _warehouse);
            RemoveBuildingBookkeeping(building);

            _selectedX = _selectedY = -1;
            _lastMessage = $"{definition.DisplayName} recyclé : {definition.RecycleRefundRatio:P0} du coût remboursé.";
        }

        // Annule un chantier tout juste posé (aucun colon n'y a encore travaillé) : contrairement
        // au recyclage, remboursement intégral puisque rien n'a réellement été construit. Le
        // bâtiment reste visible sur la grille tant que ce bouton n'a pas été utilisé — le retirer
        // de la carte est précisément ce que corrige cette méthode (le placement seul ne suffisait
        // pas à le faire disparaître).
        private void CancelSelectedConstruction()
        {
            var building = GetSelectedBuilding();
            if (building == null || building.IsStartingShelter)
            {
                _lastMessage = "Sélectionnez un chantier annulable.";
                return;
            }

            if (!_definitionsById.TryGetValue(building.Id, out var definition))
            {
                _lastMessage = "Définition introuvable pour ce bâtiment.";
                return;
            }

            if (!_placementService.TryCancelConstruction(_planet, building, definition, _warehouse))
            {
                _lastMessage = "Ce chantier a déjà commencé : utilisez Recycler (remboursement partiel) à la place.";
                return;
            }

            RemoveBuildingBookkeeping(building);

            _selectedX = _selectedY = -1;
            _lastMessage = $"{definition.DisplayName} annulé, ressources intégralement remboursées.";
        }

        // Retire un bâtiment détruit (recyclé ou chantier annulé) de tout l'état en mémoire du
        // harness : liste des bâtiments, buffer d'extraction, tâches de transport qui en
        // dépendaient, poste d'exploitation, et libère les colons qui le ciblaient. La suppression
        // de la zone elle-même (Zone.BuildingId) est déjà faite par le service appelant
        // (Recycle/TryCancelConstruction).
        private void RemoveBuildingBookkeeping(BuildingInstance building)
        {
            _buildings.Remove(building);
            _definitionsById.Remove(building.Id);
            _extractorOutputBuffers.Remove(building.Id);
            _storageInventoriesByBuildingId.Remove(building.Id);
            if (_selectedDestinationBuildingId == building.Id) _selectedDestinationBuildingId = null;

            var staleTaskIds = _transportTasks
                .Where(t => t.SourceBuildingId == building.Id || t.DestinationBuildingId == building.Id)
                .Select(t => t.Id).ToList();
            _transportTasks.RemoveAll(t => staleTaskIds.Contains(t.Id));

            _operatorSlotsByBuildingId.TryGetValue(building.Id, out var staleOperatorSlot);
            _operatorSlotsByBuildingId.Remove(building.Id);

            foreach (var colonist in _colonists)
            {
                if (colonist.CurrentAssignment == null) continue;
                var targetsRemovedBuilding =
                    (colonist.CurrentAssignment.Type == AssignmentType.Construction && colonist.CurrentAssignment.TargetId == building.Id) ||
                    (colonist.CurrentAssignment.Type == AssignmentType.Transport && staleTaskIds.Contains(colonist.CurrentAssignment.TargetId)) ||
                    (staleOperatorSlot != null && colonist.CurrentAssignment.Type == AssignmentType.Job && colonist.CurrentAssignment.TargetId == staleOperatorSlot.Id);
                if (targetsRemovedBuilding) _assignmentService.Unassign(colonist);
            }
        }

        private BuildingInstance GetSelectedBuilding()
        {
            if (_selectedX < 0 || !_planet.TryGetZone(_selectedX, _selectedY, out var zone) || !zone.BuildingId.HasValue)
                return null;

            return _buildings.FirstOrDefault(b => b.Id == zone.BuildingId.Value);
        }

        private void SaveGame()
        {
            var snapshot = new GameStateSnapshot
            {
                Planet = PlanetSnapshotMapper.ToSnapshot(_planet)
            };

            foreach (var building in _buildings)
                snapshot.Buildings.Add(BuildingSnapshotMapper.ToSnapshot(building));

            foreach (var technology in _technologiesByResourceId.Values)
                snapshot.Technologies.Add(TechnologySnapshotMapper.ToSnapshot(technology));

            foreach (var task in _transportTasks)
                snapshot.TransportTasks.Add(TransportTaskSnapshotMapper.ToSnapshot(task));

            _saveLoadService.Save(snapshot, _saveSlotName);
            _lastMessage = $"Partie sauvegardée ({_saveSlotName}).";
        }

        private void LoadGame()
        {
            GameStateSnapshot snapshot;
            try
            {
                snapshot = _saveLoadService.Load(_saveSlotName);
            }
            catch (UnsupportedSaveVersionException ex)
            {
                _lastMessage = ex.Message;
                return;
            }

            if (snapshot == null)
            {
                _lastMessage = "Aucune sauvegarde trouvée pour ce slot.";
                return;
            }

            _planet = PlanetSnapshotMapper.FromSnapshot(snapshot.Planet);

            _buildings.Clear();
            _definitionsById.Clear();
            _extractorOutputBuffers.Clear();
            _storageInventoriesByBuildingId.Clear();
            _selectedDestinationBuildingId = null;
            foreach (var buildingSnapshot in snapshot.Buildings)
            {
                var building = BuildingSnapshotMapper.FromSnapshot(buildingSnapshot);
                _buildings.Add(building);

                var definition = ResolveDefinition(buildingSnapshot.DefinitionId);
                if (definition != null) _definitionsById[building.Id] = definition;
                if (definition == _extractorDefinition || definition == _pumpDefinition) _extractorOutputBuffers[building.Id] = new Inventory();
                if (definition == _storageDefinition || definition == _cisternDefinition) _storageInventoriesByBuildingId[building.Id] = new Inventory();
                if (building.IsStartingShelter) _shelter = building;
            }

            foreach (var technologySnapshot in snapshot.Technologies)
            {
                var definition = _technologyDefinitionsByResourceId.Values.FirstOrDefault(d => d.Id == technologySnapshot.Id);
                if (definition == null) continue;
                _technologiesByResourceId[definition.TargetResourceId] =
                    TechnologySnapshotMapper.FromSnapshot(technologySnapshot, definition.TargetResourceId, definition.ProgressRequired);
            }

            _transportTasks.Clear();
            foreach (var taskSnapshot in snapshot.TransportTasks)
            {
                // Une tâche dont la source ou la destination n'existe plus (bâtiment recyclé entre
                // la sauvegarde et le chargement, par ex.) n'a plus de sens : elle est abandonnée
                // plutôt que rejouée avec une configuration de repli arbitraire.
                var sourceExists = _buildings.Any(b => b.Id == taskSnapshot.SourceBuildingId);
                var destinationExists = _buildings.Any(b => b.Id == taskSnapshot.DestinationBuildingId);
                if (!sourceExists || !destinationExists) continue;

                _transportTasks.Add(TransportTaskSnapshotMapper.FromSnapshot(taskSnapshot));
            }

            _researcherSlot = new JobSlot(Guid.NewGuid(), _researcherJobDefinition.Id, _shelter.Id);

            _selectedX = _selectedY = -1;
            _lastMessage = "Partie chargée (colons et contenu des entrepôts/citernes/trésor conservés en mémoire, non couverts par le schéma actuel — cf. US6/T051).";
        }

        private BuildingDefinition ResolveDefinition(string definitionId)
        {
            if (definitionId == _shelterDefinition.Id) return _shelterDefinition;
            if (definitionId == _storageDefinition.Id) return _storageDefinition;
            if (definitionId == _cisternDefinition.Id) return _cisternDefinition;
            if (definitionId == _extractorDefinition.Id) return _extractorDefinition;
            if (definitionId == _pumpDefinition.Id) return _pumpDefinition;
            return null;
        }

        private void OnGUI()
        {
            if (_planet == null)
            {
                GUILayout.Label("Phase1TestHarness : contenu manquant, voir la Console (AstraLink > Build Phase 1 Demo Scene).");
                return;
            }

            _titleStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };

            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height - 20), GUI.skin.box);
            DrawControlPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(380, 10, 420, Screen.height - 20), GUI.skin.box);
            DrawGrid();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(810, 10, 320, Screen.height - 20), GUI.skin.box);
            DrawColonists();
            GUILayout.EndArea();
        }

        private void DrawControlPanel()
        {
            GUILayout.Label("Phase 1 — quickstart.md scénarios 1-4", _titleStyle);
            GUILayout.Space(6);

            GUILayout.Label($"Simulation : x{_clock.SpeedMultiplier:0.#} {(_clock.IsPaused ? "(pause)" : "")}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_clock.IsPaused ? "Reprendre" : "Pause"))
            {
                if (_clock.IsPaused) _clock.Resume(); else _clock.Pause();
            }
            if (GUILayout.Button("x1")) _clock.SetSpeed(1f);
            if (GUILayout.Button("x2")) _clock.SetSpeed(2f);
            if (GUILayout.Button("x3")) _clock.SetSpeed(3f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Trésor de matériaux (construction)");
            foreach (var kvp in _warehouse.Quantities)
                GUILayout.Label($"  {ResourceDisplayName(kvp.Key)} : {kvp.Value:0.0}");

            GUILayout.Label("Entrepôts construits (solide, total)");
            foreach (var kvp in AggregateStorageQuantities(_storageDefinition))
                GUILayout.Label($"  {ResourceDisplayName(kvp.Key)} : {kvp.Value:0.0}");
            GUILayout.Label("Citernes construites (eau, total)");
            foreach (var kvp in AggregateStorageQuantities(_cisternDefinition))
                GUILayout.Label($"  {ResourceDisplayName(kvp.Key)} : {kvp.Value:0.0}");

            GUILayout.Space(10);
            GUILayout.Label("Recherche — cible active (un chercheur à la fois) :");
            GUILayout.BeginHorizontal();
            DrawResearchTargetButton(_woodResource.Id, "Bois");
            DrawResearchTargetButton(_stoneResource.Id, "Pierre");
            DrawResearchTargetButton(_waterResource.Id, "Eau");
            GUILayout.EndHorizontal();
            foreach (var kvp in _technologiesByResourceId)
            {
                var marker = kvp.Key == _selectedResearchResourceId ? "▶ " : "  ";
                GUILayout.Label($"{marker}{ResourceDisplayName(kvp.Key)} : {kvp.Value.ProgressCurrent:0.0}/{kvp.Value.ProgressRequired}{(kvp.Value.IsUnlocked ? " (débloquée)" : "")}");
            }

            GUILayout.Space(10);
            GUILayout.Label("Construire (puis cliquer une zone révélée à droite) :");
            GUILayout.BeginHorizontal();
            DrawBuildButton(_storageDefinition);
            DrawBuildButton(_cisternDefinition);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawBuildButton(_extractorDefinition);
            DrawBuildButton(_pumpDefinition);
            GUILayout.EndHorizontal();
            if (_selectedBuildToPlace != null && GUILayout.Button("Annuler la sélection"))
                CancelBuildSelection();

            if (_selectedX >= 0 && _planet.TryGetZone(_selectedX, _selectedY, out var zone))
            {
                GUILayout.Space(10);
                GUILayout.Label($"Zone sélectionnée : ({_selectedX},{_selectedY})");
                GUILayout.Label($"  Terrain: {zone.Terrain} | constructible: {zone.IsBuildable} | révélée: {zone.IsRevealed}");

                if (zone.Deposit != null)
                {
                    var remaining = zone.Deposit.IsInfinite ? "illimité" : $"{zone.Deposit.RemainingQuantity:0.0} restant";
                    GUILayout.Label($"  Gisement: {ResourceDisplayName(zone.Deposit.ResourceId)} ({remaining}, {zone.Deposit.State})");
                }

                if (zone.BuildingId.HasValue)
                {
                    var building = _buildings.FirstOrDefault(b => b.Id == zone.BuildingId.Value);
                    if (building != null && _definitionsById.TryGetValue(building.Id, out var def))
                    {
                        GUILayout.Label($"  Bâtiment: {def.DisplayName} — {building.State} ({building.ConstructionProgress:0.0}/{def.ConstructionDuration})");
                        if (def == _extractorDefinition || def == _pumpDefinition)
                        {
                            var workers = GetAssignedWorkers(building.Id);
                            var buildingYield = ExtractorYield.ComputeBuildingYield(workers, JobDefinition.MinerJobId);
                            GUILayout.Label($"    Effectif: {workers.Count}/{ExtractorYield.MaxWorkerSlots} — rendement: {buildingYield:P0}");
                            if (_extractorOutputBuffers.TryGetValue(building.Id, out var buffer))
                            {
                                foreach (var kvp in buffer.Quantities)
                                    GUILayout.Label($"    buffer: {ResourceDisplayName(kvp.Key)} = {kvp.Value:0.0}");
                            }

                            DrawTransportStatus(building);
                        }

                        if (def == _storageDefinition || def == _cisternDefinition)
                        {
                            if (_storageInventoriesByBuildingId.TryGetValue(building.Id, out var storageInventory))
                            {
                                GUILayout.Label("    Contenu :");
                                foreach (var kvp in storageInventory.Quantities)
                                    GUILayout.Label($"      {ResourceDisplayName(kvp.Key)} : {kvp.Value:0.0}");
                            }

                            if (building.State == BuildingState.Operational)
                            {
                                var isCurrentDestination = _selectedDestinationBuildingId == building.Id;
                                if (GUILayout.Button(isCurrentDestination ? "Destination ciblée ✓" : "Cibler comme destination"))
                                    _selectedDestinationBuildingId = building.Id;
                            }
                        }

                        if (!building.IsStartingShelter)
                        {
                            var chantierNonCommence = building.State == BuildingState.UnderConstruction && building.ConstructionProgress <= 0f;
                            if (chantierNonCommence)
                            {
                                if (GUILayout.Button("Annuler le chantier (remboursement intégral)"))
                                    CancelSelectedConstruction();
                            }
                            else if (GUILayout.Button($"Recycler ({def.RecycleRefundRatio:P0} remboursé)"))
                            {
                                RecycleSelectedBuilding();
                            }
                        }
                    }
                }

                if (GUILayout.Button("Construire ici"))
                    TryBuild(_selectedX, _selectedY);
            }

            GUILayout.Space(10);
            GUILayout.Label(_lastMessage);

            GUILayout.FlexibleSpace();
            GUILayout.Label("Sauvegarde : planète/fog, bâtiments, technologies, transport uniquement (pas colons/stocks, cf. US6).");
            _saveSlotName = GUILayout.TextField(_saveSlotName);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sauvegarder")) SaveGame();
            if (GUILayout.Button("Charger")) LoadGame();
            if (GUILayout.Button("Nouvelle partie")) NewGame();
            GUILayout.EndHorizontal();
        }

        // Affiche le cycle de transport en cours pour ce bâtiment source (extracteur/pompe), le cas
        // échéant : phase actuelle (trajet aller/chargement/trajet retour/déchargement), progression
        // dans la phase, et destination réellement ciblée (corrige le bug où le transport semblait
        // toujours aboutir au même endroit indépendamment du bâtiment construit).
        private void DrawTransportStatus(BuildingInstance sourceBuilding)
        {
            var task = _transportTasks.FirstOrDefault(t => t.SourceBuildingId == sourceBuilding.Id);
            if (task == null)
            {
                GUILayout.Label("    Transport: aucune tâche (ciblez une destination puis assignez un colon).");
                return;
            }

            var destinationLabel = "destination introuvable";
            if (_definitionsById.TryGetValue(task.DestinationBuildingId, out var destinationDef))
            {
                var destinationBuilding = _buildings.FirstOrDefault(b => b.Id == task.DestinationBuildingId);
                destinationLabel = destinationBuilding != null
                    ? $"{destinationDef.DisplayName} ({destinationBuilding.X},{destinationBuilding.Y})"
                    : destinationDef.DisplayName;
            }

            var phaseDuration = TryBuildTransportCycleConfig(task, out var config) ? GetConfiguredPhaseDuration(task.Phase, config) : 0f;
            GUILayout.Label($"    Transport -> {destinationLabel} : {DescribePhase(task.Phase)} ({task.PhaseProgress:0.0}/{phaseDuration:0.0}s), charge {task.CarriedQuantity:0.0} kg");
        }

        private static float GetConfiguredPhaseDuration(TransportCyclePhase phase, TransportCycleConfig config)
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

        private static string DescribePhase(TransportCyclePhase phase)
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

        private Dictionary<string, float> AggregateStorageQuantities(BuildingDefinition storageKind)
        {
            var totals = new Dictionary<string, float>();
            foreach (var kvp in _definitionsById)
            {
                if (kvp.Value != storageKind) continue;
                if (!_storageInventoriesByBuildingId.TryGetValue(kvp.Key, out var inventory)) continue;

                foreach (var quantity in inventory.Quantities)
                    totals[quantity.Key] = totals.TryGetValue(quantity.Key, out var existing) ? existing + quantity.Value : quantity.Value;
            }

            return totals;
        }

        private string ResourceDisplayName(string resourceId)
        {
            return _resourceDisplayNamesById != null && _resourceDisplayNamesById.TryGetValue(resourceId, out var name) ? name : resourceId;
        }

        private void DrawResearchTargetButton(string resourceId, string label)
        {
            var marker = resourceId == _selectedResearchResourceId ? " ✓" : "";
            if (GUILayout.Button(label + marker))
                _selectedResearchResourceId = resourceId;
        }

        private void DrawBuildButton(BuildingDefinition definition)
        {
            var marker = _selectedBuildToPlace == definition ? " ✓" : "";
            if (GUILayout.Button(definition.DisplayName + marker))
                _selectedBuildToPlace = definition;
        }

        // Annule la sélection de bâtiment en cours avant tout placement : aucune ressource n'a
        // encore été dépensée à ce stade (le coût n'est déduit qu'au clic sur "Construire ici"),
        // donc annuler revient simplement à désélectionner.
        private void CancelBuildSelection()
        {
            _selectedBuildToPlace = null;
            _lastMessage = "Sélection de construction annulée.";
        }

        private void DrawGrid()
        {
            GUILayout.Label("Planète (clic = sélectionner une zone)", _titleStyle);
            _gridScroll = GUILayout.BeginScrollView(_gridScroll);
            for (var y = 0; y < _planet.Height; y++)
            {
                GUILayout.BeginHorizontal();
                for (var x = 0; x < _planet.Width; x++)
                {
                    _planet.TryGetZone(x, y, out var zone);
                    var previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = ZoneColor(zone);
                    if (GUILayout.Button(ZoneLabel(zone), GUILayout.Width(18), GUILayout.Height(18)))
                    {
                        _selectedX = x;
                        _selectedY = y;
                    }
                    GUI.backgroundColor = previousColor;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private static Color ZoneColor(Zone zone)
        {
            if (!zone.IsRevealed) return Color.black;
            if (zone.BuildingId.HasValue) return Color.cyan;
            if (!zone.IsBuildable) return new Color(0.2f, 0.4f, 0.9f);
            if (zone.Deposit != null) return Color.yellow;
            return Color.green;
        }

        private static string ZoneLabel(Zone zone)
        {
            if (!zone.IsRevealed) return string.Empty;
            if (zone.BuildingId.HasValue) return "B";
            if (zone.Deposit != null) return "D";
            return string.Empty;
        }

        private void DrawColonists()
        {
            GUILayout.Label("Colons", _titleStyle);
            _colonistScroll = GUILayout.BeginScrollView(_colonistScroll);
            foreach (var colonist in _colonists)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{colonist.Name} ({colonist.Gender})");
                GUILayout.Label(DescribeAssignment(colonist));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Chantier")) AssignToConstruction(colonist);
                if (GUILayout.Button("Chercheur")) AssignToResearch(colonist);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Extraire")) AssignToExtraction(colonist);
                if (GUILayout.Button("Transport")) AssignToTransport(colonist);
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Libérer")) Unassign(colonist);
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        private static string DescribeAssignment(Colonist colonist)
        {
            if (colonist.CurrentAssignment == null) return "Sans affectation";
            return $"{colonist.CurrentAssignment.Type} → {colonist.CurrentAssignment.TargetId.ToString().Substring(0, 8)}";
        }
    }
}
