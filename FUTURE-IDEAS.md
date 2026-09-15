# Idées futures — AstraLink4X

Mémoire de travail informelle : idées et mécaniques évoquées en discussion mais volontairement
mises de côté pour une phase ultérieure (cf. CLAUDE.md — développement par phases). Rien ici n'est
une spec ni un plan validé ; pas de format Spec Kit à respecter. Objectif : ne rien oublier, sans
obligation de rien implémenter maintenant.

À enrichir au fil du développement quand une idée hors scope de la phase en cours est évoquée.

---

## Terrain & monde

### Terrain organique et zones de transition
Lacs aux formes arrondies plutôt que des cases carrées à un seul type de terrain, avec des zones de
transition (sable, gravier/plage) autour de l'eau. Nécessaire pour que le placement de bâtiments
comme les pompes reste cohérent visuellement (ex: une pompe posée sur une plage plutôt qu'au milieu
de l'eau, au lieu d'une case "eau" adjacente à une case "prairie" sans transition).
**Contexte** : évoqué en discutant du modèle de pompe/gisement d'eau (cf. commit `c6cec88`,
"pump model") et du système de zones/terrain de `data-model.md` (grille de `Zone` à un seul
`TerrainType` par case).

### Échelle planétaire réaliste
Vision long terme où faire le tour de la planète prend plusieurs heures de jeu réel, par opposition
à la carte restreinte (grille finie) de la Phase 1.
**Contexte** : déjà noté dans `specs/001-planet-core-simulation/spec.md` § Assumptions ("La planète
est représentée par un ensemble fini de zones... pas un monde ouvert continu sans limites").

---

## Construction & urbanisme

### Déformation de terrain et réseau routier
Inspiration *Workers & Resources: Soviet Republic* : possibilité de niveler le sol sous un bâtiment
pour éviter les parties en porte-à-faux/dans le vide, et un réseau de routes reliant les bâtiments
entre eux (avec pathfinding routier). Va plus loin que le système de placement libre déjà en cours
d'implémentation (rotation/positionnement dans la case) : nécessite un terrain réellement
déformable et un système de routes/pathfinding dédié.
**Contexte** : évoqué en discutant de l'évolution du système de placement de bâtiments au-delà de
la grille de zones actuelle (T029 `BuildingPlacementView`).

---

## Module de survie

### Démantèlement du Module de survie
Une fois les colons relogés dans de vraies habitations (US7), possibilité de démanteler le Module
de survie initial pour en récupérer une partie des ressources.
**Contexte** : évoqué lors de la clarification de conception du Module de survie/Extracteurs
multifonction (2026-09-15) ; explicitement noté comme hors périmètre dans
`specs/001-planet-core-simulation/spec.md` § Assumptions et `data-model.md` § Module de survie —
volontairement non détaillé ni implémenté pour l'instant.

---

## Militaire & conquête (Phase 3+)

### Système militaire complet
Troupes composées de colons assignés recevant une solde (même principe que l'emploi civil, US6),
coût limitant le nombre de troupes viables, mécaniques de combat/conquête. Hors scope tant que la
Phase 1 (et probablement la Phase 2) ne sont pas terminées et jouables.
**Contexte** : mentionné dans la vision long terme du jeu dès le début du projet ; explicitement
listé en "Out of Scope (Phase 1)" dans `spec.md`, avec renvoi à la Constitution (Principe I —
développement par phases).

---

## Multijoueur & persistance (Phase 3+)

### Résilience post-destruction
Une fois le multijoueur implémenté (Phase 2+), outils primitifs permettant à un joueur de
reconstruire une base de population s'il se fait "rayer de la carte" par un autre joueur.
**Contexte** : évoqué en pensant à l'équilibrage multijoueur à venir, en lien avec le système
militaire (troupes/conquête) — dépend directement de mécaniques (militaire, multi restreint) qui
n'existent pas encore en Phase 1.
