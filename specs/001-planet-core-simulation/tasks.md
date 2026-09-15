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

- [X] T001 Créer l'arborescence `Assets/_Project/Scripts/{Core,Procedural,FogOfWar,Building,Economy,Research,Colonists,Logistics,Civilization}/`, `Assets/_Project/ScriptableObjects/{Resources,Buildings,Technologies,Jobs,Identity}/`, `Assets/_Project/Prefabs/`, `Assets/_Project/Scenes/`, et `Assets/_Project/Tests/{EditMode,PlayMode}/` avec un sous-dossier miroir par module, conformément à plan.md § Project Structure
- [X] T002 [P] Ajouter le package `com.unity.nuget.newtonsoft-json` à `Packages/manifest.json` conformément à research.md §3 (sérialisation de sauvegarde)
- [X] T003 [P] Créer les fichiers `.asmdef` séparant assembly de logique pure (`Game.Core`, `Game.Procedural`, etc., sans dépendance `UnityEngine.CoreModule` où possible) des assemblies `EditMode`/`PlayMode`, pour garantir la testabilité imposée par le Principe II de la Constitution

**Checkpoint**: Structure de projet prête, aucune tâche de story ne peut commencer avant la fin de
la Phase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Types et services partagés par toutes les user stories. Regroupe, au-delà des entités
de base, les capacités génériques (créer un colon avec identité, assigner manuellement un colon à
une cible) réutilisées dès US1/US2/US3 — bien avant que leurs habillages plus riches (mode
automatique en US6, fiche UI en US10) n'existent.

**⚠️ CRITICAL**: Aucune story ne doit démarrer avant la fin de cette phase.

- [X] T004 [P] Implémenter les types `Planet` et `Zone` (Coordonnees, Terrain, Gisement, EstConstructible, EstRevelee, BatimentId) dans `Assets/_Project/Scripts/Procedural/Planet.cs` et `Assets/_Project/Scripts/Procedural/Zone.cs`, per data-model.md § Planète/Zone — `Deposit` également implémenté ici (`Deposit.cs`), en avance sur T033, car `Zone` le référence structurellement
- [X] T005 [P] Créer le ScriptableObject `ResourceDefinition` (ressource brute ou transformée, cf. `EstBrute`, `RecetteTransformation`) dans `Assets/_Project/Scripts/Economy/ResourceDefinition.cs`
- [X] T006 [P] Créer le ScriptableObject `BuildingDefinition` (`Cout`, `DureeChantier` par type — valeur de contenu/équilibrage, FR-042 —, `PrerequisTechnologiques` optionnels — FR-044 —, `RayonBrouillard`, `PostesEmploiDefinis`, `EstLogement`) dans `Assets/_Project/Scripts/Building/BuildingDefinition.cs`, per data-model.md § Catalogue de bâtiments ; structure extensible sans modification de la spec/du plan (FR-044)
- [X] T007 [P] Créer le ScriptableObject `JobDefinition` (métier, dont « Chercheur ») dans `Assets/_Project/Scripts/Colonists/JobDefinition.cs`
- [X] T008 [P] Créer le ScriptableObject `TechnologyDefinition` (ressource ciblée, progrès requis) dans `Assets/_Project/Scripts/Research/TechnologyDefinition.cs`
- [X] T009 [P] Créer le ScriptableObject `EthnicityDefinition` (listes de prénoms/noms par ethnie) dans `Assets/_Project/Scripts/Colonists/EthnicityDefinition.cs`, per research.md §6
- [X] T010 [P] Implémenter l'entité `Colon` — champs `Nom`, `Genre` (Homme/Femme, FR-045), `EthnieId`, `Biographie` (« ≤ 300 caractères », « aucun effet sur les mécaniques de jeu », cf. data-model.md § Colon), `Sante`, `Competences` (dictionnaire métier→niveau, FR-020), `ModeAssignation`, `AffectationActuelle`, `LogementId` (bâtiment d'habitation, indépendant de `AffectationActuelle`) — dans `Assets/_Project/Scripts/Colonists/Colonist.cs` (dépend de T007, T009)
- [X] T011 [P] Implémenter l'entité `NiveauCompetence` (MetierId, Valeur, Source `SurLeTas`/`Universite`) dans `Assets/_Project/Scripts/Colonists/SkillLevel.cs`, per data-model.md § Niveau de compétence
- [X] T012 Implémenter `IColonistIdentityService.CreateColonist` (nom + ethnie générés automatiquement depuis `EthnicityDefinition`, genre fourni ou tiré ~50/50 si non fourni, FR-039/FR-040/FR-045/FR-046) dans `Assets/_Project/Scripts/Colonists/ColonistIdentityService.cs` (dépend de T009, T010)
- [X] T013 Implémenter `IAssignmentService.AssignManually` (écrit `Colon.AffectationActuelle` vers un poste, un transport, un chantier ou une formation — sans encore la notion de mode manuel/automatique, ajoutée en US6) dans `Assets/_Project/Scripts/Colonists/AssignmentService.cs` (dépend de T010)
- [X] T014 [P] Implémenter `ISimulationClock`/`SimulationClock` (Pause/Resume/SetSpeed x1-x2-x3, événement `OnTick`) dans `Assets/_Project/Scripts/Core/SimulationClock.cs`, per contracts/core-interfaces.md § Game.Core et research.md §1 (temps réel continu avec pause)
- [X] T015 Définir l'enveloppe `GameStateSnapshot` (schemaVersion=1, savedAtUtc, sections placeholder planet/colony/colonists/buildings/technologies/transportTasks/treasury, étendues incrémentalement par chaque story) dans `Assets/_Project/Scripts/Core/GameStateSnapshot.cs`, per contracts/savegame-schema.md § Enveloppe
- [X] T016 Implémenter `ISaveLoadService` (Save/Load/TryMigrate) avec Newtonsoft.Json, écriture dans `Application.persistentDataPath`, refus explicite d'une sauvegarde dont `schemaVersion` n'est pas supporté (pas d'échec silencieux) dans `Assets/_Project/Scripts/Core/SaveLoadService.cs` (dépend de T002, T015)
- [X] T017 Test EditMode : aller-retour de sérialisation de `GameStateSnapshot` (état minimal) dans `Assets/_Project/Tests/EditMode/Core/SaveLoadServiceTests.cs` (dépend de T015, T016)

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

- [X] T018 [P] [US1] Implémenter `IPlanetGenerationService.Generate(seed, width, height)` générant une grille de `Zone` avec terrain et gisements (FR-001) dans `Assets/_Project/Scripts/Procedural/PlanetGenerationService.cs`, grille carrée per research.md §2
- [X] T019 [P] [US1] Implémenter `IFogOfWarService` (`IsRevealed`, `RevealAround`) : zone de départ visible en début de partie, reste masqué (FR-003), dissipation en disque de tuiles autour d'un point (FR-004) dans `Assets/_Project/Scripts/FogOfWar/FogOfWarService.cs`
- [X] T020 [US1] Définir et exposer un événement générique « bâtiment construit » — consommé par `IConstructionSiteService.BuildingCompleted` (US2, T028) plutôt que par `BuildingPlacementService` (T027) : le fog reveal se déclenche à la fin du chantier, pas à sa pose, conformément à spec.md US1 Acceptance Scenario 2 (« quand sa construction se termine ») — déclenchant `FogOfWarService.RevealAround` autour du bâtiment (FR-004) dans `Assets/_Project/Scripts/FogOfWar/FogOfWarConstructionListener.cs` (dépend de T019)
- [X] T021 [US1] Implémenter le bootstrap de partie (l'entité `Building`, nominalement T025/US2, est également implémentée ici car l'abri de secours initial en a besoin structurellement) : placement de l'abri de secours initial (`EstAbriInitial = true`), révélation de la zone de départ autour de lui, dotation initiale de ressources et de colons (créés via `IColonistIdentityService.CreateColonist`) avec, pour chaque colon de départ, un niveau de compétence initial aléatoire et variable selon les métiers et une répartition d'environ 50%/50% hommes/femmes (FR-007, FR-021, FR-046) dans `Assets/_Project/Scripts/Procedural/GameBootstrapService.cs` (dépend de T018, T019, T010, T012)
- [X] T022 [P] [US1] Implémenter le contrôleur caméra god mode (pan/zoom, aucun personnage incarné ni déplacé, FR-002) via Input System dans `Assets/_Project/Scripts/Core/GodModeCameraController.cs`
- [X] T023 [US1] Persister la grille de zones et l'état du brouillard de guerre (`EstRevelee` par zone), restauration à l'identique au chargement (FR-031 tranche US1) — implémenté via un mapper dédié `PlanetSnapshotMapper` (Planet <-> PlanetSnapshot) plutôt qu'en modifiant `SaveLoadService.cs` lui-même, qui reste générique dans `Assets/_Project/Scripts/Procedural/PlanetSnapshotMapper.cs`
- [X] T024 [P] [US1] Tests EditMode : génération de planète (zones/gisements présents), révélation initiale de la zone de départ, `RevealAround` dissipe bien un disque de tuiles dans `Assets/_Project/Tests/EditMode/Procedural/PlanetGenerationServiceTests.cs` et `Assets/_Project/Tests/EditMode/FogOfWar/FogOfWarServiceTests.cs`

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

- [X] T025 [P] [US2] Implémenter l'entité runtime `Building` (fait en avance en T021 ; renommée en C# `BuildingInstance` — un alias/using ne suffit pas à lever l'ambiguïté C# entre un type nommé `Building` et le namespace `Game.Building` référencé depuis un autre module, `BuildingDefinition` reste inchangé) (Id, DefinitionId, Position, Etat `EnChantier`/`Operationnel`, `ProgresChantier`, PostesEmploi, EstAbriInitial) dans `Assets/_Project/Scripts/Building/Building.cs`, per data-model.md § Bâtiment
- [X] T026 [P] [US2] Implémenter `Inventory`/Stock (RessourceId, Quantite, CapaciteMax) dans `Assets/_Project/Scripts/Economy/Inventory.cs`, per data-model.md § Stock / Inventaire
- [X] T027 [US2] Implémenter `IBuildingPlacementService.CanBuild`/`Build` : validation du coût contre l'`Inventory`, refus explicite avec ressources manquantes si coût > stock (FR-006), déduction du stock et démarrage en état `EnChantier` sinon (FR-005/FR-042) — ne déclenche PAS l'événement de brouillard de guerre ici (voir T020/T028) dans `Assets/_Project/Scripts/Building/BuildingPlacementService.cs` (dépend de T006, T020, T025, T026)
- [X] T028 [US2] Implémenter `IConstructionSiteService.Tick` — déclenche ici l'événement « bâtiment construit » (`BuildingCompleted`) à la transition vers `Operationnel`, consommé par `FogOfWarConstructionListener` (T020) : `ProgresChantier` n'avance que si au moins un colon a une `Affectation` de type `Construction` ciblant ce bâtiment (assigné via `IAssignmentService.AssignManually`, T013) ; transition `EnChantier → Operationnel` une fois `DureeChantier` (catalogue) atteinte ; aucune régression ni annulation en l'absence de colon (FR-042/FR-043) dans `Assets/_Project/Scripts/Building/ConstructionSiteService.cs` (dépend de T013, T025, T027)
- [X] T029 [P] [US2] Orchestrateur UI Toolkit : menu de construction + placement par clic sur une zone révélée, avec affichage de l'état de chantier dans `Assets/_Project/Scripts/Building/BuildingPlacementView.cs`, per research.md §4 (UI Toolkit)
- [X] T030 [US2] Persister la liste des bâtiments (dont `Etat`/`ProgresChantier`) et l'inventaire (FR-031 tranche US2) — via `BuildingSnapshotMapper` et `InventorySnapshotMapper` dédiés dans `Assets/_Project/Scripts/Building/BuildingSnapshotMapper.cs` et `Assets/_Project/Scripts/Economy/InventorySnapshotMapper.cs`
- [X] T031 [P] [US2] Tests EditMode : construction réussie déduit le coût et démarre en chantier, chantier ne progresse pas sans colon assigné, chantier progresse et devient opérationnel avec un colon assigné pendant la durée requise, construction refusée si stock insuffisant avec liste des ressources manquantes dans `Assets/_Project/Tests/EditMode/Building/BuildingPlacementServiceTests.cs` et `Assets/_Project/Tests/EditMode/Building/ConstructionSiteServiceTests.cs`

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

- [X] T032 [P] [US3] Implémenter l'entité `Technology` (Id, RessourceCibleId, ProgresRequis, ProgresActuel, EstDebloquee) et `IResearchService.ContributeProgress`/`IsUnlocked` dans `Assets/_Project/Scripts/Research/ResearchService.cs`, per data-model.md § Technologie
- [X] T033 [P] [US3] Implémenter l'entité `Deposit` (fait en avance en T004, cf. note associée) (gisement) avec machine à états `TechnologieNonDebloquee → PretPourExtracteur → EnExtraction → Epuise` dans `Assets/_Project/Scripts/Procedural/Deposit.cs`, per data-model.md § Gisement de ressource (dépend de T018)
- [X] T034 [US3] Implémenter `IExtractionService.CanBuildExtractor` (refuse la construction d'un extracteur si la technologie n'est pas débloquée, FR-009) et `Tick` (extraction progressive une fois l'extracteur opérationnel, arrêt automatique et notification joueur à l'épuisement, FR-010/FR-011) dans `Assets/_Project/Scripts/Economy/ExtractionService.cs` (dépend de T027, T032, T033)
- [X] T035 [US3] Assigner le métier « Chercheur » (JobDefinition) via `IAssignmentService.AssignManually` : un colon employé sur ce poste alimente `IResearchService.ContributeProgress` à chaque tick, avec un rendement provisoire fixe (remplacé par le rendement compétence/santé réel en US6, T050) (FR-008, FR-017) dans `Assets/_Project/Scripts/Research/ResearcherJobBinding.cs` (dépend de T007, T010, T013, T032)
- [X] T036 [US3] Persister les technologies (progrès/déblocage) — via `TechnologySnapshotMapper` dédié dans `Assets/_Project/Scripts/Research/TechnologySnapshotMapper.cs` (l'état des gisements est déjà couvert par `PlanetSnapshotMapper`, un `Deposit` vivant dans une `Zone`)
- [X] T037 [P] [US3] Tests EditMode : construction d'extracteur refusée avant déblocage technologique, acceptée après ; extraction progresse puis s'arrête automatiquement à l'épuisement dans `Assets/_Project/Tests/EditMode/Research/ResearchServiceTests.cs` et `Assets/_Project/Tests/EditMode/Economy/ExtractionServiceTests.cs`

**Checkpoint**: US1–US3 forment une boucle recherche → extraction fonctionnelle et testable.

---

## Phase 6: User Story 4 - Transporter les ressources (Priority: P1)

**Goal**: Le joueur organise le transport des ressources entre sites de production et sites de
stockage/consommation ; aucune ressource ne se déplace automatiquement.

**Independent Test**: Avec un extracteur produisant une ressource et un stockage disponible mais
non relié, vérifier l'accumulation sur place sans transport assigné, puis l'acheminement effectif
une fois un colon/véhicule assigné.

### Implementation for User Story 4

- [X] T038 [P] [US4] Implémenter l'entité `TransportTask` (Id, RessourceId, SiteSourceId, SiteDestinationId, AssigneA colon ou véhicule, CapaciteParCycle) dans `Assets/_Project/Scripts/Logistics/TransportTask.cs`, per data-model.md § Tâche de transport
- [X] T039 [US4] Implémenter `ITransportService.Assign`/`Tick` : la ressource s'accumule au site source tant qu'aucune tâche de capacité suffisante n'est assignée (FR-014), et est acheminée progressivement une fois assignée via `IAssignmentService.AssignManually` (FR-012/FR-013) dans `Assets/_Project/Scripts/Logistics/TransportService.cs` (dépend de T013, T026, T034, T038)
- [X] T040 [US4] Modifier `ExtractionService` pour que la ressource extraite s'accumule au site de production plutôt que d'aller directement en stock, la sortie ne rejoignant le stockage qu'au travers d'un `TransportTask` (FR-012) dans `Assets/_Project/Scripts/Economy/ExtractionService.cs` (dépend de T039)
- [X] T041 [US4] Persister les tâches de transport (FR-031 tranche US4) — via `TransportTaskSnapshotMapper` dédié dans `Assets/_Project/Scripts/Logistics/TransportTaskSnapshotMapper.cs`
- [X] T042 [P] [US4] Tests EditMode : pas de mouvement sans tâche assignée, mouvement progressif avec tâche assignée, accumulation au point de production quand la capacité de transport est insuffisante dans `Assets/_Project/Tests/EditMode/Logistics/TransportServiceTests.cs`

**Checkpoint**: US1–US4 (tout le P1) forment la boucle minimale jouable : fondation, construction,
recherche/extraction, transport.

---

## Phase 6.5: Rework Module de survie, placement libre & coût combiné (FR-050–FR-055)

**Purpose**: Clarification de conception du 2026-09-15 (cf. spec.md FR-050 à FR-053, data-model.md
§ Module de survie / § Extracteur multifonction / § Catalogue de bâtiments), à intégrer avant les
nouveaux bâtiments de contenu de US5 (Phase 7) qui en dépendent directement (coût en Crédits
Galactiques). Touche du code déjà livré en US1 (T021)/US2 (T025, T027) : traité comme un rework
ciblé plutôt qu'une réouverture de ces tâches.

### Rework pour User Story 1 (Module de survie, Extracteur multifonction)

- [X] T079 [US1] Renommer « Abri de secours initial » en « Module de survie » dans le code et le
  contenu existants (bootstrap `GameBootstrapService` T021, libellés UI de
  `BuildingPlacementView`/fenêtre de gestion T029/T070 le cas échéant, nom du `BuildingDefinition`
  ScriptableObject correspondant) — le champ C# `EstAbriInitial` (FR-007/FR-037/FR-050) est
  conservé tel quel, seuls les libellés/commentaires FR et le nom de contenu changent, per
  data-model.md § Module de survie / Bâtiment — fait sur `StartingShelter.asset` +
  `Phase1ContentLibrary` (générateur), messages `Phase1GameController`/`Phase1TestHarness` ;
  `Phase1GameView` n'avait aucune chaîne à renommer (libellé déjà dérivé de `DisplayName`)
- [X] T080 [US1] Implémenter l'inventaire consultable du Module de survie (FR-050) : ouverture au
  clic sur le bâtiment (`EstAbriInitial = true`) d'une vue UI Toolkit listant la dotation initiale
  de ressources (eau, nourriture, via `Inventory` T026) et les Extracteurs multifonction
  disponibles, dans `Assets/_Project/Scripts/Building/SurvivalModuleInventoryView.cs` (dépend de
  T026, T079) — implémenté en tant que section `DrawSurvivalModuleInventory` du panneau bâtiment
  existant de `Phase1GameView.cs` (OnGUI) plutôt qu'un fichier UI Toolkit dédié, pour rester
  cohérent avec le reste de la vue de démo déjà en place ; affiche `Warehouse` (le Module de survie
  n'a pas de réserve isolée, cf. B2 de /speckit-analyze — résolu ainsi)
- [X] T081 [P] [US1] Implémenter l'entité `MultiPurposeExtractor` (Extracteur multifonction) — `Id`,
  `GisementCibleId` (`Guid?`, `null` si rangé dans l'inventaire du Module de survie), et
  `TypesGisementAutorises` fixé à eau/pierre/bois (FR-051) — dans
  `Assets/_Project/Scripts/Building/MultiPurposeExtractor.cs`, per data-model.md § Extracteur
  multifonction — champs nommés `TargetX`/`TargetY` (nullable) plutôt que `GisementCibleId` (le
  gisement est retrouvé via `Planet.TryGetZone`, pas de référence directe)
- [X] T082 [US1] Implémenter `IMultiPurposeExtractorService.PlaceOn`/`MoveTo` : place un des 5
  exemplaires sur un gisement révélé dont le type de ressource est eau, pierre ou bois (refus sinon,
  FR-051), sans phase de chantier, et permet de le déplacer ensuite vers un autre gisement
  compatible à tout moment (`GisementCibleId` change directement, jamais de destruction/
  reconstruction, FR-052) dans `Assets/_Project/Scripts/Economy/MultiPurposeExtractorService.cs`
  (dépend de T034, T081) — n'appelle finalement pas `IExtractionService.Tick` : dédié à un `Tick`
  propre extrayant directement dans l'inventaire fourni par l'appelant (`Warehouse`), plutôt que
  dans un buffer de site nécessitant un transport (simplification documentée dans le code, cf.
  note ci-dessus) ; `contracts/core-interfaces.md` non mis à jour en conséquence (à corriger si ce
  choix est confirmé)
- [ ] T083 [US1] Persister la liste des Extracteurs multifonction (`GisementCibleId` par exemplaire)
  — via `MultiPurposeExtractorSnapshotMapper` dédié, et ajouter la section `MultiPurposeExtractor`
  correspondante à contracts/savegame-schema.md (absente à ce jour) dans
  `Assets/_Project/Scripts/Building/MultiPurposeExtractorSnapshotMapper.cs` (dépend de T081)

### Rework pour User Story 2 (coût de construction combiné)

- [ ] T084 [P] [US2] Implémenter l'entité minimale `Treasury` (`Montant`, `float`) dans
  `Assets/_Project/Scripts/Economy/Treasury.cs` — fait en avance sur T048 (US6), car T086
  (coût combiné, FR-053) a besoin d'un solde déductible dès US2/US5 ; T048 étendra ce type avec
  `RevenuImpotParTick`/`CoutChomageParTick` sans modifier `Montant`, per data-model.md § Trésorerie
  / Crédits Galactiques
- [ ] T091 [US2] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister `Treasury.Montant`
  (FR-031/FR-053) dès la Phase 6.5, per contracts/savegame-schema.md § Treasury — nécessaire car
  `Montant` devient mutable dès T086/T088/T089 (bien avant T048/US6) : sans cette persistance
  anticipée, une sauvegarde/rechargement entre la fin de cette phase et la fin de US6 perdrait
  silencieusement le solde de Crédits Galactiques dépensé (contradiction FR-031/FR-033/SC-010) dans
  `Assets/_Project/Scripts/Core/SaveLoadService.cs` (dépend de T084)
- [ ] T085 [US2] Étendre `BuildingDefinition` (T006) avec `CoutCreditsGalactiques` (`float`) — coût
  en Crédits Galactiques d'un type de bâtiment, en complément de `Cout` en ressources, déduit
  conjointement au lancement du chantier (FR-053) dans
  `Assets/_Project/Scripts/Building/BuildingDefinition.cs` (dépend de T006)
- [ ] T086 [US2] Étendre `IBuildingPlacementService.CanBuild`/`Build` (T027) pour prendre un
  paramètre `Treasury` supplémentaire (cf. contracts/core-interfaces.md § Game.Building, signature
  mise à jour) et déduire également `CoutCreditsGalactiques` du `Treasury.Montant` du joueur (T084),
  refus explicite de la construction si le coût en ressources OU le coût en Crédits Galactiques
  dépasse ce qui est disponible (FR-053) dans
  `Assets/_Project/Scripts/Building/BuildingPlacementService.cs` (dépend de T027, T084, T085)
- [ ] T087 [P] [US2] Tests EditMode : le bootstrap produit toujours un Module de survie fonctionnel
  après renommage (aucune régression sur US1/US2), placement d'un Extracteur multifonction accepté
  sur un gisement eau/pierre/bois et refusé sur un gisement incompatible, déplacement vers un autre
  gisement compatible sans passer par un état intermédiaire de chantier, construction refusée si
  `CoutCreditsGalactiques` insuffisant même avec un stock de ressources suffisant dans
  `Assets/_Project/Tests/EditMode/Building/MultiPurposeExtractorServiceTests.cs` et une extension de
  `Assets/_Project/Tests/EditMode/Building/BuildingPlacementServiceTests.cs`

### Positionnement libre, rotation, et contrainte d'adjacence de la pompe (FR-054/FR-055)

- [ ] T092 [US2] Étendre `BuildingInstance` avec `PositionOffset` (`Vector2`, 0..1 sur chaque axe,
  défaut `(0.5, 0.5)`) et `Rotation` (`0°/90°/180°/270°`), purement cosmétiques (FR-054, aucune
  règle de jeu n'en dépend) ; exposer un contrôle de positionnement/rotation dans
  `BuildingPlacementView` (T029) avant validation du placement dans
  `Assets/_Project/Scripts/Building/BuildingInstance.cs` (dépend de T025)
- [ ] T093 [US2] Ajouter `GisementCibleX`/`GisementCibleY` à `BuildingInstance` (coordonnées du
  gisement réellement exploité, identiques à `Position` par défaut) ; renommer
  `BuildingDefinition.CanBuildOnWater` en `ExtraitEauAdjacente` (FR-055) et inverser sa sémantique :
  refuse désormais la construction sur une case d'eau elle-même, accepte une case constructible
  adjacente à l'eau (fixe `GisementCibleX/Y` sur la case d'eau voisine) ou une case à gisement « nappe
  phréatique » (fixe `GisementCibleX/Y` sur sa propre case) — met à jour tous les points du code qui
  supposaient jusqu'ici `zone.Deposit` == gisement de la case du bâtiment (extraction, assignation,
  transport) dans `Assets/_Project/Scripts/Building/BuildingDefinition.cs`,
  `Assets/_Project/Scripts/Building/BuildingPlacementService.cs` et
  `Assets/_Project/Scripts/Core/Phase1GameController.cs` (dépend de T025, T092)
- [ ] T094 [P] [US2] Tests EditMode : placement refusé directement sur une case d'eau pour une pompe,
  accepté sur une case adjacente à l'eau (extraction ciblant bien la case voisine) et sur une case à
  gisement nappe phréatique (extraction ciblant sa propre case), positionnement/rotation persistés
  sans effet sur le coût/la durée de chantier dans
  `Assets/_Project/Tests/EditMode/Building/BuildingPlacementServiceTests.cs` (extension)

**Checkpoint**: Le Module de survie et le catalogue de bâtiments reflètent la clarification de
conception du 2026-09-15 ; User Story 5 peut s'appuyer sur `CoutCreditsGalactiques` (T085) pour ses
nouveaux bâtiments de contenu.

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
- [ ] T088 [P] [US5] Créer les `BuildingDefinition` de contenu (ScriptableObject) pour les 3
  bâtiments primitifs « de fortune » (cf. spec.md § Assumptions, data-model.md § Catalogue de
  bâtiments) : Ferme primitive, Puits (citerne primitive), Entrepôt primitif — coût en bois +
  pierre et `CoutCreditsGalactiques` (T085) pour chacun, capacité de stockage d'eau de 5 à 10 m³
  pour le Puits (valeur exacte d'équilibrage), dans
  `Assets/_Project/ScriptableObjects/Buildings/{FermePrimitive,PuitsPrimitif,EntrepotPrimitif}.asset`
  (dépend de T006, T085)
- [ ] T089 [US5] Créer la `ProductionRecipe` de contenu de la Ferme primitive (T043) : consomme de
  l'eau en continu, produit de la nourriture, `DureeCycle` d'équilibrage — première chaîne de
  production concrète de US5, validée par le Independent Test de cette story dans
  `Assets/_Project/ScriptableObjects/Buildings/FermePrimitiveRecipe.asset` (dépend de T043, T088)
- [ ] T090 [P] [US5] Tests EditMode : le Puits primitif limite bien le stock d'eau à sa
  `CapaciteMax` de contenu (5 à 10 m³, refus/plafonnement au-delà), la Ferme primitive consomme
  l'eau livrée par transport et produit de la nourriture via `IProductionService` (T044) tant que
  l'approvisionnement suit dans `Assets/_Project/Tests/EditMode/Building/PrimitiveBuildingsTests.cs`
  (dépend de T044, T088, T089)

**Checkpoint**: L'industrie fonctionne au-dessus de la boucle P1 sans la modifier ; la Ferme
primitive, le Puits et l'Entrepôt primitif donnent un premier catalogue de contenu concret pour
cette story.

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
- [ ] T051 [US6] Étendre `GameStateSnapshot`/`ISaveLoadService` pour persister les colons (compétences, santé, mode d'assignation, affectation) et les champs dérivés de la trésorerie (`RevenuImpotParTick`/`CoutChomageParTick`) — `Treasury.Montant` est déjà persisté depuis T091 (Phase 6.5), ne pas le redéfinir ici, per contracts/savegame-schema.md § Colonist/Treasury (FR-031 tranche US6)
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

**Goal**: Depuis le Module de survie, le joueur consulte la liste des colons, ouvre leur
fiche détaillée (genre, santé, compétence, éducation, ethnie, biographie), choisit leur mode
d'assignation, les renomme et édite leur biographie — sans effet sur le gameplay.

**Independent Test**: Ouvrir la fenêtre de gestion, sélectionner un colon, vérifier l'affichage
complet de sa fiche, basculer son mode d'assignation, le renommer, éditer sa biographie jusqu'à la
limite de 300 caractères (saisie bloquée au-delà), et vérifier l'absence d'effet sur compétence/
santé/rendement.

### Implementation for User Story 10

- [ ] T068 [P] [US10] Étendre `ColonistIdentityService` (T012) avec `Rename` (FR-039) dans `Assets/_Project/Scripts/Colonists/ColonistIdentityService.cs` (dépend de T012)
- [ ] T069 [US10] Étendre `ColonistIdentityService` avec `SetBiography` — champ libre, optionnel, vide par défaut ; « la saisie DOIT être bloquée dès que 300 caractères sont atteints (sans troncature ni message d'erreur après coup) » (FR-041, citation verbatim data-model.md § Colon) ; aucun effet sur compétence/santé/rendement dans `Assets/_Project/Scripts/Colonists/ColonistIdentityService.cs` (dépend de T068)
- [ ] T070 [P] [US10] Orchestrateur UI Toolkit : fenêtre de gestion ouverte depuis le Module de survie listant les ressources disponibles et les colons par nom (FR-037) dans `Assets/_Project/Scripts/Colonists/ColonistManagementWindow.cs`
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
- **Rework Module de survie (Phase 6.5)** : dépend de US1/US2 déjà complétées (T021, T025-T027) ;
  bloque les tâches de contenu de US5 qui utilisent `CoutCreditsGalactiques` (T088)
- **Polish (Phase 13)** : dépend de toutes les user stories retenues pour la release Phase 1

### User Story Dependencies

- **US1 (P1)** : après Foundational — aucune dépendance sur une autre story
- **US2 (P1)** : après Foundational — consomme l'événement défini par US1 (T020) mais reste
  testable seule (US2 peut simuler l'appel de l'événement sans le système de fog complet)
- **US3 (P1)** : après US2 (a besoin de `IBuildingPlacementService`/chantier pour construire un
  extracteur)
- **US4 (P1)** : après US3 (transporte la sortie d'un extracteur) — étend aussi US5 plus tard
- **US5 (P2)** : après US4 (consomme des ressources livrées par transport) et après Phase 6.5
  (T088/T089 utilisent `CoutCreditsGalactiques`, T085)
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
3. Phase 6.5 (rework Module de survie, Extracteur multifonction, coût combiné) → US5 → US6 → US7 →
   économie/industrie, compétence/rendement réels, et naissances → valider via quickstart.md 5-7
4. US8 → US9 → US10 → université, développement de la civilisation, gestion des colons → valider
   via quickstart.md 8-10
5. Phase 13 (Polish) → validation transverse SC-001 à SC-018, régression sauvegarde/chargement

## Notes

- [P] = fichiers différents, aucune dépendance non résolue
- Chaque story reste indépendamment testable via son « Independent Test » du spec.md
- Committer après chaque tâche ou groupe logique de tâches (Principe V : un commit = une tâche
  Spec Kit complète et fonctionnelle)
- Vérifier après chaque story que le quickstart.md correspondant passe avant de continuer
