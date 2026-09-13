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
    bool CanBuild(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory);
    Building Build(Planet planet, BuildingDefinition definition, int x, int y, Inventory inventory); // FR-005/FR-006
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
    void Tick(Deposit deposit, Building extractor, float deltaSimTime); // FR-010/FR-011
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
    void SetAssignmentMode(Colonist colonist, AssignmentMode mode); // FR-035
    void AssignManually(Colonist colonist, IAssignable target);
    void RunAutomaticAssignmentPass(IEnumerable<Colonist> automaticColonists, IEnumerable<IAssignable> openTargets);
}

public interface IColonistIdentityService
{
    Colonist CreateColonist(EthnicityDefinition colonyEthnicity); // nom + ethnie auto (FR-039/FR-040)
    void Rename(Colonist colonist, string newName);
    void SetBiography(Colonist colonist, string text); // troncature/validation ≤ 300 en amont (FR-041)
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
    void Tick(Colony colony, Treasury treasury, float deltaSimTime); // FR-028/FR-029/FR-030
    void EvaluateCollapse(Colony colony, Treasury treasury); // FR-036
}
```

## Règles de contrat

- Toute méthode de `Tick`/progression DOIT être pure vis-à-vis d'Unity (aucun appel à `UnityEngine.*`
  hormis des types de données comme `Vector2Int` si nécessaire) pour rester testable en EditMode
  sans scène chargée (Principe II).
- Les `MonoBehaviour` d'orchestration appellent ces interfaces depuis `Update`/événements UI
  Toolkit, mais n'implémentent eux-mêmes aucune règle métier.
