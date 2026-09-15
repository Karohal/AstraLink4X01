# Contract: Interfaces des services métier (C# pur)

Ce projet n'expose pas d'API externe (jeu solo hors ligne) ; le « contrat » pertinent ici est la
frontière entre la logique métier pure (`Game.<Module>`, testable en EditMode, Principe II) et
l'orchestration `MonoBehaviour`. Ces interfaces fixent cette frontière et servent de base aux
tests de contrat en `Assets/_Project/Tests/EditMode/`. Signatures indicatives (C#-like), affinées
en implémentation ; les types `*Id` sont des `Guid` ou des références vers des `ScriptableObject`
de définition (cf. data-model.md).

## Game.Core

```csharp
public interface ISaveLoadService
{
    void Save(GameStateSnapshot state, string slotName);
    GameStateSnapshot Load(string slotName);
    bool TryMigrate(int fromSchemaVersion, JObject raw, out JObject migrated);
}

public interface ISimulationClock
{
    bool IsPaused { get; }
    float SpeedMultiplier { get; } // 1x/2x/3x
    void Pause();
    void Resume();
    void SetSpeed(float multiplier);
    event Action<float> OnTick; // deltaSimTime
}
```

## Game.Procedural

```csharp
public interface IPlanetGenerationService
{
    Planet Generate(int seed, int width, int height);
}
```

## Game.FogOfWar

```csharp
public interface IFogOfWarService
{
    bool IsRevealed(Planet planet, int x, int y);
    void RevealAround(Planet planet, int x, int y, int radius); // appelé à la construction (FR-004)
}
```

## Game.Building

```csharp
public interface IBuildingPlacementService
{
    bool CanBuild(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory, Treasury treasury); // vérifie le coût en ressources (FR-006) ET en Crédits Galactiques (FR-053) ; le catalogue peut aussi porter des PrerequisTechnologiques (FR-044) pour de futurs types de bâtiments, sans que cela soit exercé par le contenu connu de la Phase 1 au-delà du cas extracteur déjà couvert par IExtractionService.CanBuildExtractor
    Building Build(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory, Treasury treasury); // déduit le coût en ressources ET en Crédits Galactiques (BuildingDefinition.CoutCreditsGalactiques), démarre le bâtiment en état EnChantier (FR-005/FR-042/FR-053)
}

public interface IConstructionSiteService
{
    void Tick(Building buildingUnderConstruction, float deltaSimTime); // ProgresChantier n'avance que si un colon a une Affectation de type Construction ciblant ce bâtiment ; transition EnChantier -> Operationnel une fois DureeChantier atteinte (FR-042/FR-043)
}

public interface IProductionService
{
    void Tick(Building transformationBuilding, float deltaSimTime); // FR-015/FR-016
}
```

## Game.Research

```csharp
public interface IResearchService
{
    void ContributeProgress(Technology technology, float amount); // alimenté par le rendement du colon-chercheur (FR-008)
    bool IsUnlocked(Technology technology);
}
```

## Game.Economy

```csharp
public interface IExtractionService
{
    bool CanBuildExtractor(Deposit deposit, Technology technology); // FR-009
    void Tick(Deposit deposit, Building extractor, float deltaSimTime); // FR-010/FR-011 — extracteur fixe (construit via chantier)
    void Tick(Deposit deposit, MultiPurposeExtractor extractor, float deltaSimTime); // FR-051/FR-052 — surcharge pour un Extracteur multifonction placé/déplacé sans chantier (pas de Building)
}

public interface ITreasuryService
{
    void ApplyEmploymentIncome(IEnumerable<Colonist> employed, float deltaSimTime); // FR-018
    void ApplyUnemploymentCost(IEnumerable<Colonist> unemployed, float deltaSimTime); // FR-019
    bool IsCritical(); // seuil déclenchant l'alerte / la voie vers l'effondrement (FR-030/FR-036)
}
```

## Game.Colonists

```csharp
public interface ISkillProgressionService
{
    void ApplyOnTheJobProgress(Colonist colonist, JobDefinition job, float deltaSimTime); // FR-022, plafond bas
    void ApplyUniversityProgress(Training training, float deltaSimTime); // FR-024, plafond haut
    float ComputeYield(Colonist colonist, JobDefinition job); // f(compétence, santé) — FR-026/FR-027
}

public interface IAssignmentService
{
    // AssignManually (écrit Colonist.AffectationActuelle, cible un poste, un transport, un chantier
    // ou une formation) est disponible dès les fondations, réutilisé par US2 (chantier), US3
    // (chercheur), US4 (transport) avant même que SetAssignmentMode/RunAutomaticAssignmentPass
    // (FR-035, ci-dessous) n'existent.
    void AssignManually(Colonist colonist, IAssignable target);

    void SetAssignmentMode(Colonist colonist, AssignmentMode mode); // FR-035
    void RunAutomaticAssignmentPass(IEnumerable<Colonist> automaticColonists, IEnumerable<IAssignable> openTargets);
}

public interface IColonistIdentityService
{
    Colonist CreateColonist(EthnicityDefinition colonyEthnicity, Gender? gender = null); // nom + ethnie auto (FR-039/FR-040) ; genre tiré ~50/50 si non fourni (FR-045/FR-046), ou imposé par IHousingBirthService avec rééquilibrage de quota pour un nouveau-né (FR-048)
    void Rename(Colonist colonist, string newName);
    void SetBiography(Colonist colonist, string text); // troncature/validation ≤ 300 en amont (FR-041)
}

public interface IHousingService
{
    void AssignResident(Colonist colonist, Building housing); // écrit Colonist.LogementId ; housing doit avoir BuildingDefinition.EstLogement = true
    void RemoveResident(Colonist colonist);
}

public interface IHousingBirthService
{
    void Tick(Building housing, float deltaSimTime); // fait progresser DureeCohabitationContinue si le logement héberge au moins un homme et une femme, la remet à zéro sinon (FR-047)
    Gender PickNewbornGender(Colony colony); // aléatoire, rééquilibré vers le genre sous-représenté au-delà de ±10% (FR-048)
    // Ne lit jamais Treasury ni Inventory : le déclenchement d'une naissance est indépendant des ressources/de la trésorerie (FR-049)
}
```

## Game.Logistics

```csharp
public interface ITransportService
{
    TransportTask Assign(Building source, Building destination, ResourceDefinition resource, IAssignable hauler); // FR-013
    void Tick(TransportTask task, float deltaSimTime); // FR-012/FR-014
}
```

## Game.Civilization

```csharp
public interface ICivilizationService
{
    void Tick(Colony colony, Treasury treasury, float deltaSimTime); // fait évoluer NiveauDeveloppement uniquement (FR-028/FR-029/FR-030) — ne touche jamais Population, alimentée séparément par IHousingBirthService (FR-049)
    void EvaluateCollapse(Colony colony, Treasury treasury); // FR-036
}
```

## Règles de contrat

- Toute méthode de `Tick`/progression DOIT être pure vis-à-vis d'Unity (aucun appel à `UnityEngine.*`
  hormis des types de données comme `Vector2Int` si nécessaire) pour rester testable en EditMode
  sans scène chargée (Principe II).
- Les `MonoBehaviour` d'orchestration appellent ces interfaces depuis `Update`/événements UI
  Toolkit, mais n'implémentent eux-mêmes aucune règle métier.
