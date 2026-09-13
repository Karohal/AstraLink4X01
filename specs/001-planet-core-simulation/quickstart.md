# Quickstart: Validation Phase 1 — planète solo

Guide de validation manuelle dans l'éditeur Unity, un scénario par User Story du
[spec.md](./spec.md). À exécuter après l'implémentation des tâches correspondantes
(`/speckit-tasks` puis `/speckit-implement`). Ne remplace pas les tests automatisés EditMode/
PlayMode (cf. [contracts/core-interfaces.md](./contracts/core-interfaces.md)) : sert à prouver le
comportement de bout en bout dans le jeu réel.

## Prérequis

- Unity 6000.3.9f1 ouvert sur ce projet, scène de jeu Phase 1 chargée.
- Build/Play Mode depuis l'éditeur (pas besoin de build standalone pour valider).
- Une partie neuve démarrée (aucune sauvegarde préalable requise pour les scénarios 1 à 9).

## 1. Fonder la colonie et révéler le territoire (US1)

1. Lancer une nouvelle partie.
2. Observer la planète : une zone de départ autour de l'abri de secours initial doit être visible,
   le reste masqué par le brouillard de guerre.
3. Construire un bâtiment en bordure de la zone visible.
4. **Attendu** : le brouillard de guerre se dissipe automatiquement dans un rayon autour du
   nouveau bâtiment (SC-001).
5. Sauvegarder puis recharger la partie : l'état du brouillard de guerre doit être identique.

## 2. Construire des bâtiments (US2)

1. Avec le stock de départ, sélectionner un type de bâtiment et le placer sur une zone révélée.
2. **Attendu** : le stock de ressources diminue du coût affiché, le bâtiment apparaît.
3. Retenter une construction dont le coût dépasse le stock disponible.
4. **Attendu** : la construction est refusée, les ressources manquantes sont indiquées (FR-006).

## 3. Débloquer et exploiter des ressources (US3)

1. Ouvrir la fenêtre de gestion des colons depuis l'abri de secours initial (cf. scénario 9).
2. Assigner un colon au métier de chercheur.
3. Laisser le temps de jeu s'écouler jusqu'au déblocage d'une technologie d'extraction.
4. Tenter de construire un extracteur sur le gisement correspondant AVANT le déblocage : doit être
   refusé (FR-009). Après déblocage : doit réussir.
5. **Attendu** : une fois l'extracteur construit, le stock de la ressource associée augmente
   progressivement à son point de production (SC-002).

## 4. Transporter les ressources (US4)

1. Sans assigner de transport, observer que la ressource produite par l'extracteur (scénario 3)
   s'accumule sur place et n'atteint aucun stockage.
2. Assigner un colon ou véhicule au transport entre l'extracteur et un bâtiment de stockage.
3. **Attendu** : le stock global de la ressource au bâtiment de stockage augmente (SC-003).

## 5. Développer l'économie et l'industrie (US5)

1. Construire un bâtiment de transformation consommant la ressource brute du scénario 4.
2. Assigner un transport de la ressource brute vers ce bâtiment.
3. **Attendu** : le bâtiment consomme la ressource brute et produit la ressource transformée tant
   que l'approvisionnement suit (SC-005) ; couper le transport en entrée doit mettre la production
   en pause, puis la reprendre automatiquement une fois rétabli (FR-016).

## 6. Gérer l'emploi, la fiscalité et le rendement (US6)

1. Assigner un colon sans expérience à un poste ouvert.
2. **Attendu** : la trésorerie augmente périodiquement (impôt), modulée par la compétence/santé du
   colon (SC-006) ; la compétence du colon progresse lentement vers un plafond bas.
3. Laisser un autre colon au chômage.
4. **Attendu** : la trésorerie diminue périodiquement tant qu'il reste sans emploi (FR-019).
5. Comparer deux bâtiments identiques, l'un avec des colons compétents/en bonne santé, l'autre
   avec des colons peu compétents/en mauvaise santé : le second doit produire moins malgré 100%
   de postes occupés (SC-008).

## 7. Former les colons via l'université (US7)

1. Faire progresser la colonie jusqu'au palier débloquant l'université, puis la construire.
2. Assigner un colon disponible à une formation ciblant un métier.
3. **Attendu** : un coût est déduit de la trésorerie, la compétence progresse plus vite et
   au-delà du plafond de l'apprentissage sur le tas (SC-007).
4. Retirer le colon de la formation en cours.
5. **Attendu** : la formation s'arrête immédiatement, la compétence acquise est conservée
   (FR-025).

## 8. Développer la civilisation (US8)

1. Maintenir la colonie approvisionnée et financièrement positive sur plusieurs cycles.
2. **Attendu** : la population/le niveau de développement progresse, débloquant un nouveau
   bâtiment ou une capacité (SC-009).
3. À l'inverse, dans une partie séparée, priver la colonie de ressources vitales et laisser la
   trésorerie négative durablement.
4. **Attendu** : la colonie entre en état d'échec (effondrement), clairement signalé au joueur ;
   aucun état de victoire n'existe (SC-015).

## 9. Consulter et gérer chaque colon individuellement (US9)

1. Depuis l'abri de secours initial, ouvrir la fenêtre de gestion.
2. **Attendu** : la liste des ressources disponibles et la liste des colons par nom s'affichent
   (SC-013).
3. Cliquer sur un colon : sa fiche détaillée doit afficher santé, compétence par métier,
   éducation/formation en cours, ethnie et biographie.
4. Basculer son mode d'assignation entre manuel et automatique via le contrôle dédié.
5. **Attendu** : le mode choisi est respecté par le système d'assignation (SC-012).
6. Renommer le colon et éditer sa biographie jusqu'à la limite de 300 caractères (la saisie doit
   se bloquer à la limite, sans troncature ni message d'erreur).
7. **Attendu** : le nouveau nom et la biographie sont conservés après rechargement, sans effet sur
   la compétence, la santé ou le rendement du colon (SC-014).

## Validation transverse

- **Sauvegarde/chargement** (SC-010) : à n'importe quel moment des scénarios 1 à 9, sauvegarder
  puis recharger la partie et vérifier qu'aucun état n'est perdu ou incohérent.
- **Hors ligne** (SC-011) : dérouler l'intégralité des scénarios 1 à 9 sans connexion réseau
  active, aucune fonctionnalité ne doit être bloquée.
- **Hors périmètre** : vérifier qu'aucune option militaire/combat n'est accessible en Phase 1
  (Out of Scope du spec).
