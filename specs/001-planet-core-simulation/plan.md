# Implementation Plan: Cœur de simulation Phase 1 — planète solo

**Branch**: `001-planet-core-simulation` | **Date**: 2026-09-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-planet-core-simulation/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Boucle de jeu solo en vue de gestion (god mode) sur une planète unique générée procéduralement :
brouillard de guerre dissipé par la construction (chantiers progressifs nécessitant de la
main-d'œuvre, catalogue de bâtiments extensible), recherche/extraction de ressources, transport
manuel, industrie/production, emploi/fiscalité avec compétence et santé des colons, université,
naissance de colons par cohabitation en logement (indépendante de l'économie), développement de la
civilisation, gestion/identité des colons — le tout hors ligne, sans militaire ni multijoueur.
Approche technique : simulation en temps réel (pause possible) pilotée par des services C# purs
par module (`Game.<Module>`), orchestrés par des `MonoBehaviour` légers, avec sauvegarde locale en
JSON et UI via Unity UI Toolkit.

## Technical Context

**Language/Version**: C# (profil scripting .NET Standard 2.1, Unity 6000.3.9f1)

**Primary Dependencies**: Unity 6000.3.9f1 ; Universal Render Pipeline 17.3.0 (com.unity.render-pipelines.universal) ; Input System 1.18.0 (com.unity.inputsystem) ; AI Navigation 2.0.10 (com.unity.ai.navigation) ; Unity Test Framework 1.6.0 (com.unity.test-framework) ; UI Toolkit (com.unity.modules.uielements, déjà présent) ; sérialisation JSON via Newtonsoft.Json (com.unity.nuget.newtonsoft-json, cf. research.md §3)

**Storage**: fichiers de sauvegarde locaux (JSON) dans le dossier de données persistantes de l'application ; aucune base de données, aucun serveur ; sérialisation via Newtonsoft.Json (cf. research.md §3)

**Testing**: Unity Test Framework (NUnit) — tests EditMode pour toute la logique métier pure (`Game.<Module>`, sans dépendance à une scène) ; tests PlayMode limités pour l'orchestration `MonoBehaviour` (câblage input/UI/rendu)

**Target Platform**: PC Windows standalone (poste de développement solo actuel) ; portage futur non exclu mais hors périmètre Phase 1

**Project Type**: application de jeu Unity mono-projet (desktop-app) — pas de web/mobile/bibliothèque séparée

**Performance Goals**: 60 FPS stables avec jusqu'à ~200 colons, quelques centaines de bâtiments et de tâches de transport actives simultanément sur une seule planète (hypothèse de dimensionnement Phase 1, à ajuster en équilibrage — cf. Assumptions du spec)

**Constraints**: fonctionnement 100% hors ligne (FR-032) ; toute logique métier DOIT vivre hors des `MonoBehaviour`, dans des classes C# pures testables (Principe II) ; conventions de nommage/dossiers/namespaces de la Constitution (Principe III) ; aucune mécanique militaire/combat (FR-034)

**Scale/Scope**: une planète unique (grille finie de zones), une seule colonie, population bornée à l'échelle Phase 1 (dizaines à ~200 colons) ; pas de multi-planètes, pas de multijoueur

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Évaluation | Statut |
|---|---|---|
| I. Développement par phases | Le plan ne couvre que le périmètre Phase 1 du spec (planète unique, hors ligne, pas de militaire/multijoueur) ; aucune mécanique de Phase 2+ n'est introduite. | PASS |
| II. Séparation logique / présentation | Le plan place toute la logique (brouillard de guerre, recherche, extraction, transport, production, emploi, compétence/santé, trésorerie, identité des colons) dans des classes C# pures par module ; les `MonoBehaviour` se limitent à l'orchestration (scène, input, UI, rendu). Voir Project Structure. | PASS |
| III. Conventions de code et structure cohérentes | Namespaces `Game.<Module>` et dossiers `Assets/_Project/Scripts/<Module>/` respectés. La liste de modules de la Constitution (`Economy, Building, Procedural, Core`) est étendue avec de nouveaux modules requis par le périmètre du spec (`Colonists, Research, Logistics, Civilization`). | PASS (constitution v1.2.1 sanctionne explicitement ces modules) |
| IV. Spec-Driven Development | Ce plan fait suite à un spec.md validé (checklist qualité passée) ; aucun code n'est écrit avant tasks.md/implement. | PASS |
| V. Hygiène Git et gestion des assets | Aucune action Git/asset binaire n'est requise par ce plan ; les nouveaux dossiers `Assets/_Project/Scripts/*` génèreront leurs `.meta` automatiquement via l'éditeur Unity lors de l'implémentation, jamais édités à la main. | PASS |

**Post-Design Re-check** (après Phase 0/1) : `data-model.md` et `contracts/` confirment que toute
la logique (services `I*Service`) reste indépendante d'Unity (Principe II, PASS) et suit les
namespaces `Game.<Module>` retenus ci-dessus (Principe III, PASS — constitution v1.2.1). Aucune
mécanique militaire, multijoueur ou hors périmètre Phase 1 n'a été introduite par le design
(Principe I, PASS). Aucun nouveau gate n'est déclenché.

## Project Structure

### Documentation (this feature)

```text
specs/001-planet-core-simulation/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── savegame-schema.md
│   └── core-interfaces.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Assets/
  _Project/
    Scripts/
      Core/                  # Boucle de jeu, service de sauvegarde/chargement, horloge de
                              # simulation (pause/vitesse), utilitaires partagés, bus d'événements
      Procedural/             # Génération procédurale de la planète et de ses zones/gisements
      FogOfWar/                # État de visibilité des zones, dissipation autour des bâtiments
      Building/                # Placement, coût, cycle de vie des bâtiments (dont extracteurs,
                              # bâtiments de transformation, université)
      Economy/                 # Ressources, stocks, trésorerie, impôts/coût du chômage,
                              # chaînes de production/industrie
      Research/                # Technologies, progression de la recherche portée par les colons
      Colonists/                # Colon (identité, compétence, santé, mode d'assignation),
                              # emploi, formation universitaire
      Logistics/                # Tâches de transport (colons/véhicules) entre sites
      Civilization/             # Développement de la colonie (population, paliers, échec/
                              # effondrement)
    ScriptableObjects/
      Resources/               # Définitions de ressources (brutes/transformées)
      Buildings/                # Définitions de bâtiments (coût, effets, rayon de brouillard)
      Technologies/             # Définitions de technologies d'extraction
      Jobs/                     # Définitions de métiers (dont chercheur)
      Identity/                 # Listes de noms/ethnies pour la génération d'identité des colons
    Prefabs/
    Scenes/
  Art/
  Plugins/

Assets/_Project/Tests/
  EditMode/                    # Tests unitaires de la logique métier pure (un dossier miroir par
                              # module : Core, Procedural, FogOfWar, Building, Economy, Research,
                              # Colonists, Logistics, Civilization)
  PlayMode/                    # Tests d'intégration limités (orchestration MonoBehaviour ↔
                              # services purs, ex: clic sur un colon ouvre bien sa fiche)
```

**Structure Decision**: Projet Unity mono-application (pas de séparation front/back ni mobile).
La logique métier de chaque module du spec est isolée dans `Assets/_Project/Scripts/<Module>/`
en classes C# pures (Principe II), testées en EditMode sans dépendance de scène. Les
`MonoBehaviour` (sous chaque dossier de module, sous-répertoire implicite `Runtime/` au moment de
l'implémentation) ne font que relier ces services aux événements Unity (Update, input, UI Toolkit,
rendu). La Constitution énumère `Economy, Building, Procedural, Core` à titre d'exemple pour le
namespace `Game.<Module>` ; ce plan ajoute `FogOfWar, Research, Colonists, Logistics, Civilization`
comme modules de premier niveau, nécessaires pour refléter fidèlement les entités et FR du spec
sans surcharger les quatre dossiers existants (cf. Complexity Tracking).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Extension de la liste de dossiers `Scripts/` au-delà de `{Economy, Building, Procedural, Core}` (ajout de `FogOfWar, Research, Colonists, Logistics, Civilization`) | Le spec Phase 1 introduit des domaines métier distincts (brouillard de guerre, recherche/technologies, colons/compétence/santé/identité, transport/logistique, développement de la civilisation/échec) qui ne se rattachent naturellement à aucun des 4 dossiers existants sans les surcharger. | Tout faire tenir dans `Economy`, `Building`, `Procedural`, `Core` a été rejeté : cela mélangerait des responsabilités sans rapport (ex: compétence des colons et trésorerie dans `Economy`, brouillard de guerre et génération de planète dans `Procedural`), ce qui va à l'encontre de la clarté/maintenabilité visée par le Principe III lui-même. **Recommandation appliquée** : amendement effectué — voir `.specify/memory/constitution.md` v1.2.1 (Principe III), qui nomme désormais explicitement ces 5 modules comme exemples sanctionnés d'une liste non-exhaustive. |
