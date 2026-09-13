---

description: "Task list template for feature implementation"
---

# Tasks: Cœur de simulation Phase 1 — planète solo

**Input**: Design documents from `/specs/001-planet-core-simulation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md (all present)

**Tests**: Included. La Constitution (Principe II) exige une logique métier testable en C# pur, et
plan.md définit explicitement `Assets/_Project/Tests/{EditMode,PlayMode}` ainsi que des contrats
de service dédiés (`contracts/core-interfaces.md`) — les tâches de test EditMode par service sont
donc incluses, sans viser une couverture exhaustive de chaque détail.

**Organization**: Tasks are grouped by user story (US1–US10, priorités P1/P1/P1/P1/P2/P2/P2/P3/P3/P3
issues de spec.md) pour permettre une implémentation et une validation indépendantes de chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, aucune dépendance non résolue)
- **[Story]**: Story concernée (US1..US10)
- Chemins de fichiers exacts inclus dans chaque description

## Path Conventions

Projet Unity mono-application (cf. plan.md — Project Structure) :

```text
Assets/_Project/Scripts/{Core,Procedural,FogOfWar,Building,Economy,Research,Colonists,Logistics,Civilization}/
Assets/_Project/ScriptableObjects/{Resources,Buildings,Technologies,Jobs,Identity}/
Assets/_Project/Tests/{EditMode,PlayMode}/<Module>/
```

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialisation du projet selon la structure définie dans plan.md

- [ ] T001 Créer l'arborescence `Assets/_Project/Scripts/{Core,Procedural,FogOfWar,Building,Economy,Research,Colonists,Logistics,Civilization}/`, `Assets/_Project/ScriptableObjects/{Resources,Buildings,Technologies,Jobs,Identity}/`, `Assets/_Project/Prefabs/`, `Assets/_Project/Scenes/`, et `Assets/_Project/Tests/{EditMode,PlayMode}/` avec un sous-dossier miroir par module, conformément à plan.md § Project Structure
- [ ] T002 [P] Ajouter le package `com.unity.nuget.newtonsoft-json` à `Packages/manifest.json` conformément à research.md §3 (sérialisation de sauvegarde)
- [ ] T003 [P] Créer les fichiers `.asmdef` séparant assembly de logique pure (`Game.Core`, `Game.Procedural`, etc., sans dépendance `UnityEngine.CoreModule` où possible) des assemblies `EditMode`/`PlayMode`, pour garantir la testabilité imposée par le Principe II de la Constitution

**Checkpoint**: Structure de projet prête, aucune tâche de story ne peut commencer avant la fin de
la Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Types et services partagés par toutes les user stories. Regroupe, au-delà des entités
de base, les capacités génériques (créer un colon avec identité, assigner manuellement un colon à
une cible) réutilisées dès US1/US2/US3 — bien avant que leurs habillages plus riches (mode
automatique en US6, fiche UI en US10) n'existent.

**⚠️ CRITICAL**: Aucune story ne doit démarrer avant la fin de cette phase.

- [ ] T004 [P] Implémenter les types `Planet` et `Zone` (Coordonnees, Terrain, Gisement, EstConstructible, EstRevelee, BatimentId) dans `Assets/_Project/Scripts/Procedural/Planet.cs` et `Assets/_Project/Scripts/Procedural/Zone.cs`, per data-model.md § Planète/Zone
- [ ] T005 [P] Créer le ScriptableObject `ResourceDefinition` (ressource brute ou transformée, cf. `EstBrute`, `RecetteTransformation`) dans `Assets/_Project/Scripts/Economy/ResourceDefinition.cs`
- [ ] T006 [P] Créer le ScriptableObject `BuildingDefinition` (`Cout`, `DureeChantier` par type — valeur de contenu/équilibrage, FR-042 —, `PrerequisTechnologiques` optionnels — FR-044 —, `RayonBrouillard`, `PostesEmploiDefinis`, `EstLogement`) dans `Assets/_Project/Scripts/Building/BuildingDefinition.cs`, per data-model.md § Catalogue de bâtiments ; structure extensible sans modification de la spec/du plan (FR-044)
- [ ] T007 [P] Créer le ScriptableObject `JobDefinition` (métier, dont « Chercheur ») dans `Assets/_Project/Scripts/Colonists/JobDefinition.cs`
- [ ] T008 [P] Créer le ScriptableObject `TechnologyDefinition` (ressource ciblée, progrès requis) dans `Assets/_Project/Scripts/Research/TechnologyDefinition.cs`
- [ ] T009 [P] Créer le ScriptableObject `EthnicityDefinition` (listes de prénoms/noms par ethnie) dans `Assets/_Project/Scripts/Colonists/EthnicityDefinition.cs`, per research.md §6
- [ ] T010 [P] Implémenter l'entité `Colon` — champs `Nom`, `Genre` (Homme/Femme, FR-045), `EthnieId`, `Biographie` (« ≤ 300 caractères », « aucun effet sur les mécaniques de jeu », cf. data-model.md § Colon), `Sante`, `Competences` (dictionnaire métier→niveau, FR-020), `ModeAssignation`, `AffectationActuelle`, `LogementId` (bâtiment d'habitation, indépendant de `AffectationActuelle`) — dans `Assets/_Project/Scripts/Colonists/Colonist.cs` (dépend de T007, T009)
- [ ] T011 [P] Implémenter l'entité `NiveauCompetence` (MetierId, Valeur, Source `SurLeTas`/`Universite`) dans `Assets/_Project/Scripts/Colonists/SkillLevel.cs`, per data-model.md § Niveau de compétence
- [ ] T012 Implémenter `IColonistIdentityService.CreateColonist` (nom + ethnie générés automatiquement depuis `EthnicityDefinition`, genre fourni ou tiré ~50/50 si non fourni, FR-039/FR-040/FR-045/FR-046) dans `Assets/_Project/Scripts/Colonists/ColonistIdentityService.cs` (dépend de T009, T010)
- [ ] T013 Implémenter `IAssignmentService.AssignManually` (écrit `Colon.AffectationActuelle` vers un poste, un transport, un chantier ou une formation — sans encore la notion de mode manuel/automatique, ajoutée en US6) dans `Assets/_Project/Scripts/Colonists/AssignmentService.cs` (dépend de T010)
- [ ] T014 [P] Implémenter `ISimulationClock`/`SimulationClock` (Pause/Resume/SetSpeed x1-x2-x3, événement `OnTick`) dans `Assets/_Project/Scripts/Core/SimulationClock.cs`, per contracts/core-interfaces.md § Game.Core et research.md §1 (temps réel continu avec pause)
- [ ] T015 Définir l'enveloppe `GameStateSnapshot` (schemaVersion=1, savedAtUtc, sections placeholder planet/colony/colonists/buildings/technologies/transportTasks/treasury, étendues incrémentalement par chaque story) dans `Assets/_Project/Scripts/Core/GameStateSnapshot.cs`, per contracts/savegame-schema.md § Enveloppe
- [ ] T016 Implémenter `ISaveLoadService` (Save/Load/TryMigrate) avec Newtonsoft.Json, écriture dans `Application.persistentDataPath`, refus explicite d'une sauvegarde dont `schemaVersion` n'est pas supporté (pas d'échec silencieux) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs` (dépend de T002, T015)
- [ ] T017 Test EditMode : aller-retour de sérialisation de `GameStateSnapshot` (état minimal) dans `Assets/_Project/Tests/EditMode/Core/SaveLoadServiceTests.cs` (dépend de T015, T016)

**Checkpoint**: Fondation prête — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 - Fonder la colonie et révéler le territoire (Priority: P1) 🎯 MVP

**Goal**: Une planète est générée, une zone de départ est visible autour de l'abri de secours
initial, le reste masqué par le brouillard de guerre, qui se dissipe automatiquement autour de
chaque bâtiment construit.

**Independent Test**: Lancer une nouvelle partie, vérifier zone de départ visible + reste masqué ;
appeler manuellement l'équivalent d'une construction en bordure de zone visible et vérifier que le
brouillard recule autour d'elle (sans dépendre du système complet de construction de US2).

### Implementation for User Story 1

- [ ] T018 [P] [US1] Implémenter `IPlanetGenerationService.Generate(seed, width, height)` générant une grille de `Zone` avec terrain et gisements (FR-001) dans `Assets/_Project/Scripts/Procedural/PlanetGenerationService.cs`, grille carrée per research.md §2
- [ ] T019 [P] [US1] Implémenter `IFogOfWarService` (`IsRevealed`, `RevealAround`) : zone de départ visible en début de partie, reste masqué (FR-003), dissipation en disque de tuiles autour d'un point (FR-004) dans `Assets/_Project/Scripts/FogOfWar/FogOfWarService.cs`
- [ ] T020 [US1] Définir et exposer un événement générique « bâtiment construit » (consommé plus tard par `BuildingPlacementService` en US2, T027) déclenchant `FogOfWarService.RevealAround` autour du bâtiment (FR-004) dans `Assets/_Project/Scripts/FogOfWar/FogOfWarConstructionListener.cs` (dépend de T019)
- [ ] T021 [US1] Implémenter le bootstrap de partie : placement de l'abri de secours initial (`EstAbriInitial = true`), révélation de la zone de départ autour de lui, dotation initiale de ressources et de colons (créés via `IColonistIdentityService.CreateColonist`) avec, pour chaque colon de départ, un niveau de compétence initial aléatoire et variable selon les métiers et une répartition d'environ 50%/50% hommes/femmes (FR-007, FR-021, FR-046) dans `Assets/_Project/Scripts/Procedural/GameBootstrapService.cs` (dépend de T018, T019, T010, T012)
- [ ] T022 [P] [US1] Implémenter le contrôleur caméra god mode (pan/zoom, aucun personnage incarné ni déplacé, FR-002) via Input System dans `Assets/_Project/Scripts/Core/GodModeCameraController.cs`
- [ ] T023 [US1] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister la grille de zones et l'état du brouillard de guerre (`EstRevelee` par zone), restauration à l'identique au chargement (FR-031 tranche US1) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs` (dépend de T015, T016, T018, T019)
- [ ] T024 [P] [US1] Tests EditMode : génération de planète (zones/gisements présents), révélation initiale de la zone de départ, `RevealAround` dissipe bien un disque de tuiles dans `Assets/_Project/Tests/EditMode/Procedural/PlanetGenerationServiceTests.cs` et `Assets/_Project/Tests/EditMode/FogOfWar/FogOfWarServiceTests.cs`

**Checkpoint**: US1 fonctionnelle et testable indépendamment (le déclenchement du brouillard de
guerre peut être testé sans le système de construction complet de US2).

---

## Phase 4: User Story 2 - Construire des bâtiments (Priority: P1)

**Goal**: Le joueur dépense des ressources pour lancer la construction d'un bâtiment, qui entre en
chantier et ne devient opérationnel que si un colon y est assigné pendant la durée requise.

**Independent Test**: Avec un stock de ressources suffisant, placer un bâtiment sur une zone
révélée et constructible, vérifier qu'il apparaît en chantier et ne progresse pas tant qu'aucun
colon n'y est assigné, puis qu'il devient opérationnel une fois un colon assigné pendant la durée
de chantier requise.

### Implementation for User Story 2

- [ ] T025 [P] [US2] Implémenter l'entité runtime `Building` (Id, DefinitionId, Position, Etat `EnChantier`/`Operationnel`, `ProgresChantier`, PostesEmploi, EstAbriInitial) dans `Assets/_Project/Scripts/Building/Building.cs`, per data-model.md § Bâtiment
- [ ] T026 [P] [US2] Implémenter `Inventory`/Stock (RessourceId, Quantite, CapaciteMax) dans `Assets/_Project/Scripts/Economy/Inventory.cs`, per data-model.md § Stock / Inventaire
- [ ] T027 [US2] Implémenter `IBuildingPlacementService.CanBuild`/`Build` : validation du coût contre l'`Inventory`, refus explicite avec ressources manquantes si coût > stock (FR-006), déduction du stock et démarrage en état `EnChantier` sinon (FR-005/FR-042) ; déclenche l'événement « bâtiment construit » consommé par T020 dans `Assets/_Project/Scripts/Building/BuildingPlacementService.cs` (dépend de T006, T020, T025, T026)
- [ ] T028 [US2] Implémenter `IConstructionSiteService.Tick` : `ProgresChantier` n'avance que si au moins un colon a une `Affectation` de type `Construction` ciblant ce bâtiment (assigné via `IAssignmentService.AssignManually`, T013) ; transition `EnChantier → Operationnel` une fois `DureeChantier` (catalogue) atteinte ; aucune régression ni annulation en l'absence de colon (FR-042/FR-043) dans `Assets/_Project/Scripts/Building/ConstructionSiteService.cs` (dépend de T013, T025, T027)
- [ ] T029 [P] [US2] Orchestrateur UI Toolkit : menu de construction + placement par clic sur une zone révélée, avec affichage de l'état de chantier dans `Assets/_Project/Scripts/Building/BuildingPlacementView.cs`, per research.md §4 (UI Toolkit)
- [ ] T030 [US2] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister la liste des bâtiments (dont `Etat`/`ProgresChantier`) et l'inventaire (FR-031 tranche US2) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs` (dépend de T016, T025, T026)
- [ ] T031 [P] [US2] Tests EditMode : construction réussie déduit le coût et démarre en chantier, chantier ne progresse pas sans colon assigné, chantier progresse et devient opérationnel avec un colon assigné pendant la durée requise, construction refusée si stock insuffisant avec liste des ressources manquantes dans `Assets/_Project/Tests/EditMode/Building/BuildingPlacementServiceTests.cs` et `Assets/_Project/Tests/EditMode/Building/ConstructionSiteServiceTests.cs`

**Checkpoint**: US1 + US2 fonctionnent ensemble (un chantier mené à bien dissipe visiblement le
brouillard de guerre) tout en restant testables séparément.

---

## Phase 5: User Story 3 - Débloquer et exploiter des ressources (Priority: P1)

**Goal**: Un colon assigné au métier de chercheur débloque une technologie d'extraction, un
extracteur peut alors être construit (via chantier) sur le gisement correspondant et produire la
ressource.

**Independent Test**: Débloquer une technologie (recherche simulée), construire l'extracteur
correspondant sur un gisement révélé, vérifier que la construction est refusée avant déblocage et
acceptée après, puis que l'extraction produit la ressource jusqu'à épuisement une fois l'extracteur
opérationnel.

### Implementation for User Story 3

- [ ] T032 [P] [US3] Implémenter l'entité `Technology` (Id, RessourceCibleId, ProgresRequis, ProgresActuel, EstDebloquee) et `IResearchService.ContributeProgress`/`IsUnlocked` dans `Assets/_Project/Scripts/Research/ResearchService.cs`, per data-model.md § Technologie
- [ ] T033 [P] [US3] Implémenter l'entité `Deposit` (gisement) avec machine à états `TechnologieNonDebloquee → PretPourExtracteur → EnExtraction → Epuise` dans `Assets/_Project/Scripts/Procedural/Deposit.cs`, per data-model.md § Gisement de ressource (dépend de T018)
- [ ] T034 [US3] Implémenter `IExtractionService.CanBuildExtractor` (refuse la construction d'un extracteur si la technologie n'est pas débloquée, FR-009) et `Tick` (extraction progressive une fois l'extracteur opérationnel, arrêt automatique et notification joueur à l'épuisement, FR-010/FR-011) dans `Assets/_Project/Scripts/Economy/ExtractionService.cs` (dépend de T027, T032, T033)
- [ ] T035 [US3] Assigner le métier « Chercheur » (JobDefinition) via `IAssignmentService.AssignManually` : un colon employé sur ce poste alimente `IResearchService.ContributeProgress` à chaque tick, avec un rendement provisoire fixe (remplacé par le rendement compétence/santé réel en US6, T050) (FR-008, FR-017) dans `Assets/_Project/Scripts/Research/ResearcherJobBinding.cs` (dépend de T007, T010, T013, T032)
- [ ] T036 [US3] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister les technologies (progrès/déblocage) et l'état des gisements (FR-031 tranche US3) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs`
- [ ] T037 [P] [US3] Tests EditMode : construction d'extracteur refusée avant déblocage technologique, acceptée après ; extraction progresse puis s'arrête automatiquement à l'épuisement dans `Assets/_Project/Tests/EditMode/Research/ResearchServiceTests.cs` et `Assets/_Project/Tests/EditMode/Economy/ExtractionServiceTests.cs`

**Checkpoint**: US1–US3 forment une boucle recherche → extraction fonctionnelle et testable.

---

## Phase 6: User Story 4 - Transporter les ressources (Priority: P1)

**Goal**: Le joueur organise le transport des ressources entre sites de production et sites de
stockage/consommation ; aucune ressource ne se déplace automatiquement.

**Independent Test**: Avec un extracteur produisant une ressource et un stockage disponible mais
non relié, vérifier l'accumulation sur place sans transport assigné, puis l'acheminement effectif
une fois un colon/véhicule assigné.

### Implementation for User Story 4

- [ ] T038 [P] [US4] Implémenter l'entité `TransportTask` (Id, RessourceId, SiteSourceId, SiteDestinationId, AssigneA colon ou véhicule, CapaciteParCycle) dans `Assets/_Project/Scripts/Logistics/TransportTask.cs`, per data-model.md § Tâche de transport
- [ ] T039 [US4] Implémenter `ITransportService.Assign`/`Tick` : la ressource s'accumule au site source tant qu'aucune tâche de capacité suffisante n'est assignée (FR-014), et est acheminée progressivement une fois assignée via `IAssignmentService.AssignManually` (FR-012/FR-013) dans `Assets/_Project/Scripts/Logistics/TransportService.cs` (dépend de T013, T026, T034, T038)
- [ ] T040 [US4] Modifier `ExtractionService` pour que la ressource extraite s'accumule au site de production plutôt que d'aller directement en stock, la sortie ne rejoignant le stockage qu'au travers d'un `TransportTask` (FR-012) dans `Assets/_Project/Scripts/Economy/ExtractionService.cs` (dépend de T039)
- [ ] T041 [US4] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister les tâches de transport (FR-031 tranche US4) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs`
- [ ] T042 [P] [US4] Tests EditMode : pas de mouvement sans tâche assignée, mouvement progressif avec tâche assignée, accumulation au point de production quand la capacité de transport est insuffisante dans `Assets/_Project/Tests/EditMode/Logistics/TransportServiceTests.cs`

**Checkpoint**: US1–US4 (tout le P1) forment la boucle minimale jouable : fondation, construction,
recherche/extraction, transport.

---

## Phase 7: User Story 5 - Développer l'économie et l'industrie (Priority: P2)

**Goal**: Des bâtiments de transformation convertissent des ressources brutes livrées par
transport en ressources plus élaborées.

**Independent Test**: Bâtiment de transformation construit et approvisionné via transport : vérifie
qu'il consomme l'entrée et produit la sortie tant que l'approvisionnement suit, avec mise en pause/
reprise automatique.

### Implementation for User Story 5

- [ ] T043 [P] [US5] Implémenter l'entité `Recette`/chaîne de production (BatimentId, Entrees, Sorties, DureeCycle) dans `Assets/_Project/Scripts/Economy/ProductionRecipe.cs`, per data-model.md § Chaîne de production
- [ ] T044 [US5] Implémenter `IProductionService.Tick` : consommation des entrées livrées par `ITransportService`, production des sorties, mise en pause automatique si intrant manquant ou stock de sortie plein, reprise automatique dès que la condition bloquante est levée (FR-015/FR-016), sans affecter les autres chaînes indépendantes dans `Assets/_Project/Scripts/Building/ProductionService.cs` (dépend de T039, T043)
- [ ] T045 [US5] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister `productionRecipeState` (inputBuffer/outputBuffer) par bâtiment, per contracts/savegame-schema.md § Building (FR-031 tranche US5)
- [ ] T046 [P] [US5] Tests EditMode : cycle de production consomme/produit, pause sur intrant manquant, pause sur stock de sortie plein, reprise automatique, chaînes indépendantes non affectées entre elles dans `Assets/_Project/Tests/EditMode/Building/ProductionServiceTests.cs`

**Checkpoint**: L'industrie fonctionne au-dessus de la boucle P1 sans la modifier.

---

## Phase 8: User Story 6 - Gérer l'emploi, la fiscalité et le rendement des colons (Priority: P2)

**Goal**: Les colons assignés à un emploi rapportent de l'impôt, les colons au chômage coûtent de
l'argent, le rendement de chaque colon dépend de sa compétence et de sa santé, et le joueur peut
choisir un mode d'assignation manuel ou automatique par colon.

**Independent Test**: Assigner un colon sans expérience à un poste : la compétence progresse
lentement vers un plafond bas, la trésorerie augmente proportionnellement à compétence/santé ;
laisser un colon au chômage : la trésorerie diminue.

### Implementation for User Story 6

- [ ] T047 [US6] Implémenter `ISkillProgressionService.ApplyOnTheJobProgress` (progression automatique plafonnée bas pendant l'occupation d'un poste, FR-022) et `ComputeYield` (rendement = f(compétence, santé), un bâtiment à 100% de postes occupés peut sous-performer si compétence/santé faibles, FR-026/FR-027) dans `Assets/_Project/Scripts/Colonists/SkillProgressionService.cs` (dépend de T010, T011)
- [ ] T048 [US6] Implémenter `ITreasuryService.ApplyEmploymentIncome` (impôt modulé par le rendement, FR-018) et `ApplyUnemploymentCost` (FR-019), plus `IsCritical()` (seuil réutilisé par US9 pour l'effondrement, FR-030/FR-036) dans `Assets/_Project/Scripts/Economy/TreasuryService.cs` (dépend de T047)
- [ ] T049 [US6] Étendre `AssignmentService` (T013) avec `SetAssignmentMode`/`RunAutomaticAssignmentPass` — mode d'assignation manuel ou automatique choisi individuellement par colon (FR-035) dans `Assets/_Project/Scripts/Colonists/AssignmentService.cs` (dépend de T013)
- [ ] T050 [US6] Remplacer le rendement provisoire de `ResearcherJobBinding` (T035) par `ISkillProgressionService.ComputeYield` réel dans `Assets/_Project/Scripts/Research/ResearcherJobBinding.cs` (dépend de T035, T047)
- [ ] T051 [US6] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister les colons (compétences, santé, mode d'assignation, affectation) et la trésorerie, per contracts/savegame-schema.md § Colonist/Treasury (FR-031 tranche US6)
- [ ] T052 [P] [US6] Tests EditMode : progression sur le tas plafonnée, rendement dépendant de compétence+santé (bâtiment 100% pourvu mais peu compétent sous-performe), revenu d'impôt vs coût de chômage, mode d'assignation manuel/automatique respecté dans `Assets/_Project/Tests/EditMode/Colonists/SkillProgressionServiceTests.cs`, `Assets/_Project/Tests/EditMode/Economy/TreasuryServiceTests.cs` et `Assets/_Project/Tests/EditMode/Colonists/AssignmentServiceTests.cs`

**Checkpoint**: L'économie complète (impôt, chômage, rendement réel, mode d'assignation) est
active ; le métier de chercheur (US3) utilise désormais le vrai rendement.

---

## Phase 9: User Story 7 - Faire naître de nouveaux colons par cohabitation en logement (Priority: P2)

**Goal**: Un homme et une femme cohabitant dans un logement pendant un an de temps de jeu continu
donnent naissance à un nouveau colon, indépendamment des ressources ou de la trésorerie de la
colonie.

**Independent Test**: Loger un homme et une femme dans le même bâtiment d'habitation, avancer le
temps de jeu d'un an sans toucher aux ressources/trésorerie, et vérifier qu'une naissance a lieu ;
vérifier qu'aucune naissance ne se produit avant un an ou sans cohabitation homme/femme continue.

### Implementation for User Story 7

- [ ] T053 [P] [US7] Implémenter `IHousingService.AssignResident`/`RemoveResident` (écrit `Colon.LogementId` vers un bâtiment dont `BuildingDefinition.EstLogement = true`) dans `Assets/_Project/Scripts/Colonists/HousingService.cs` (dépend de T010, T025)
- [ ] T054 [P] [US7] Implémenter l'entité `Cohabitation de logement` (BatimentId, DureeCohabitationContinue) dans `Assets/_Project/Scripts/Colonists/HousingCohabitation.cs`, per data-model.md § Cohabitation de logement (dépend de T006, T025)
- [ ] T055 [US7] Implémenter `IHousingBirthService.Tick` : fait progresser `DureeCohabitationContinue` tant qu'au moins un `Colon` de genre Homme et un de genre Femme ont `LogementId` pointant vers ce logement, la remet à zéro dès que cette condition cesse d'être vraie (FR-047) dans `Assets/_Project/Scripts/Colonists/HousingBirthService.cs` (dépend de T053, T054)
- [ ] T056 [US7] Implémenter `IHousingBirthService.PickNewbornGender` (tirage aléatoire rééquilibré vers le genre sous-représenté dès que l'écart dépasse 10%, FR-048) et déclencher la naissance via `IColonistIdentityService.CreateColonist` dès qu'un an de cohabitation continue est atteint, sans lire `Treasury` ni `Inventory` (FR-049) dans `Assets/_Project/Scripts/Colonists/HousingBirthService.cs` (dépend de T012, T055)
- [ ] T057 [US7] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister `LogementId` par colon et `DureeCohabitationContinue` par logement (FR-031 tranche US7) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs`
- [ ] T058 [P] [US7] Tests EditMode : pas de naissance sans cohabitation homme/femme continue, naissance après un an continu, réinitialisation du décompte après interruption (FR-047), naissance indépendante d'une trésorerie/ressources négatives (FR-049), rééquilibrage du ratio de genre au-delà de ±10% (FR-048) dans `Assets/_Project/Tests/EditMode/Colonists/HousingBirthServiceTests.cs`

**Checkpoint**: La population croît par cohabitation en logement, indépendamment de l'économie et
du niveau de développement de la civilisation.

---

## Phase 10: User Story 8 - Former les colons via l'université (Priority: P3)

**Goal**: Une fois l'université débloquée, un colon peut y être formé à un métier ciblé pour
progresser plus vite et plus haut que l'apprentissage sur le tas.

**Independent Test**: Avec une université construite, envoyer un colon en formation, vérifier coût
déduit + progression accélérée au-delà du plafond sur le tas, puis retrait possible à tout moment
avec conservation de la compétence acquise.

### Implementation for User Story 8

- [ ] T059 [P] [US8] Implémenter l'entité `Formation` (ColonId, MetierCibleId, CoutArgent, DureeRestante, Etat `EnCours`/`Terminee`/`Interrompue`) dans `Assets/_Project/Scripts/Colonists/Training.cs`, per data-model.md § Formation
- [ ] T060 [US8] Implémenter `UniversityTrainingService` : démarrage de formation avec déduction du `CoutArgent` (variable selon le métier visé, FR-023), progression de compétence plus rapide et plafond plus élevé que l'apprentissage sur le tas (FR-024), retrait à tout moment avec conservation de la compétence acquise (FR-025) dans `Assets/_Project/Scripts/Colonists/UniversityTrainingService.cs` (dépend de T047, T048, T059)
- [ ] T061 [US8] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister les formations en cours (FR-031 tranche US8)
- [ ] T062 [P] [US8] Tests EditMode : coût déduit au démarrage, progression accélérée au-delà du plafond sur le tas, retrait conserve la compétence acquise dans `Assets/_Project/Tests/EditMode/Colonists/UniversityTrainingServiceTests.cs`

**Checkpoint**: La formation universitaire améliore le rendement de la colonie sans modifier les
mécaniques précédentes.

---

## Phase 11: User Story 9 - Développer la civilisation (Priority: P3)

**Goal**: Le niveau de développement de la colonie progresse grâce à l'économie en place,
débloquant de nouveaux bâtiments (dont l'université) ; en l'absence de victoire possible, un état
d'échec (effondrement) existe si les conditions critiques persistent. La population, elle, continue
d'évoluer indépendamment via les naissances (US7).

**Independent Test**: Maintenir la colonie approvisionnée et financièrement positive : vérifier la
progression du niveau de développement et le déblocage d'un palier ; à l'inverse, priver la colonie
durablement : vérifier l'entrée en état d'échec, sans que cela n'arrête les naissances (US7).

### Implementation for User Story 9

- [ ] T063 [P] [US9] Implémenter l'entité `Colony` (Nom, NiveauDeveloppement, ProgresDeveloppement, Etat `EnCroissance`/`Stagnation`/`Effondrement`) dans `Assets/_Project/Scripts/Civilization/Colony.cs`, per data-model.md § Colonie (`NiveauDeveloppement`/`Etat` ne portent que sur le développement, jamais sur la population)
- [ ] T064 [US9] Implémenter `ICivilizationService.Tick` : progression de `NiveauDeveloppement` selon ressources produites/consommées et trésorerie, déblocage d'au moins un nouveau bâtiment/capacité par palier dont l'université (FR-028/FR-029) — ne lit ni ne modifie jamais la population des colons dans `Assets/_Project/Scripts/Civilization/CivilizationService.cs` (dépend de T048, T063)
- [ ] T065 [US9] Implémenter `ICivilizationService.EvaluateCollapse` : transition `EnCroissance → Stagnation` si besoin de base/trésorerie non couvert (FR-030), `Stagnation → Effondrement` si la situation critique persiste au-delà d'un seuil configurable, notification claire au joueur, aucune condition de victoire en Phase 1 (FR-036) dans `Assets/_Project/Scripts/Civilization/CivilizationService.cs` (dépend de T048 IsCritical, T064)
- [ ] T066 [US9] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister l'état de la colonie (FR-031 tranche US9) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs`
- [ ] T067 [P] [US9] Tests EditMode : croissance du niveau de développement sur conditions positives, stagnation sur besoin non couvert, déblocage de palier, effondrement après persistance d'une situation critique, absence de tout état de victoire, aucun effet sur la population/les naissances (US7) dans `Assets/_Project/Tests/EditMode/Civilization/CivilizationServiceTests.cs`

**Checkpoint**: La progression long terme et la condition d'échec sont fonctionnelles, sans
interférer avec la croissance démographique de US7.

---

## Phase 12: User Story 10 - Consulter et gérer chaque colon individuellement (Priority: P3)

**Goal**: Depuis l'abri de secours initial, le joueur consulte la liste des colons, ouvre leur
fiche détaillée (genre, santé, compétence, éducation, ethnie, biographie), choisit leur mode
d'assignation, les renomme et édite leur biographie — sans effet sur le gameplay.

**Independent Test**: Ouvrir la fenêtre de gestion, sélectionner un colon, vérifier l'affichage
complet de sa fiche, basculer son mode d'assignation, le renommer, éditer sa biographie jusqu'à la
limite de 300 caractères (saisie bloquée au-delà), et vérifier l'absence d'effet sur compétence/
santé/rendement.

### Implementation for User Story 10

- [ ] T068 [P] [US10] Étendre `ColonistIdentityService` (T012) avec `Rename` (FR-039) dans `Assets/_Project/Scripts/Colonists/ColonistIdentityService.cs` (dépend de T012)
- [ ] T069 [US10] Étendre `ColonistIdentityService` avec `SetBiography` — champ libre, optionnel, vide par défaut ; « la saisie DOIT être bloquée dès que 300 caractères sont atteints (sans troncature ni message d'erreur après coup) » (FR-041, citation verbatim data-model.md § Colon) ; aucun effet sur compétence/santé/rendement dans `Assets/_Project/Scripts/Colonists/ColonistIdentityService.cs` (dépend de T068)
- [ ] T070 [P] [US10] Orchestrateur UI Toolkit : fenêtre de gestion ouverte depuis l'abri de secours initial listant les ressources disponibles et les colons par nom (FR-037) dans `Assets/_Project/Scripts/Colonists/ColonistManagementWindow.cs`
- [ ] T071 [US10] Fiche détaillée UI Toolkit d'un colon : genre, santé, compétence par métier, formation en cours, ethnie, champ biographie à saisie bornée à 300 caractères (pas de message d'erreur, cf. T069), et contrôle radio manuel/automatique lié à `IAssignmentService.SetAssignmentMode` (FR-035/FR-038) dans `Assets/_Project/Scripts/Colonists/ColonistDetailView.cs` (dépend de T049, T068, T069, T070)
- [ ] T072 [P] [US10] Tests EditMode : nom/ethnie/genre générés automatiquement, renommage conservé, biographie bornée à 300 caractères sans troncature d'erreur, aucun champ de généalogie n'existe dans `Assets/_Project/Tests/EditMode/Colonists/ColonistIdentityServiceTests.cs`
- [ ] T073 [P] [US10] Test PlayMode : cliquer un colon dans la fenêtre de gestion ouvre sa fiche détaillée avec les champs attendus dans `Assets/_Project/Tests/PlayMode/Colonists/ColonistManagementWindowTests.cs`

**Checkpoint**: Toutes les user stories (US1–US10) sont fonctionnelles indépendamment et ensemble.

---

## Phase 13: Polish & Cross-Cutting Concerns

**Purpose**: Validation transverse et conformité Constitution

- [ ] T074 [P] Dérouler les 10 scénarios de `quickstart.md` de bout en bout dans l'éditeur Unity et consigner les résultats
- [ ] T075 [P] Vérifier chacun des critères SC-001 à SC-018 de spec.md contre la build implémentée
- [ ] T076 Revue de conformité Principe II sur tous les `Game.<Module>` : aucune logique métier ne doit résider dans un `MonoBehaviour` (uniquement orchestration scène/input/UI/rendu)
- [ ] T077 [P] Compléter les commentaires (français, uniquement où le POURQUOI n'est pas évident) sur les classes `Game.<Module>` conformément à CLAUDE.md
- [ ] T078 Exécuter une régression complète sauvegarde/rechargement sur les 10 scénarios de quickstart.md pour confirmer SC-010 et FR-033 (aucun état perdu ou incohérent, y compris pour les actions en cours — extraction, transport, chantier, production, formation, cohabitation — au moment de la sauvegarde)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance — démarre immédiatement
- **Foundational (Phase 2)** : dépend de Setup — bloque toutes les user stories
- **User Stories (Phase 3-12)** : dépendent toutes de Foundational ; US1→US4 (P1) forment la
  boucle minimale et doivent être complétées avant de considérer la Phase 1 comme un MVP jouable ;
  US5/US6/US7 (P2) et US8/US9/US10 (P3) s'appuient sur des services introduits par US1-US6 (cf.
  dépendances inter-tâches explicites ci-dessus) mais restent chacune indépendamment testable via
  son Independent Test
- **Polish (Phase 13)** : dépend de toutes les user stories retenues pour la release Phase 1

### User Story Dependencies

- **US1 (P1)** : après Foundational — aucune dépendance sur une autre story
- **US2 (P1)** : après Foundational — consomme l'événement défini par US1 (T020) mais reste
  testable seule (US2 peut simuler l'appel de l'événement sans le système de fog complet)
- **US3 (P1)** : après US2 (a besoin de `IBuildingPlacementService`/chantier pour construire un
  extracteur)
- **US4 (P1)** : après US3 (transporte la sortie d'un extracteur) — étend aussi US5 plus tard
- **US5 (P2)** : après US4 (consomme des ressources livrées par transport)
- **US6 (P2)** : après US2/US3 (emploi de colons, dont le métier de chercheur introduit en US3)
- **US7 (P2)** : après US2 (a besoin d'un bâtiment logement) et Foundational (`Colon.Genre`,
  `IColonistIdentityService.CreateColonist`) — n'a besoin ni de US5/US6 (économie) ni de US9
  (civilisation) : la naissance est volontairement indépendante de ces systèmes (FR-049)
- **US8 (P3)** : après US6 (dépend de `ISkillProgressionService`/`ITreasuryService`)
- **US9 (P3)** : après US6 (dépend de `ITreasuryService.IsCritical`)
- **US10 (P3)** : après US6 (mode d'assignation, T049) et US7/US1 (identité des colons, T012) —
  n'a plus besoin de US9 : la création d'un nouveau-né est entièrement gérée par US7 (T056)

### Parallel Opportunities

- Toutes les tâches `[P]` de Setup et Foundational peuvent s'exécuter en parallèle entre elles
- Au sein de chaque story, les tâches `[P]` (entités/tests sur des fichiers distincts) peuvent
  être menées en parallèle ; les tâches de service qui les assemblent restent séquentielles
- US1 et US2 peuvent être développées en parallèle par deux personnes une fois Foundational
  terminé, à condition de convenir à l'avance du contrat de l'événement « bâtiment construit »
  (T020/T027)
- US7 (naissances) peut être développée en parallèle de US5/US6/US8/US9 une fois US2 terminé,
  puisqu'elle ne dépend d'aucune de ces stories

---

## Parallel Example: Foundational Phase

```bash
Task: "Implémenter Planet et Zone dans Assets/_Project/Scripts/Procedural/Planet.cs et Zone.cs"
Task: "Créer ResourceDefinition dans Assets/_Project/Scripts/Economy/ResourceDefinition.cs"
Task: "Créer BuildingDefinition dans Assets/_Project/Scripts/Building/BuildingDefinition.cs"
Task: "Créer JobDefinition dans Assets/_Project/Scripts/Colonists/JobDefinition.cs"
Task: "Créer TechnologyDefinition dans Assets/_Project/Scripts/Research/TechnologyDefinition.cs"
Task: "Créer EthnicityDefinition dans Assets/_Project/Scripts/Colonists/EthnicityDefinition.cs"
Task: "Implémenter l'entité Colon dans Assets/_Project/Scripts/Colonists/Colonist.cs"
Task: "Implémenter NiveauCompetence dans Assets/_Project/Scripts/Colonists/SkillLevel.cs"
Task: "Implémenter SimulationClock dans Assets/_Project/Scripts/Core/SimulationClock.cs"
```

## Parallel Example: User Story 1

```bash
Task: "Implémenter PlanetGenerationService dans Assets/_Project/Scripts/Procedural/PlanetGenerationService.cs"
Task: "Implémenter FogOfWarService dans Assets/_Project/Scripts/FogOfWar/FogOfWarService.cs"
Task: "Implémenter GodModeCameraController dans Assets/_Project/Scripts/Core/GodModeCameraController.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 à 4, tout le P1)

1. Compléter Phase 1 : Setup
2. Compléter Phase 2 : Foundational (CRITIQUE — bloque toutes les stories, inclut l'entité `Colon`,
   la création d'identité et l'assignation manuelle de base)
3. Compléter Phase 3 (US1), Phase 4 (US2), Phase 5 (US3), Phase 6 (US4) dans l'ordre
4. **STOP et VALIDER** : dérouler les scénarios 1 à 4 de quickstart.md — c'est la boucle minimale
   jouable de la Phase 1 (fondation, construction/chantier, recherche/extraction, transport)

### Incremental Delivery

1. Setup + Foundational → fondation prête
2. US1 → US2 → US3 → US4 → boucle P1 complète (MVP Phase 1) → valider via quickstart.md 1-4
3. US5 → US6 → US7 → économie/industrie, compétence/rendement réels, et naissances → valider via
   quickstart.md 5-7
4. US8 → US9 → US10 → université, développement de la civilisation, gestion des colons → valider
   via quickstart.md 8-10
5. Phase 13 (Polish) → validation transverse SC-001 à SC-018, régression sauvegarde/chargement

## Notes

- [P] = fichiers différents, aucune dépendance non résolue
- Chaque story reste indépendamment testable via son « Independent Test » du spec.md
- Committer après chaque tâche ou groupe logique de tâches (Principe V : un commit = une tâche
  Spec Kit complète et fonctionnelle)
- Vérifier après chaque story que le quickstart.md correspondant passe avant de continuer
