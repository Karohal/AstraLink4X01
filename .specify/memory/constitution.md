<!--
Sync Impact Report
- Version change: 1.0.0 → 1.1.0
- Modified principles:
  - I. Développement par phases — scope de la Phase 1 redéfini : « un seul système solaire »
    → « une seule planète » ; contenu détaillé (exploration, extraction de ressources,
    construction, développement de la civilisation, économie et industrie), toujours sans
    réseau.
- Added sections: none
- Removed sections: none
- Bump rationale: MINOR — le cadre à 4 phases et les 5 principes restent inchangés ; seule la
  description du contenu de la Phase 1 est précisée/affinée (réduction du périmètre physique
  initial de « système » à « planète » et ajout de sous-systèmes de jeu attendus), sans
  supprimer ni redéfinir la structure de gouvernance elle-même.
- Templates requiring follow-up: none checked automatically by this command (out of scope).
- Follow-up TODOs: none. Note (non-governance, informative only): CLAUDE.md décrit encore la
  Phase 1 comme « un seul système solaire » — à aligner manuellement si souhaité (hors
  périmètre de cette commande, qui ne modifie que la constitution).
-->

# AstraLink4X Constitution

## Core Principles

### I. Développement par phases (NON-NÉGOCIABLE)
Le jeu final (galaxie complète, 1000 systèmes, 200 joueurs persistants) ne DOIT JAMAIS être
implémenté directement. Le projet avance par phases, chacune complète et jouable avant de
passer à la suivante :

1. **Phase 1 — Cœur de simulation (solo)** : une seule planète générée procéduralement.
   Portée fonctionnelle : exploration, extraction de ressources, construction, développement
   de la civilisation, économie et industrie. Aucun réseau.
2. **Phase 2 — Multi restreint** : même contenu, serveur dédié séparé, 4-10 joueurs sur un
   système partagé.
3. **Phase 3 — Extension** : plusieurs systèmes connectés, conquête, montée en charge.
4. **Phase 4 — Échelle galaxie** : 1000 systèmes, 200 joueurs persistants.

Si une demande dépasse la phase en cours, elle DOIT être signalée avant toute implémentation,
accompagnée d'une proposition de découpage en tâches compatibles avec la phase active.

**Rationale** : Projet solo, long terme, orienté qualité plutôt que vitesse. Livrer des
tranches petites et stables évite la dette technique et valide chaque brique avant de
monter en échelle vers un univers persistant multijoueur.

### II. Séparation logique / présentation
Les `MonoBehaviour` orchestrent uniquement ; ils ne contiennent AUCUNE logique métier. Toute
logique métier vit dans des classes C# pures, indépendantes d'Unity, testables unitairement.

**Rationale** : Facilite les tests unitaires dès la Phase 1 et permet, à partir de la Phase 2,
de porter la simulation vers un serveur dédié sans réécrire la logique métier.

### III. Conventions de code et structure cohérentes
- Namespace racine `Game.<Module>` (ex: `Game.Economy`, `Game.Building`, `Game.Procedural`).
- PascalCase pour classes/méthodes publiques, camelCase avec préfixe `_` pour les variables
  privées.
- Un ScriptableObject par type de donnée de configuration (ressources, bâtiments, unités).
- Structure des dossiers Unity fixée : `Assets/_Project/Scripts/{Economy,Building,Procedural,
  Core}/`, `ScriptableObjects/`, `Prefabs/`, `Scenes/` ; assets hors code dans `Art/` et
  `Plugins/`.
- Commentaires en français, noms de code (classes, méthodes, variables) en anglais.

**Rationale** : Sur un projet solo mené sur le long terme, la cohérence des conventions est ce
qui permet de rester productif des mois ou années plus tard, et facilite une éventuelle
reprise par d'autres développeurs.

### IV. Développement piloté par spécification (Spec-Driven Development)
Le projet utilise GitHub Spec Kit. Chaque feature vit dans `specs/00X-nom-feature/`. Le cycle
**specify → plan → tasks → implement** DOIT être suivi intégralement. Aucun code ne DOIT être
écrit pour une feature de la phase en cours sans spec validée au préalable.

**Rationale** : En solo, sans revue de pair, la spec validée en amont est le principal garde-
fou contre la dérive de scope et les implémentations qui débordent de la phase active
(cf. Principe I).

### V. Hygiène Git et gestion des assets
- Les fichiers `.meta` ne DOIVENT JAMAIS être modifiés manuellement.
- Un commit correspond à une tâche Spec Kit complète et fonctionnelle (pas de commits
  intermédiaires cassés).
- Git LFS DOIT être utilisé pour tout asset binaire (modèles, textures, audio).

**Rationale** : Unity gère l'intégrité du projet via les `.meta` ; des commits atomiques et
fonctionnels garantissent un historique fiable, un rollback sûr, et un dépôt qui reste léger
malgré les assets binaires.

## Portée et exigences techniques

Le jeu est un 4X spatial en Unity (C#), à terme un univers persistant en ligne (1000 systèmes
procéduraux, jusqu'à 200 joueurs par galaxie/serveur). Toute exigence technique ou de
conception DOIT être évaluée par rapport à la phase active (Principe I) : une fonctionnalité
réseau ou de mise à l'échelle proposée en Phase 1, par exemple, est hors périmètre tant que la
Phase 1 n'est pas complète et jouable.

## Workflow de développement

Le cycle de vie d'une feature suit Spec Kit : `/speckit-specify` → `/speckit-plan` →
`/speckit-tasks` → `/speckit-implement`, avec `/speckit-clarify`, `/speckit-checklist` et
`/speckit-analyze` utilisés au besoin pour lever les ambiguïtés et vérifier la cohérence avant
implémentation. Étant un projet solo, la revue de conformité aux Principes I-V est
auto-effectuée avant chaque commit et avant chaque passage à la phase suivante.

## Governance

Cette constitution prévaut sur toute autre pratique ou convention informelle du projet. Toute
modification DOIT :
1. Être documentée dans cette constitution (pas de règles non écrites).
2. Suivre le versionnage sémantique défini ci-dessous.
3. Se référer, pour les conventions de code détaillées, à `CLAUDE.md` (guide d'exécution
   quotidien) — cette constitution en fixe les principes non négociables, `CLAUDE.md` en donne
   le mode d'emploi opérationnel.

**Politique de versionnage** :
- **MAJOR** : suppression ou redéfinition rétro-incompatible d'un principe ou de la
  gouvernance.
- **MINOR** : ajout d'un principe ou extension matérielle d'une règle existante.
- **PATCH** : clarifications, reformulations, corrections non sémantiques.

**Revue de conformité** : chaque tâche Spec Kit implémentée DOIT être compatible avec les
Principes I-V avant d'être commitée ; toute violation détectée DOIT être corrigée ou justifiée
explicitement dans la spec de la feature concernée.

**Version**: 1.1.0 | **Ratified**: 2026-09-13 | **Last Amended**: 2026-09-13
