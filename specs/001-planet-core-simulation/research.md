# Phase 0 Research: Cœur de simulation Phase 1 — planète solo

## 1. Modèle de simulation temporelle

**Decision**: Simulation en temps réel continu (tick fixe côté logique, ex. `FixedUpdate`-like),
avec pause et contrôle de vitesse (x1/x2/x3) géré par `Game.Core`.

**Rationale**: Le spec ancre explicitement le jeu « dans l'esprit de SimCity » (vue god mode,
pas de tour par tour) ; toutes les Acceptance Scenarios du spec s'expriment en « When le temps de
jeu s'écoule » plutôt qu'en « When le joueur termine son tour ». Un temps continu avec pause est
aussi le standard des jeux de gestion/4X en vue de gestion de ce type.

**Alternatives considered**: Tour par tour classique (rejeté : contredit la référence SimCity et
la formulation temporelle du spec) ; temps réel sans pause (rejeté : un jeu de gestion solo doit
permettre au joueur de réfléchir sans pression, la pause est un standard attendu).

## 2. Représentation de la planète (zones/grille)

**Decision**: Grille de tuiles carrées (coordonnées entières `(x, y)`), chaque tuile référençant
un type de terrain et, le cas échéant, un gisement de ressource. Le brouillard de guerre est un
état booléen par tuile.

**Rationale**: Une grille carrée est la structure la plus simple à générer procéduralement, à
sérialiser (tableau 2D), à requêter pour le rayon de dissipation du brouillard de guerre (disque
de tuiles), et à tester unitairement. Le spec ne requiert aucune mécanique dépendant d'une
géométrie hexagonale (pas de portée d'attaque, pas de coûts de déplacement direction-dépendants en
Phase 1 puisqu'il n'y a pas de personnage qui se déplace). Reste un détail d'implémentation
(assumption du spec) sans impact sur les Functional Requirements.

**Alternatives considered**: Grille hexagonale (rejetée pour Phase 1 : complexité supplémentaire
sans bénéfice fonctionnel identifié dans le spec) ; régions polygonales irrégulières façon carte
de Risk (rejetée : bien plus complexe à générer procéduralement et à raisonner pour le rayon de
brouillard de guerre, sans besoin exprimé).

## 3. Sérialisation de la sauvegarde

**Decision**: `Newtonsoft.Json` via le package `com.unity.nuget.newtonsoft-json`, à ajouter au
projet (non présent dans le manifest actuel).

**Rationale**: FR-031 exige la persistance d'un état riche et profondément imbriqué (brouillard de
guerre par tuile, colons avec compétences par métier, chaînes de production, formations en cours,
etc.). Le `JsonUtility` intégré à Unity ne sérialise ni les dictionnaires, ni le polymorphisme, ni
les types imbriqués complexes sans contournements fragiles. `Newtonsoft.Json` est le standard de
facto dans l'écosystème Unity pour ce niveau de complexité et reste un fichier texte local simple
(aucune base de données, conforme à Storage: N/A base de données).

**Alternatives considered**: `JsonUtility` (rejeté : limitations sur dictionnaires/polymorphisme
qui obligeraient à un code de conversion manuel important, fragile face à l'évolution du modèle de
données) ; sérialisation binaire custom (rejetée : complique le debug solo et la compatibilité
ascendante des sauvegardes entre versions, sans bénéfice de performance nécessaire à l'échelle
Phase 1).

## 4. Interface utilisateur

**Decision**: UI Toolkit (`com.unity.modules.uielements`, déjà présent dans le projet) pour
l'ensemble des fenêtres de gestion (ressources, liste des colons, fiche détaillée de colon,
panneaux de construction/recherche).

**Rationale**: UI Toolkit est le framework UI moderne recommandé par Unity pour les interfaces
denses en données (listes, fiches détaillées, formulaires) typiques d'un jeu de gestion, avec un
modèle proche du web (USS/UXML) plus adapté à ce volume d'écrans que l'uGUI historique. Le module
est déjà présent dans le projet (`com.unity.modules.uielements`), aucun package supplémentaire
n'est nécessaire. Cela n'empêche pas d'utiliser `com.unity.ugui` ponctuellement si un besoin
spécifique (ex: effets in-world) apparaît en implémentation.

**Alternatives considered**: uGUI classique (rejeté comme choix par défaut : plus verbeux pour des
listes/fiches détaillées data-driven comme la fiche colon, bien que le package reste disponible en
secours) ; Visual Scripting (`com.unity.visualscripting`, présent) pour l'UI (rejeté : le Principe
II exige une logique testable en C# pur, peu compatible avec un flux visuel pour cette couche).

## 5. Transport / logistique

**Decision**: Tâches de transport modélisées comme des entités `Game.Logistics` pures (site
source, site destination, ressource, capacité, colon ou véhicule assigné), sans dépendance à
`com.unity.ai.navigation` (NavMesh) en Phase 1 ; le déplacement visuel d'un colon/véhicule entre
deux tuiles peut s'appuyer sur une interpolation simple le long du chemin le plus court sur la
grille (Constat 2), le NavMesh étant réservé à un besoin futur (terrain non-grille, obstacles
dynamiques complexes).

**Rationale**: Le spec ne demande aucun évitement d'obstacles dynamique complexe pour le transport
en Phase 1 (pas de combat, pas d'unités ennemies) ; un calcul de chemin sur grille (ex. A*) suffit
et reste entièrement testable en C# pur (Principe II), sans dépendance à un système de navigation
Unity plus lourd à piloter depuis des tests EditMode.

**Alternatives considered**: NavMesh (`com.unity.ai.navigation`, déjà dans le manifest) pour le
pathfinding des transporteurs (rejeté pour Phase 1 : sur-dimensionné pour une grille régulière sans
obstacles dynamiques, complique les tests unitaires purs ; le package reste disponible si un besoin
apparaît en Phase 3+ avec un terrain plus complexe).

## 6. Génération d'identité des colons (nom/ethnie)

**Decision**: Données de nommage/ethnies pilotées par ScriptableObject (`Assets/_Project/
ScriptableObjects/Identity/`), un objet par ethnie contenant des listes de prénoms/noms ; le
service `Game.Colonists.IdentityGenerationService` (C# pur) tire un nom aléatoire dans les listes
de l'ethnie de la colonie à la création d'un colon.

**Rationale**: Cohérent avec le Principe III (« un ScriptableObject par type de donnée de
configuration ») et permet d'étoffer/traduire les listes de noms sans toucher au code, y compris
pour de futures ethnies ajoutées en Phase 2+.

**Alternatives considered**: Génération procédurale de noms par règles phonétiques (rejetée pour
Phase 1 : complexité et risque de noms peu crédibles, sans besoin exprimé dans le spec) ; service
web de génération de noms (rejeté : violerait FR-032, le jeu doit fonctionner hors ligne).

## 7. Plateforme cible

**Decision**: PC Windows standalone pour le développement et les tests Phase 1 (poste de
développement actuel), build Unity Standalone.

**Rationale**: Le spec ne spécifie pas de plateforme ; le contexte projet (poste de développement
Windows, projet solo, pas de contrainte mobile/console mentionnée) rend ce choix par défaut évident
et sans risque, un build Standalone Unity restant portable à d'autres plateformes desktop sans
changement d'architecture si besoin plus tard.

**Alternatives considered**: Mobile/tablette (rejeté : aucune mention dans le spec ni la
Constitution, et l'interface dense en données — listes, fiches détaillées — est mieux adaptée à un
écran desktop pour cette phase).
