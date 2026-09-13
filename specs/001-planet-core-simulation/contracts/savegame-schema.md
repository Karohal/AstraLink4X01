# Contract: Format de sauvegarde (JSON)

Contrat de persistance couvrant l'intégralité de l'état requis par FR-031/FR-033/SC-010. Sérialisé
via `Newtonsoft.Json` (cf. research.md §3) dans un fichier unique par partie, sous le dossier de
données persistantes de l'application (`Application.persistentDataPath`, hors dépôt Git).

## Enveloppe

```json
{
  "schemaVersion": 1,
  "savedAtUtc": "2026-09-13T00:00:00Z",
  "planet": { "...": "cf. section Planet" },
  "colony": { "...": "cf. section Colony" },
  "colonists": [ "...": "cf. section Colonist" ],
  "buildings": [ "...": "cf. section Building" ],
  "technologies": [ "...": "cf. section Technology" ],
  "transportTasks": [ "...": "cf. section TransportTask" ],
  "treasury": { "...": "cf. section Treasury" }
}
```

`schemaVersion` DOIT être incrémenté à chaque changement de structure incompatible ; le service de
chargement (`Game.Core.ISaveLoadService`) DOIT refuser ou migrer explicitement une sauvegarde dont
la version n'est pas supportée, plutôt que d'échouer silencieusement.

## Planet

```json
{
  "id": "guid",
  "seed": 12345,
  "width": 64,
  "height": 64,
  "zones": [
    { "x": 0, "y": 0, "terrainId": "plains", "revealed": true,
      "deposit": { "resourceId": "iron", "remaining": 850.0, "state": "ExtractorBuilt" },
      "buildingId": "guid-or-null" }
  ]
}
```

## Colony

```json
{
  "name": "Premier Pas",
  "developmentLevel": 1,
  "developmentProgress": 0.35,
  "state": "Growing"
}
```

`state` ∈ `Growing | Stagnating | Collapsed` (cf. data-model.md — Colonie.Etat) ; ne concerne que
`developmentLevel`/`developmentProgress`. La population et le ratio hommes/femmes ne sont pas
stockés ici : ils se déduisent de la liste `colonists` (FR-028/FR-049).

## Colonist

```json
{
  "id": "guid",
  "name": "Ael Voss",
  "gender": "Female",
  "ethnicityId": "colony-default",
  "biography": "",
  "health": 0.82,
  "skills": [ { "jobId": "researcher", "value": 0.4, "source": "OnTheJob" } ],
  "assignmentMode": "Manual",
  "currentAssignment": { "type": "Job", "targetId": "guid-posteEmploi" }
}
```

`gender` ∈ `Male | Female` (FR-045) ; `currentAssignment.type` ∈ `Job | Transport | Construction |
Training` (FR-043 ajoute `Construction`).

`biography` DOIT être ≤ 300 caractères (contrainte déjà appliquée côté saisie UI, FR-041) ;
`currentAssignment` est absent/`null` si le colon est au chômage.

## Building

```json
{
  "id": "guid",
  "definitionId": "extractor-iron",
  "x": 4, "y": 7,
  "state": "UnderConstruction",
  "constructionProgress": 3.5,
  "isStartingShelter": false,
  "productionRecipeState": { "inputBuffer": [...], "outputBuffer": [...] },
  "housingCohabitation": { "continuousDuration": 0.0 }
}
```

`state` ∈ `UnderConstruction | Operational` (FR-042) ; `constructionProgress` est absent/ignoré une
fois `Operational`. `housingCohabitation` n'est présent que pour un bâtiment dont la définition est
un logement (`isHousing: true` côté catalogue) et suit `DureeCohabitationContinue` (FR-047).

## Technology

```json
{ "id": "extraction-iron", "progress": 120.0, "unlocked": true }
```

## TransportTask

```json
{
  "id": "guid",
  "resourceId": "iron",
  "sourceBuildingId": "guid",
  "destinationBuildingId": "guid",
  "assignedColonistId": "guid-or-null",
  "assignedVehicleId": "guid-or-null"
}
```

## Treasury

```json
{ "amount": 1250.5 }
```

## Règles de compatibilité

- Tout ajout de champ optionnel est rétro-compatible sans changement de `schemaVersion`.
- Toute suppression/renommage de champ, ou changement de sémantique d'un champ existant, DOIT
  incrémenter `schemaVersion` et être accompagné d'une routine de migration documentée dans
  `Game.Core.ISaveLoadService`.
- Ce contrat n'expose aucune donnée réseau : il s'agit d'un format de fichier local uniquement
  (conforme à FR-032, hors ligne).
