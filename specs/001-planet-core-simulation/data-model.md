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
| `DureeChantier` | `float` | Durée de chantier propre à ce type de bâtiment ; valeur de contenu/équilibrage, pas fixée par la spec (FR-042) |
| `PrerequisTechnologiques` | référence(s) `TechnologyDefinition`, optionnel | Technologie(s) devant être débloquée(s) avant de pouvoir lancer ce type de bâtiment (FR-044), en plus du cas particulier des extracteurs (gisement + technologie, FR-009) |
| `RayonBrouillard` | `int` | Rayon de dissipation du brouillard de guerre à la construction (FR-004) |
| `PostesEmploiDefinis` | `JobDefinition[]` | Postes ouverts par ce type de bâtiment (dont chercheur, université) |
| `EstLogement` | `bool` | Vrai si ce type de bâtiment est un logement, site du mécanisme de naissance (FR-047) |

Ce catalogue est une structure de données extensible et séparée de la spec/du plan (FR-044) : de
nouveaux `BuildingDefinition` peuvent être ajoutés au fil du développement sans modifier cette
spécification ni le plan technique ; seul le mécanisme générique de déblocage (ressources, et le
cas échéant technologie/gisement) reste fixé ici.

## Abri de secours initial / Bâtiment (`Game.Building`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant du bâtiment |
| `DefinitionId` | référence `BuildingDefinition` (ScriptableObject) | Type de bâtiment (coût, chantier, prérequis, rayon, postes — cf. ci-dessus) |
| `Position` | `(int x, int y)` | Zone occupée |
| `Etat` | `BatimentEtat` (enum) | `EnChantier` → `Operationnel` ; pour les bâtiments de transformation : `Operationnel` ↔ `EnPause` (FR-016) |
| `ProgresChantier` | `float` | Progression du chantier, de 0 à `DureeChantier` ; n'avance que si au moins un colon est assigné (FR-042/FR-043) |
| `PostesEmploi` | `PosteEmploi[]` | Postes ouverts par ce bâtiment une fois opérationnel (dont chercheur, université) |
| `EstAbriInitial` | `bool` | Vrai uniquement pour le bâtiment de départ (FR-007/FR-037) |

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
| `DureeCohabitationContinue` | `float` | Temps continu écoulé depuis qu'au moins un `Colon` de genre `Homme` et un de genre `Femme` ont `LogementId` pointant vers ce bâtiment ; remis à zéro dès que cette condition n'est plus vraie (FR-047) |

**Transitions** : une naissance est déclenchée quand `DureeCohabitationContinue` atteint un an de
temps de jeu (FR-047) ; le nouveau colon créé via `IColonistIdentityService.CreateColonist` hérite
de l'ethnie de la `Colonie` (FR-040) et reçoit un genre tiré aléatoirement avec rééquilibrage de
quota (FR-048), indépendamment de l'état des ressources/de la trésorerie de la colonie (FR-049).

## Tâche de transport (`Game.Logistics`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant |
| `RessourceId` | référence `RessourceDefinition` | Ressource transportée |
| `SiteSourceId` | référence `Batiment` | Origine (extracteur, bâtiment de production) |
| `SiteDestinationId` | référence `Batiment` | Destination (stockage, bâtiment consommateur) |
| `AssigneA` | `ColonId?` \| `VehiculeId?` | Colon ou véhicule assigné (FR-013) |
| `CapaciteParCycle` | `float` | Volume transportable par unité de temps |

**Transitions** : la ressource ne quitte le site source que si une tâche de transport avec
capacité suffisante est active (FR-012/FR-014) ; sans tâche assignée, la ressource s'accumule sur
place (Edge Case).

## Trésorerie (`Game.Economy`)

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
