# Data Model: Cœur de simulation Phase 1 — planète solo

Entités extraites de la section *Key Entities* de [spec.md](./spec.md), avec champs, relations et
transitions d'état. Modélisation en classes C# pures (`Game.<Module>`), sans dépendance Unity,
conformément au Principe II de la Constitution. Les valeurs numériques précises (rayons, plafonds,
coûts, seuils) sont des paramètres de configuration (ScriptableObject) à ajuster en équilibrage —
seuls les champs et leur rôle sont fixés ici.

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

## Abri de secours initial / Bâtiment (`Game.Building`)

| Champ | Type | Description |
|---|---|---|
| `Id` | `Guid` | Identifiant du bâtiment |
| `DefinitionId` | référence `BuildingDefinition` (ScriptableObject) | Type de bâtiment, coût, rayon de brouillard de guerre, postes d'emploi disponibles |
| `Position` | `(int x, int y)` | Zone occupée |
| `Etat` | `BatimentEtat` (enum) | `EnConstruction` → `Operationnel` ; pour les bâtiments de transformation : `Operationnel` ↔ `EnPause` (FR-016) |
| `PostesEmploi` | `PosteEmploi[]` | Postes ouverts par ce bâtiment (dont chercheur, université) |
| `EstAbriInitial` | `bool` | Vrai uniquement pour le bâtiment de départ (FR-007/FR-037) |

**Transitions** : `EnConstruction → Operationnel` à la fin de la construction (US2) ;
`Operationnel ↔ EnPause` pour un bâtiment de transformation selon la disponibilité des intrants ou
la saturation du stock de sortie (FR-016).

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
| `EthnieId` | référence `EthnicityDefinition` (ScriptableObject) | Héritée de la colonie à la naissance (FR-040) |
| `Biographie` | `string` (≤ 300 caractères) | Libre, optionnelle, vide par défaut, saisie bornée côté UI (FR-041), aucun effet gameplay |
| `Sante` | `float` (0–1 ou 0–100) | Influence le rendement (FR-026) |
| `Competences` | `Dictionary<MetierId, NiveauCompetence>` | Un niveau par métier possible (FR-020) |
| `ModeAssignation` | `ModeAssignation` (enum `Manuel` \| `Automatique`) | Par colon, modifiable à tout moment (FR-035) |
| `AffectationActuelle` | `Affectation?` | Poste d'emploi, tâche de transport, ou formation en cours ; `null` = chômage |

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
| `Type` | `AffectationType` (enum `Emploi` \| `Transport` \| `Formation`) | Nature de l'affectation |
| `CibleId` | `Guid` | Poste, tâche de transport, ou formation concernée |

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
| `NiveauDeveloppement` | `int` | Palier courant (FR-028/FR-029) |
| `ProgresDeveloppement` | `float` | Progression vers le palier suivant |
| `Etat` | `ColonieEtat` (enum) | `EnCroissance` → `Stagnation` → `Effondrement` (FR-030/FR-036, US8) |

**Transitions** : `EnCroissance → Stagnation` quand un besoin de base ou la trésorerie n'est plus
couvert (FR-030) ; `Stagnation → Effondrement` si la situation critique persiste au-delà d'un seuil
(FR-036, paramètre d'équilibrage) ; `Stagnation → EnCroissance` si la situation redevient positive
avant l'effondrement (comportement symétrique implicite de FR-030, à confirmer en tasks/tests) ;
`Effondrement` est un état terminal d'échec pour la partie (pas de condition de victoire, FR-036).

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
