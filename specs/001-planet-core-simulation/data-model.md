# Data Model: Cœur de simulation Phase 1 — planète solo

Entités extraites de la section *Key Entities* de [spec.md](./spec.md), avec champs, relations et
transitions d'état. Modélisation en classes C# pures (`Game.<Module>`), sans dépendance Unity,
conformément au Principe II de la Constitution. Les valeurs numériques précises (rayons, plafonds,
coûts, seuils) sont des paramètres de configuration (ScriptableObject) à ajuster en équilibrage —
seuls les champs et leur rôle sont fixés ici. Les noms de champs/entités ci-dessous sont donnés en
français pour la lisibilité de conception ; conformément au Principe III (noms de code en anglais),
l'implémentation C# DOIT utiliser des identifiants anglais équivalents (ex: `Genre` → `Gender`,
`Sante` → `Health`, `ModeAssignation` → `AssignmentMode`), comme le font déjà `contracts/
core-interfaces.md` et les chemins de fichiers de `tasks.md` (ex: `Colonist.cs`).

## Planète (`Game.Procedural`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant de la partie/planète générée |
| `Seed` | `int` | Graine de génération procédurale |
| `Largeur`, `Hauteur` | `int` | Dimensions de la grille de zones (FR-001) |
| `Zones` | `Zone[,]` | Grille de zones (cf. entité Zone) |

## Zone (`Game.Procedural` + `Game.FogOfWar`)

| Champ | Type | Description |
|---|---|---|
| `Coordonnees` | `(int x, int y)` | Position dans la grille de la planète |
| `Terrain` | `TerrainType` (enum, défini par ScriptableObject `TerrainDefinition`) | Type de terrain |
| `Gisement` | `GisementDeRessource?` | Gisement présent, le cas échéant (FR-001) |
| `EstConstructible` | `bool` | Dérivé du terrain |
| `EstRevelee` | `bool` | État du brouillard de guerre sur cette zone (FR-003/FR-004) |
| `BatimentId` | `Guid?` | Bâtiment occupant la zone, le cas échéant |

**Transitions** : `EstRevelee` passe de `false` à `true` quand la zone entre dans le rayon de
dissipation d'un bâtiment nouvellement construit (FR-004) ; ne revient jamais à `false` (pas de
ré-obscurcissement en Phase 1, non requis par le spec).

## Gisement de ressource (`Game.Procedural` + `Game.Economy`)

| Champ | Type | Description |
|---|---|---|
| `RessourceId` | référence `RessourceDefinition` | Type de ressource brute |
| `QuantiteRestante` | `float` | Stock restant, décroît avec l'extraction (FR-011) |
| `Etat` | `GisementEtat` (enum) | `TechnologieNonDebloquee` → `PretPourExtracteur` → `EnExtraction` → `Epuise` |

**Transitions** : `TechnologieNonDebloquee → PretPourExtracteur` quand la technologie associée est
débloquée (FR-009/FR-010) ; `PretPourExtracteur → EnExtraction` quand un extracteur est construit
dessus ; `EnExtraction → Epuise` quand `QuantiteRestante` atteint 0 (FR-011).

**Extension (contenu) — ressource durable** : un gisement peut être marqué `EstDurable = true`
(ex: bois), auquel cas `QuantiteRestante` ne décroît jamais et l'état `Epuise` n'est jamais atteint
— exception à FR-011 portée par le contenu (catalogue de ressources), pas par la règle générale.

**Extension (contenu) — gisement sous case d'eau** : une case d'eau peut porter systématiquement un
gisement d'une ressource liquide dédiée (ex: eau), extractible uniquement par un type de bâtiment
marqué `ExtraitEauAdjacente = true` (pompe). Une « nappe phréatique » est le même gisement, situé
sur une case terrestre normale plutôt que sous l'eau.

**Correction 2026-09-15 (FR-055)** : contrairement à la version précédente de ce document, la pompe
ne se construit JAMAIS directement sur une case d'eau — `Zone.EstConstructible` n'a donc plus
d'exception liée à l'eau. La pompe se construit sur une case constructible ADJACENTE à une case
d'eau (elle y extrait alors le gisement de la case d'eau voisine, pas le sien) OU directement sur
une case portant un gisement de type « nappe phréatique » (elle extrait alors son propre gisement,
comme un extracteur classique). `Batiment.GisementCibleX/GisementCibleY` (cf. § Bâtiment) porte les
coordonnées du gisement réellement exploité, qui ne coïncident avec celles du bâtiment que dans le
cas nappe phréatique ou pour tout extracteur non-pompe.

## Technologie (`Game.Research`)

| Champ | Type | Description |
|---|---|---|
| `Id` | référence `TechnologyDefinition` (ScriptableObject) | Identifiant/catalogue |
| `RessourceCibleId` | référence `RessourceDefinition` | Ressource dont elle débloque l'extraction |
| `ProgresRequis` | `float` | Seuil de progrès de recherche nécessaire au déblocage |
| `ProgresActuel` | `float` | Progrès accumulé, alimenté par les colons-chercheurs (FR-008) |
| `EstDebloquee` | `bool` | Dérivé de `ProgresActuel >= ProgresRequis` |

## Ressource (`Game.Economy`)

| Champ | Type | Description |
|---|---|---|
| `Id` | référence `RessourceDefinition` (ScriptableObject) | Catalogue (brute ou transformée) |
| `EstBrute` | `bool` | Distingue ressource brute vs. transformée |
| `RecetteTransformation` | `Recette?` | Entrées/sorties si ressource issue d'une chaîne de production |
| `Densite` | `float` (kg/m³) | Densité réelle de la ressource (ex: eau 1000, bois 650, pierre 2600, fer 7870) ; propriété de contenu par ressource, pas une liste fixe — sert à la double limite de capacité de transport (masse ET volume) |

## Stock / Inventaire (`Game.Economy`)

| Champ | Type | Description |
|---|---|---|
| `RessourceId` | référence `RessourceDefinition` | Ressource concernée |
| `Quantite` | `float` | Quantité actuellement stockée |
| `CapaciteMax` | `float` | Capacité de stockage globale ou par bâtiment de stockage |

## Catalogue de bâtiments — `BuildingDefinition` (ScriptableObject, `Game.Building`)

| Champ | Type | Description |
|---|---|---|
| `Id` | identifiant de catalogue | Type de bâtiment |
| `Cout` | `(RessourceId, Quantite)[]` | Coût en ressources, déduit au lancement du chantier (FR-005/FR-006) |
| `CoutCreditsGalactiques` | `float` | Coût en Crédits Galactiques (= Trésorerie, cf. entité Trésorerie ci-dessous), déduit conjointement au `Cout` en ressources au lancement du chantier (FR-053) ; refus de construction si l'un ou l'autre coût dépasse ce qui est disponible |
| `DureeChantier` | `float` | Durée de chantier propre à ce type de bâtiment ; valeur de contenu/équilibrage, pas fixée par la spec (FR-042) |
| `PrerequisTechnologiques` | référence(s) `TechnologyDefinition`, optionnel | Technologie(s) devant être débloquée(s) avant de pouvoir lancer ce type de bâtiment (FR-044), en plus du cas particulier des extracteurs (gisement + technologie, FR-009) |
| `RayonBrouillard` | `int` | Rayon de dissipation du brouillard de guerre à la construction (FR-004) |
| `PostesEmploiDefinis` | `JobDefinition[]` | Postes ouverts par ce type de bâtiment (dont chercheur, université) |
| `EstLogement` | `bool` | Vrai si ce type de bâtiment est un logement, site du mécanisme de naissance (FR-047) |
| `ExtraitEauAdjacente` | `bool` | Vrai uniquement pour une pompe : le bâtiment se construit sur une case constructible et extrait le gisement d'eau d'une case ADJACENTE (ou son propre gisement si nappe phréatique) — corrigé le 2026-09-15, cf. Gisement de ressource § Correction FR-055 |
| `TauxRecyclage` | `float` | Fraction du `Cout` remboursée au recyclage (ex: 0.5 = 50%) ; valeur d'équilibrage du catalogue, pas fixée par la spec |

**Extension (contenu) — recyclage** : le joueur peut détruire un bâtiment existant (typiquement un
extracteur/une pompe dont le gisement est épuisé) pour récupérer `Cout × TauxRecyclage` en
ressources ; la zone redevient libre. Ce principe est fixé ici, le taux exact reste un paramètre de
contenu.

Ce catalogue est une structure de données extensible et séparée de la spec/du plan (FR-044) : de
nouveaux `BuildingDefinition` peuvent être ajoutés au fil du développement sans modifier cette
spécification ni le plan technique ; seul le mécanisme générique de déblocage (ressources, et le
cas échéant technologie/gisement) reste fixé ici.

**Extension (contenu) — bâtiments primitifs attendus pour US5** : trois `BuildingDefinition` « de
fortune », coût `Cout` = bois + pierre, en attendant leurs équivalents avancés (valeurs exactes
d'équilibrage à définir) :
- **Ferme primitive** : consomme de l'eau en continu (taux dépendant de taille/technologie) pour
  produire de la nourriture — premier `Recette`/`Chaîne de production` de US5.
- **Puits (citerne primitive)** : `CapaciteMax` de stockage d'eau faible (5 à 10 m³), en attendant
  une citerne avancée nécessitant du fer (ressource non extractible en tout début de partie).
- **Entrepôt primitif** : stockage générique, en attendant un entrepôt avancé nécessitant d'autres
  matériaux.

## Module de survie / Bâtiment (`Game.Building`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant du bâtiment |
| `DefinitionId` | référence `BuildingDefinition` (ScriptableObject) | Type de bâtiment (coût, chantier, prérequis, rayon, postes — cf. ci-dessus) |
| `Position` | `(int x, int y)` | Zone occupée |
| `Etat` | `BatimentEtat` (enum) | `EnChantier` → `Operationnel` ; pour les bâtiments de transformation : `Operationnel` ↔ `EnPause` (FR-016) |
| `ProgresChantier` | `float` | Progression du chantier, de 0 à `DureeChantier` ; n'avance que si au moins un colon est assigné (FR-042/FR-043) |
| `PostesEmploi` | `PosteEmploi[]` | Postes ouverts par ce bâtiment une fois opérationnel (dont chercheur, université) |
| `EstAbriInitial` | `bool` | Vrai uniquement pour le Module de survie, le bâtiment de départ (FR-007/FR-037/FR-050) |
| `Inventaire` | `Inventory` | Uniquement pour le Module de survie (`EstAbriInitial = true`) : dotation initiale de ressources (eau, nourriture) et les `ExtracteurMultifonction` disponibles, consultable en cliquant sur le bâtiment (FR-050) |
| `PositionCase` | `Vector2` (0..1 sur chaque axe) | Offset du bâtiment à l'intérieur de sa case, purement cosmétique en Phase 1 (aucune règle de jeu n'en dépend) ; `(0.5, 0.5)` = centré (FR-054) |
| `Rotation` | enum `0°/90°/180°/270°` | Orientation du bâtiment, choisie par le joueur avant validation du placement, purement cosmétique en Phase 1 (FR-054) |
| `GisementCibleX`, `GisementCibleY` | `int` | Coordonnées de la case portant le gisement réellement exploité par ce bâtiment ; identiques à `Position` sauf pour une pompe adjacente à l'eau (cf. Gisement de ressource § Correction FR-055), où elles pointent vers la case d'eau voisine |

## Extracteur multifonction (`Game.Building` ou `Game.Economy`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant de cet exemplaire (parmi les 5 fournis, FR-051) |
| `GisementCibleId` | `Guid?` | Gisement sur lequel l'exemplaire est actuellement placé ; `null` s'il est rangé dans l'inventaire du Module de survie et non placé |
| `TypesGisementAutorises` | `RessourceId[]` (fixe : eau, pierre, bois) | Types de gisement compatibles (FR-051) |

**Transitions** : contrairement à un extracteur/une pompe fixe (`Batiment`), un `ExtracteurMultifonction`
n'entre jamais en chantier : le joueur le place directement sur un gisement compatible révélé
(`GisementCibleId` passe de `null` à l'Id du gisement) ou le déplace vers un autre gisement
compatible à tout moment (`GisementCibleId` change directement, sans passer par `null`) — jamais de
destruction/reconstruction (FR-052). Une fois placé, il extrait la ressource de son gisement au même
titre qu'un extracteur fixe opérationnel (réutilise `IExtractionService.Tick`, US3).

**Transitions** : `EnChantier → Operationnel` quand `ProgresChantier` atteint `DureeChantier`, ce
qui ne peut arriver que pendant qu'au moins un colon est assigné au chantier (`AffectationType.
Construction`, FR-042/FR-043) — sans colon assigné, `ProgresChantier` n'évolue pas mais ne régresse
pas non plus. `Operationnel ↔ EnPause` pour un bâtiment de transformation selon la disponibilité
des intrants ou la saturation du stock de sortie (FR-016).

## Chaîne de production / Recette (`Game.Economy`)

| Champ | Type | Description |
|---|---|---|
| `BatimentId` | référence `Batiment` | Bâtiment de transformation concerné |
| `Entrees` | `(RessourceId, Quantite)[]` | Ressources consommées par cycle |
| `Sorties` | `(RessourceId, Quantite)[]` | Ressources produites par cycle |
| `DureeCycle` | `float` | Temps de simulation par cycle de production |

## Colon (`Game.Colonists`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant unique |
| `Nom` | `string` | Généré automatiquement, renommable (FR-039) |
| `Genre` | `Genre` (enum `Homme` \| `Femme`) | Attribué à la création ; ~50/50 pour la population initiale (FR-045/FR-046), tiré avec rééquilibrage de quota pour un nouveau-né (FR-048) |
| `EthnieId` | référence `EthnicityDefinition` (ScriptableObject) | Héritée de la colonie à la naissance (FR-040) |
| `Biographie` | `string` (≤ 300 caractères) | Libre, optionnelle, vide par défaut, saisie bornée côté UI (FR-041), aucun effet gameplay |
| `Sante` | `float` (0–1 ou 0–100) | Influence le rendement (FR-026) |
| `Competences` | `Dictionary<MetierId, NiveauCompetence>` | Un niveau par métier possible (FR-020) |
| `ModeAssignation` | `ModeAssignation` (enum `Manuel` \| `Automatique`) | Par colon, modifiable à tout moment (FR-035) |
| `AffectationActuelle` | `Affectation?` | Poste d'emploi, tâche de transport, chantier, ou formation en cours ; `null` = chômage |
| `LogementId` | `Guid?` | Bâtiment d'habitation où réside ce colon, indépendant de `AffectationActuelle` (un colon peut être employé ailleurs que là où il loge) ; utilisé par la Cohabitation de logement (FR-047) |

### Niveau de compétence (`Game.Colonists`)

| Champ | Type | Description |
|---|---|---|
| `MetierId` | référence `JobDefinition` (ScriptableObject) | Métier concerné (dont Chercheur) |
| `Valeur` | `float` | Niveau actuel |
| `Source` | `SourceProgression` (enum `SurLeTas` \| `Universite`) | Détermine le plafond atteignable (FR-022/FR-024) |

**Transitions** : `Valeur` augmente automatiquement pendant qu'un colon occupe le métier
(`SurLeTas`, plafond bas) ou pendant une `Formation` active (`Universite`, plafond plus haut) ;
ne régresse pas en Phase 1 (non requis par le spec).

## Formation (`Game.Colonists`)

| Champ | Type | Description |
|---|---|---|
| `ColonId` | référence `Colon` | Colon en formation |
| `MetierCibleId` | référence `JobDefinition` | Métier visé |
| `CoutArgent` | `float` | Déduit de la trésorerie au démarrage (FR-023), variable selon le métier |
| `DureeRestante` | `float` | Temps de simulation restant |
| `Etat` | `FormationEtat` (enum) | `EnCours` → `Terminee` \| `Interrompue` (FR-025, retrait possible à tout moment) |

## Emploi / Affectation (`Game.Colonists`)

| Champ | Type | Description |
|---|---|---|
| `Type` | `AffectationType` (enum `Emploi` \| `Transport` \| `Construction` \| `Formation`) | Nature de l'affectation ; `Construction` désigne un colon assigné à faire progresser le chantier d'un bâtiment (FR-043) |
| `CibleId` | `Guid` | Poste, tâche de transport, bâtiment en chantier, ou formation concernée |

## Cohabitation de logement (`Game.Colonists`)

| Champ | Type | Description |
|---|---|---|
| `BatimentId` | référence `Batiment` (logement, `EstLogement = true`) | Logement concerné |
| `DureeCohabitationContinue` | `float` | Temps continu écoulé depuis que le logement est occupé par un couple (un `Colon` `Homme` et un `Colon` `Femme`, tous deux adultes) ; remis à zéro dès que cette condition n'est plus vraie (FR-047) |
| `TempsDepuisDerniereTentative` | `float` | Temps écoulé depuis la dernière tentative de naissance (une fois `DureeCohabitationContinue` ≥ un an) |

**Assignation automatique, pas manuelle** (`IHousingService.TryFormCouple`, extension contenu) : dès
qu'un logement n'a aucun adulte, les deux premiers colons adultes disponibles (`LogementId` nul,
non-enfants) de sexe opposé y sont assignés simultanément pour former un couple — aucune sélection
par le joueur. Exclusivité : un logement occupé par un couple n'accueille jamais de 3ᵉ adulte (le
modèle ne connaît que 0 ou 2 adultes par logement, jamais 1 seul).

**Capacité et taux de natalité** (`BuildingDefinition`, valeurs d'équilibrage) : `CapaciteAdultes`
(2, fixe dans ce modèle), `CapaciteEnfants`, `IntervalleTentativeNaissance`,
`ProbabiliteSuccesTentative` — le premier type de logement (« Abri basique ») a un taux de succès
plus faible que les futurs types (« Maison »...), à capacité/confort croissants.

**Transitions** : une fois `DureeCohabitationContinue` ≥ un an de temps de jeu ET une place
d'enfant libre, des **tentatives de naissance répétées** ont lieu (toutes les
`IntervalleTentativeNaissance`, avec probabilité `ProbabiliteSuccesTentative` de succès chacune) —
un échec n'est pas définitif, la tentative suivante a lieu plus tard (FR-047), ce qui disperse
naturellement les naissances dans le temps plutôt que de les concentrer à l'année pile. Le nouveau
colon créé via `IColonistIdentityService.CreateColonist` hérite de l'ethnie de la `Colonie`
(FR-040), reçoit un genre tiré aléatoirement avec rééquilibrage de quota (FR-048), naît `Enfant`
dans ce logement, et est indépendant de l'état des ressources/de la trésorerie de la colonie
(FR-049). Un enfant grandit (temps de jeu cumulé) jusqu'à sa majorité (18 ans), puis quitte
automatiquement le logement (`LogementId` réinitialisé) et devient un colon adulte disponible, avec
un niveau de compétence initial de 25% (cohérent avec le plafond d'un colon sans éducation
formelle) pour les métiers déjà pratiqués par les colons de départ.

**Important** : le Module de survie (`EstAbriInitial = true`) n'est PAS un logement au sens de ce
système (`EstLogement = false`) — les colons de départ y vivent sans `LogementId` défini, et aucune
naissance n'y est possible ; il faut construire un premier logement dédié (Abri basique) pour qu'un
couple puisse se former et qu'une naissance devienne possible. Une fois ce relogement effectif, le
Module de survie pourra être démantelé pour récupérer une partie de ses ressources — mécanique hors
périmètre de cette spécification, à détailler ultérieurement (cf. spec.md § Assumptions).

## Tâche de transport (`Game.Logistics`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant |
| `RessourceId` | référence `RessourceDefinition` | Ressource transportée |
| `SiteSourceId` | référence `Batiment` | Origine (extracteur, bâtiment de production) — **doit être un bâtiment réellement ciblé par le joueur**, jamais une valeur par défaut |
| `SiteDestinationId` | référence `Batiment` | Destination (entrepôt/citerne) — idem, le bâtiment de stockage effectivement construit et ciblé |
| `AssigneA` | `ColonId?` \| `VehiculeId?` | Colon ou véhicule assigné (FR-013) |
| `Phase` | `CyclePhase` (enum) | `TrajetVersSource → Chargement → TrajetVersDestination → Dechargement`, puis reprise automatique (cf. Cycle de transport ci-dessous) |
| `ProgresPhase` | `float` | Temps écoulé (s) dans la phase actuelle |
| `QuantiteTransportee` | `float` | Charge actuellement portée par le transporteur (0 hors phases de trajet/déchargement) |

## Cycle de transport (`Game.Logistics`)

Chaque transporteur (colon, puis véhicule) suit un cycle à quatre phases, sans mouvement continu :

1. **Trajet vers la source** : durée = distance(dépôt, source) / (vitesse du transporteur ×
   modificateur de terrain moyen du trajet, cf. Vitesse de terrain ci-dessous).
2. **Chargement** : durée fixe (valeur d'équilibrage de départ, ex. 5 s — amenée à diminuer avec
   des améliorations de bâtiments/technologies) ; le transporteur patiente à la source sans
   commencer ce décompte tant qu'aucun stock n'est disponible (pas d'aller-retour à vide). Au
   terme du chargement, `min(CapaciteMasseKg, CapaciteVolumeM3 × Densite)` quitte le site source.
3. **Trajet retour vers la destination** : même distance/terrain que le trajet aller (calcul
   symétrique), donc même durée.
4. **Déchargement** : durée fixe (même principe que le chargement) ; la charge rejoint le stock du
   bâtiment de destination.

Le cycle reprend alors automatiquement à l'étape 1, sans intervention du joueur.

## Transporteur — `TransporteurDefinition` (ScriptableObject, `Game.Logistics`)

Catalogue extensible (FR-044) : le colon (transporteur de base) est une première entrée ; les
futurs véhicules (camions...) suivront le même principe.

| Champ | Type | Description |
|---|---|---|
| `Id` | identifiant de catalogue | ex: `colonist`, futur `truck-basic`... |
| `CapaciteMasseKg` | `float` | Plafond de masse transportable (colon : 12 kg) |
| `CapaciteVolumeM3` | `float` | Plafond de volume transportable (colon : 0.012 m³, soit 12 L) |

**Règle de capacité** : la charge prise en une fois lors du chargement (cf. Cycle de transport
ci-dessous) pour une ressource donnée est `min(CapaciteMasseKg, CapaciteVolumeM3 × Densite)` — le
premier des deux plafonds atteint limite, jamais l'un des deux seul
(`Game.Logistics.TransportCapacityCalculator`).

## Vitesse de terrain — `TerrainSpeedCatalog` (ScriptableObject, `Game.Procedural`)

Catalogue de contenu (FR-044) associant à chaque `TerrainType` un modificateur de vitesse de
transport (principe fixé ici ; valeurs par terrain laissées à l'équilibrage). La vitesse moyenne
d'un trajet source → destination est la moyenne des modificateurs des cases traversées
(`Game.Procedural.TerrainRouting`, approximation en ligne droite — aucun pathfinding en Phase 1).

**Transitions** : la ressource ne quitte le site source que si une tâche de transport avec
capacité suffisante est active (FR-012/FR-014) ; sans tâche assignée, la ressource s'accumule sur
place (Edge Case).

## Trésorerie / Crédits Galactiques (`Game.Economy`)

« Crédits Galactiques » est la désignation narrative de cette même entité (FR-053) — pas une
devise distincte ; `BuildingDefinition.CoutCreditsGalactiques` est déduit de `Montant` au même
titre que `RevenuImpotParTick`/`CoutChomageParTick` l'alimentent ou le réduisent.

| Champ | Type | Description |
|---|---|---|
| `Montant` | `float` | Solde courant du joueur |
| `RevenuImpotParTick` | `float` (dérivé) | Somme des contributions des colons employés, modulées par leur rendement (FR-018) |
| `CoutChomageParTick` | `float` (dérivé) | Somme des coûts des colons sans emploi (FR-019) |

## Colonie (`Game.Civilization`)

| Champ | Type | Description |
|---|---|---|
| `Nom` | `string` | Donné par le joueur à la fondation ; sert d'ethnie par défaut héritée par les nouveau-nés |
| `NiveauDeveloppement` | `int` | Palier courant, fonction des ressources/trésorerie (FR-028/FR-029) — n'inclut PAS la population, qui évolue séparément via les naissances |
| `ProgresDeveloppement` | `float` | Progression vers le palier suivant |
| `Etat` | `ColonieEtat` (enum) | `EnCroissance` → `Stagnation` → `Effondrement`, relatif au `NiveauDeveloppement` uniquement (FR-030/FR-036, US9) |
| `Population` (dérivé) | `int` | Nombre de `Colon` vivants, alimenté exclusivement par la dotation initiale et le mécanisme de naissance (FR-046/FR-047), indépendamment de `Etat`/`NiveauDeveloppement` |
| `RatioGenre` (dérivé) | `float` | Proportion hommes/femmes courante parmi les `Colon`, recalculée à chaque naissance pour le rééquilibrage de quota (FR-048) |

**Transitions** : `EnCroissance → Stagnation` quand un besoin de base ou la trésorerie n'est plus
couvert (FR-030) ; `Stagnation → Effondrement` si la situation critique persiste au-delà d'un seuil
(FR-036, paramètre d'équilibrage) ; `Stagnation → EnCroissance` si la situation redevient positive
avant l'effondrement (comportement symétrique implicite de FR-030, à confirmer en tasks/tests) ;
`Effondrement` est un état terminal d'échec pour la partie (pas de condition de victoire, FR-036).
Ces transitions ne concernent que `NiveauDeveloppement` : `Population` continue d'évoluer via les
naissances (Cohabitation de logement) même en `Stagnation` ou `Effondrement` (FR-028/FR-049).

## Relations principales

- `Planete` 1—N `Zone` ; `Zone` 0..1 `Gisement de ressource` ; `Zone` 0..1 `Batiment`.
- `Gisement de ressource` N—1 `Technologie` (déblocage) et N—1 `RessourceDefinition`.
- `Batiment` 0..N `PosteEmploi` ; `PosteEmploi` 0..1 `Colon` (occupant).
- `Colon` 1—N `NiveauCompetence` (un par métier connu) ; `Colon` 0..1 `Affectation` (emploi,
  transport ou formation) ; `Colon` 0..1 `Formation` active.
- `TacheDeTransport` N—1 `Batiment` (source) et N—1 `Batiment` (destination) ; 0..1 `Colon` ou
  véhicule assigné.
- `Recette` 1—1 `Batiment` (bâtiment de transformation) ; N `RessourceDefinition` en entrée/sortie.
- `Colonie` 1—1 `Tresorerie` ; `Colonie` 1—N `Colon` ; `Colonie` 1—1 `Planete`.
- `Batiment` 1—1 `BuildingDefinition` (catalogue) ; `BuildingDefinition` 0..N `TechnologyDefinition` (prérequis, FR-044).
- `Batiment` (logement, `EstLogement = true`) 1—1 `Cohabitation de logement` ; `Colon` 0..1 `LogementId` référençant un tel `Batiment` (0..N `Colon` résidents par logement).
- `Colon` 0..1 `Affectation` de type `Construction` ciblant un `Batiment` en état `EnChantier`.
- Module de survie (`Batiment` avec `EstAbriInitial = true`) 1—1 `Inventaire` ; 1—5 `ExtracteurMultifonction`.
- `ExtracteurMultifonction` 0..1 `Gisement de ressource` (via `GisementCibleId`, uniquement eau/pierre/bois).
