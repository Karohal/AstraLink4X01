# Feature Specification: Cœur de simulation Phase 1 — planète solo

**Feature Branch**: `001-planet-core-simulation`

**Created**: 2026-09-13

**Status**: Draft

**Input**: User description: "Phase 1 — Cœur de simulation (solo) : une seule planète générée
procéduralement. Portée fonctionnelle : exploration, extraction de ressources, construction,
développement de la civilisation, économie et industrie. Aucun réseau, aucun multijoueur. Cette
phase doit être complète et jouable de façon autonome avant de passer à la Phase 2 (multi
restreint)." Précisé ensuite : vue de gestion god mode façon SimCity (pas de personnage incarné) ;
le brouillard de guerre se dissipe automatiquement autour de chaque bâtiment construit ; les
ressources sont physiquement présentes sur la carte dès le début mais ne sont extractibles qu'une
fois la technologie correspondante débloquée par la recherche ET un extracteur construit sur le
gisement ; la recherche est menée par des colons assignés au métier de chercheur, comme n'importe
quel autre métier ; les colons assignés à un emploi rapportent de l'impôt, les colons au chômage
coûtent de l'argent ; chaque colon a un niveau de compétence distinct par métier, progressant sur
le tas (plafond bas) ou via une université (plafond plus haut, mais payante et lente), et son
rendement dépend de sa compétence et de sa santé ; le militaire (troupes = colons assignés, soldés)
fait partie de la vision du jeu mais est hors périmètre de la Phase 1 ; le transport des ressources
entre sites d'extraction/production et sites de stockage/consommation doit être géré par le joueur
(colons assignés au transport ou véhicules), les ressources ne se déplaçant jamais automatiquement ;
chaque colon a un nom et une ethnie générés automatiquement à sa création (renommable par le
joueur), les nouveau-nés héritant de l'ethnie de la colonie, sans généalogie ; chaque colon dispose
d'une biographie libre optionnelle (300 caractères max, saisie bloquée au-delà de la limite, vide
par défaut, sans effet sur le gameplay) ; le joueur démarre avec un Module de survie depuis lequel
il peut ouvrir une fenêtre listant les ressources disponibles et les colons par leur nom, et
cliquer sur un colon pour ouvrir sa fiche détaillée (santé, compétence par métier, éducation,
ethnie, biographie éditable, et un contrôle pour choisir individuellement son mode d'assignation
manuel ou automatique) ; la Phase 1 n'a pas de condition de victoire, mais un état d'échec
(effondrement de la colonie) est possible. Précisé une seconde fois : la construction d'un bâtiment
n'est pas instantanée — une fois les ressources dépensées, le bâtiment entre en chantier pour une
durée dépendant de son type (valeur de catalogue/équilibrage), et ce chantier ne progresse que si
au moins un colon y est assigné ; le catalogue des bâtiments (coûts, prérequis technologiques,
durée de chantier) est une structure de contenu extensible séparée de la spec, seul le mécanisme de
déblocage restant fixé ici ; chaque colon a un genre (homme/femme), la population de départ est
répartie environ 50/50, et une naissance a lieu lorsqu'un homme et une femme cohabitent un an de
temps de jeu continu dans le même logement, le genre du nouveau-né étant tiré aléatoirement tout en
rééquilibrant le ratio de la colonie à ± 10% — ce mécanisme de naissance est indépendant des
ressources/besoins de base et de la trésorerie de la colonie.

## User Scenarios & Testing *(mandatory)*

### Perspective de jeu

Le joueur ne contrôle pas de personnage et ne se déplace pas physiquement sur la carte : il joue
en vue de gestion à l'échelle de la colonie (« god mode », dans l'esprit de SimCity), observant et
pilotant sa colonie depuis une vue d'ensemble de la planète.

### User Story 1 - Fonder la colonie et révéler le territoire (Priority: P1)

En tant que joueur, je démarre une nouvelle partie sur une planète générée procéduralement, avec
un Module de survie et une zone de départ visible, le reste de la surface étant masqué par
un brouillard de guerre qui se dissipe automatiquement autour de chaque bâtiment que je construis.

**Why this priority**: C'est le point d'entrée de la partie : sans zone de départ visible, aucune
construction ni aucune autre mécanique n'a d'endroit où démarrer. C'est le MVP minimal jouable.

**Independent Test**: Lancer une nouvelle partie et vérifier qu'une zone de départ est visible
autour du Module de survie, que le reste de la planète est masqué, et que construire un
bâtiment en bordure de zone visible dissipe le brouillard de guerre dans un rayon autour de lui.

**Acceptance Scenarios**:

1. **Given** une nouvelle partie vient d'être créée, **When** le joueur observe la planète pour la
   première fois, **Then** une zone de départ est visible autour du Module de survie et
   le reste de la surface est masqué par le brouillard de guerre.
2. **Given** un bâtiment vient d'être construit en bordure de la zone visible, **When** sa
   construction se termine, **Then** le brouillard de guerre se dissipe automatiquement dans un
   rayon autour de ce bâtiment, révélant le terrain et les gisements de ressources visibles dans
   ce rayon.
3. **Given** une partie sauvegardée avec un territoire partiellement révélé, **When** le joueur
   recharge la partie, **Then** l'état du brouillard de guerre (zones révélées vs. masquées) est
   restauré à l'identique.

---

### User Story 2 - Construire des bâtiments (Priority: P1)

En tant que joueur, je dépense des ressources pour lancer la construction d'un bâtiment sur une
zone révélée ; le bâtiment entre alors en chantier et ne devient opérationnel qu'une fois la durée
de chantier de son type écoulée, à condition qu'au moins un colon y soit assigné pour y travailler.

**Why this priority**: La construction est le mécanisme central qui matérialise toutes les autres
mécaniques (révélation du territoire, extraction, transport, industrie) : sans elle, rien
d'autre n'est possible.

**Independent Test**: Avec un stock de ressources suffisant (y compris la dotation de démarrage),
placer un bâtiment sur une zone révélée et constructible, vérifier qu'il apparaît en chantier et ne
progresse pas tant qu'aucun colon n'y est assigné, puis qu'il devient opérationnel une fois un colon
assigné pendant la durée de chantier requise.

**Acceptance Scenarios**:

1. **Given** un stock de ressources suffisant et une zone révélée constructible, **When** le
   joueur choisit de construire un bâtiment, **Then** les ressources requises sont déduites du
   stock et le bâtiment apparaît en chantier à l'emplacement choisi (pas encore opérationnel).
2. **Given** un stock de ressources insuffisant, **When** le joueur tente de construire un
   bâtiment, **Then** la construction est refusée et le joueur voit les ressources manquantes.
3. **Given** un bâtiment en chantier sans aucun colon assigné, **When** le temps de jeu s'écoule,
   **Then** le chantier ne progresse pas et reste à l'arrêt.
4. **Given** un bâtiment en chantier avec un colon assigné pendant la durée de chantier requise
   pour son type, **When** cette durée est atteinte, **Then** le bâtiment devient opérationnel.
5. **Given** un bâtiment en chantier (avec sa progression) ou déjà opérationnel, **When** le
   joueur recharge la partie, **Then** son état est restauré à l'identique.

---

### User Story 3 - Débloquer et exploiter des ressources (Priority: P1)

En tant que joueur, j'assigne un colon au métier de chercheur pour débloquer la technologie
d'exploitation d'une ressource, puis je construis un extracteur sur un gisement visible afin de
commencer à produire cette ressource.

**Why this priority**: Sans extraction, aucune ressource nouvelle n'entre dans l'économie au-delà
de la dotation de démarrage ; c'est la mécanique qui alimente toute la suite (transport, industrie,
croissance).

**Independent Test**: Assigner un colon au métier de chercheur, débloquer la technologie d'une
ressource, construire l'extracteur correspondant sur un gisement révélé, et vérifier que
l'extraction ne démarre qu'une fois ces conditions réunies, puis que le gisement produit la
ressource jusqu'à épuisement.

**Acceptance Scenarios**:

1. **Given** un gisement de ressource visible dont la technologie d'extraction n'est pas encore
   débloquée, **When** le joueur tente d'y construire un extracteur, **Then** la construction est
   refusée et le joueur est informé que la technologie requise n'est pas débloquée.
2. **Given** un colon assigné au métier de chercheur, **When** le temps de jeu s'écoule, **Then**
   la recherche progresse vers le déblocage de technologies d'extraction, à une vitesse dépendant
   de la compétence et de la santé du chercheur (cf. User Story 6).
3. **Given** la technologie d'extraction d'une ressource est débloquée, **When** le joueur
   construit un extracteur sur le gisement correspondant, **Then** l'extracteur entre en chantier
   puis devient opérationnel (cf. User Story 2) et produit progressivement la ressource associée.
4. **Given** un gisement en cours d'extraction, **When** le gisement est épuisé, **Then**
   l'extraction s'arrête automatiquement et le joueur en est informé.

---

### User Story 4 - Transporter les ressources (Priority: P1)

En tant que joueur, j'organise le transport des ressources depuis leur lieu d'extraction ou de
production jusqu'à leur lieu de stockage ou de consommation, à l'aide de colons assignés au
transport ou de véhicules, sachant qu'aucune ressource ne se déplace automatiquement d'un bâtiment
à l'autre.

**Why this priority**: Sans logistique, les ressources produites par l'extraction (US3) restent
bloquées à leur point de production et ne peuvent alimenter ni le stockage, ni la construction, ni
l'industrie : le transport referme la boucle minimale extraction → utilisation.

**Independent Test**: Avec un extracteur produisant une ressource et un bâtiment de stockage
disponible mais non relié logistiquement, vérifier que la ressource ne rejoint pas le stockage tant
qu'aucun colon ou véhicule n'est assigné au transport entre les deux ; puis, une fois le transport
assigné, vérifier que la ressource est effectivement acheminée.

**Acceptance Scenarios**:

1. **Given** une ressource produite à un site d'extraction sans transport assigné, **When** le
   temps de jeu s'écoule, **Then** la ressource s'accumule sur place et n'atteint aucun site de
   stockage ou de consommation.
2. **Given** un colon ou un véhicule assigné au transport entre un site de production et un site de
   stockage, **When** le transport est actif, **Then** la ressource est progressivement acheminée
   du site de production vers le site de stockage.
3. **Given** une capacité de transport insuffisante par rapport au volume produit, **When** la
   production dépasse la capacité de transport disponible, **Then** la ressource s'accumule
   partiellement au point de production jusqu'à ce que la capacité de transport augmente ou que la
   production ralentisse.

---

### User Story 5 - Développer l'économie et l'industrie (Priority: P2)

En tant que joueur, je relie des bâtiments de production pour transformer des ressources brutes,
livrées par le transport, en ressources ou biens plus élaborés.

**Why this priority**: L'industrie donne un objectif à moyen terme à la boucle ressources →
construction → transport ; elle dépend des trois mécaniques précédentes déjà en place.

**Independent Test**: Avec un bâtiment de transformation construit, approvisionné en ressource
brute via le transport, vérifier qu'il produit automatiquement la ressource transformée
correspondante tant que l'approvisionnement suit.

**Acceptance Scenarios**:

1. **Given** un bâtiment de transformation construit et alimenté en ressource brute par le
   transport, **When** la production est active, **Then** le stock de ressource brute diminue et
   le stock de ressource transformée augmente.
2. **Given** un bâtiment de transformation dont le stock de sortie est plein, **When** la
   production continue, **Then** la production se met en pause jusqu'à libération de capacité de
   stockage ou reprise du transport de sortie.
3. **Given** plusieurs chaînes de production indépendantes, **When** l'une d'elles manque de
   ressource en entrée (production ou transport insuffisants), **Then** seule cette chaîne est
   interrompue, sans affecter les autres.

---

### User Story 6 - Gérer l'emploi, la fiscalité et le rendement des colons (Priority: P2)

En tant que joueur, j'assigne mes colons à des emplois (y compris le métier de chercheur) dans les
bâtiments qui en nécessitent, ce qui génère un revenu d'impôt et fait progresser leur compétence
« sur le tas » ; les colons non assignés (au chômage) coûtent de l'argent plutôt que d'en rapporter,
et le rendement de chaque colon employé dépend de sa compétence et de sa santé. Pour chaque colon,
je peux choisir individuellement (depuis sa fiche détaillée, cf. User Story 10) s'il est assigné
manuellement par moi ou automatiquement par le système.

**Why this priority**: Cette mécanique introduit une trésorerie et une notion de qualité de
main-d'œuvre qui régulent le niveau de développement de la civilisation (US9) et équilibrent la
colonie contre une main-d'œuvre non productive ou mal formée ; elle suppose que des bâtiments et
des colons existent déjà.

**Independent Test**: Assigner un colon sans expérience à un poste disponible et vérifier que sa
compétence dans ce métier progresse lentement jusqu'à un plafond bas, que la trésorerie du joueur
augmente (impôt) proportionnellement à sa compétence et à sa santé ; laisser un colon sans emploi
et vérifier que la trésorerie diminue au fil du temps (coût de chômage).

**Acceptance Scenarios**:

1. **Given** un poste disponible dans un bâtiment et un colon non assigné, **When** le joueur
   assigne ce colon au poste (ou que le colon est en mode automatique et qu'un poste se libère),
   **Then** la trésorerie du joueur augmente périodiquement tant que le colon occupe ce poste,
   proportionnellement à sa compétence et à sa santé dans ce métier.
2. **Given** un colon assigné à un métier dans lequel il n'a jamais été formé, **When** le temps de
   jeu s'écoule, **Then** sa compétence dans ce métier progresse progressivement mais plafonne à un
   niveau bas (apprentissage « sur le tas »).
3. **Given** un bâtiment dont tous les postes sont occupés par des colons peu compétents ou en
   mauvaise santé, **When** le joueur observe la production de ce bâtiment, **Then** le rendement
   produit est inférieur à celui d'un bâtiment équivalent occupé par des colons compétents et en
   bonne santé, malgré une capacité de main-d'œuvre à 100%.
4. **Given** un colon sans emploi, **When** le temps de jeu s'écoule, **Then** la trésorerie du
   joueur diminue périodiquement tant que ce colon reste sans emploi.
5. **Given** une trésorerie qui atteint zéro ou devient négative, **When** cette situation
   persiste, **Then** le système en informe clairement le joueur.

---

### User Story 7 - Faire naître de nouveaux colons par cohabitation en logement (Priority: P2)

En tant que joueur, je loge mes colons dans des bâtiments d'habitation ; lorsqu'un homme et une
femme cohabitent dans le même logement pendant une année de jeu continue, un nouveau colon naît,
indépendamment de l'état de mes ressources ou de ma trésorerie.

**Why this priority**: Cette mécanique alimente la croissance de la population indépendamment de
l'économie ; elle ne dépend que du logement (US2) et de l'existence de colons ayant un genre, mais
reste secondaire à la boucle minimale P1.

**Independent Test**: Loger un homme et une femme dans le même bâtiment d'habitation, avancer le
temps de jeu d'un an sans modifier les ressources ni la trésorerie, et vérifier qu'une naissance a
lieu ; vérifier qu'aucune naissance ne se produit avant un an ou sans cohabitation homme/femme
continue.

**Acceptance Scenarios**:

1. **Given** un homme et une femme cohabitant dans le même logement depuis un an de temps de jeu
   continu, **When** ce délai est atteint, **Then** un nouveau colon naît, avec un genre déterminé
   aléatoirement.
2. **Given** une colonie où un genre est sous-représenté de plus de 10% par rapport à l'autre,
   **When** une naissance a lieu, **Then** la probabilité que le nouveau-né soit du genre
   sous-représenté est augmentée, pour rééquilibrer le ratio global vers 50/50.
3. **Given** une colonie dont les ressources ou la trésorerie sont insuffisantes ou négatives,
   **When** un couple homme/femme cohabite un an dans un logement, **Then** la naissance a lieu
   normalement, sans être bloquée ni retardée par l'état économique de la colonie.
4. **Given** un homme et une femme qui cessent de cohabiter avant la fin de l'année requise,
   **When** leur cohabitation reprend ensuite, **Then** le décompte de la durée continue redémarre
   depuis le début (les périodes de cohabitation discontinues ne se cumulent pas).

---

### User Story 8 - Former les colons via l'université (Priority: P3)

En tant que joueur, une fois l'université débloquée, j'y envoie un colon se former à un métier
ciblé afin qu'il progresse plus vite et jusqu'à un niveau de compétence plus élevé que
l'apprentissage sur le tas, moyennant un coût en argent (variable selon le métier visé) et du
temps ; je peux retirer un colon de la formation à tout moment.

**Why this priority**: La formation accélère et rehausse le plafond de compétence atteignable, ce
qui améliore le rendement de la colonie (US6) ; elle suppose l'existence préalable de l'économie,
de l'emploi et d'un palier de développement de la civilisation débloquant l'université (US9).

**Independent Test**: Avec une université construite, envoyer un colon en formation pour un métier
donné et vérifier que sa compétence dans ce métier progresse plus vite et au-delà du plafond de
l'apprentissage sur le tas, que la trésorerie du joueur diminue en conséquence, et que le joueur
peut retirer le colon de la formation à tout moment.

**Acceptance Scenarios**:

1. **Given** une université construite et un colon disponible, **When** le joueur assigne ce colon
   à une formation pour un métier ciblé, **Then** un coût en argent est déduit de la trésorerie
   selon le métier visé, et la formation démarre.
2. **Given** un colon en cours de formation, **When** le temps de jeu s'écoule, **Then** sa
   compétence dans le métier visé progresse plus rapidement, et peut dépasser le plafond atteignable
   par l'apprentissage sur le tas.
3. **Given** un colon en cours de formation, **When** le joueur décide de le retirer de la
   formation, **Then** la formation s'arrête immédiatement et le colon conserve la compétence déjà
   acquise.

---

### User Story 9 - Développer la civilisation (Priority: P3)

En tant que joueur, je développe le niveau de ma colonie (au-delà de sa simple population) grâce à
l'économie déjà en place, afin de débloquer de nouveaux bâtiments et possibilités (dont
l'université). La Phase 1 n'a pas de condition de victoire, mais une négligence prolongée de la
colonie peut mener à un état d'échec (effondrement).

**Why this priority**: C'est la couche de progression à long terme qui donne un sens cumulatif à
toutes les autres mécaniques ; elle a le plus de valeur une fois que la fondation, la construction,
l'extraction, le transport et l'économie existent déjà.

**Independent Test**: Maintenir une colonie approvisionnée et financièrement équilibrée sur
plusieurs cycles de jeu et vérifier que le niveau de développement progresse, débloquant au moins
une nouvelle capacité ou un nouveau bâtiment ; à l'inverse, priver la colonie de ressources vitales
et de trésorerie sur une durée prolongée et vérifier qu'elle entre en état d'échec.

**Acceptance Scenarios**:

1. **Given** une colonie dont les besoins de base et la trésorerie sont positifs, **When** le
   temps de jeu s'écoule, **Then** le niveau de développement de la colonie augmente. (La
   population de la colonie évolue indépendamment de cet indicateur, via le mécanisme de naissance
   de la User Story 7.)
2. **Given** une colonie dont un besoin de base n'est plus couvert ou dont la trésorerie est
   négative de façon prolongée, **When** le temps de jeu s'écoule, **Then** le niveau de
   développement stagne ou régresse, et le joueur en est informé (la population continue
   d'évoluer indépendamment via les naissances, cf. User Story 7).
3. **Given** un palier de développement de la civilisation atteint, **When** ce palier est franchi,
   **Then** un nouveau bâtiment ou une nouvelle capacité devient disponible à la construction/à
   l'utilisation.
4. **Given** une trésorerie négative ou des ressources vitales à zéro qui persistent durablement,
   **When** ces conditions critiques se prolongent au-delà d'un seuil, **Then** la colonie entre
   dans un état d'échec (effondrement) clairement signalé au joueur ; aucune condition de victoire
   n'existe en Phase 1.

---

### User Story 10 - Consulter et gérer chaque colon individuellement (Priority: P3)

En tant que joueur, j'ouvre depuis le Module de survie une fenêtre listant les ressources
disponibles et mes colons par leur nom ; en cliquant sur un colon, j'accède à sa fiche détaillée
(genre, santé, compétence par métier, éducation/formation en cours, ethnie, biographie éditable) et
je peux y choisir son mode d'assignation individuel (manuel ou automatique), le renommer, et éditer
sa biographie pour mon attachement narratif, sans effet sur le gameplay.

**Why this priority**: Cette interface centralise la gestion et la personnalisation des colons
utilisée par les autres mécaniques (emploi, transport, formation, logement) ; elle apporte de la
valeur dès que des colons existent (dotation de départ), mais n'est jamais bloquante pour les
systèmes économiques ou démographiques eux-mêmes.

**Independent Test**: Depuis le Module de survie, ouvrir la fenêtre de gestion, vérifier
qu'elle liste les ressources et les colons par nom ; sélectionner un colon et vérifier que sa fiche
affiche genre, santé, compétence, éducation, ethnie et biographie ; changer son mode d'assignation,
le renommer, et éditer sa biographie (jusqu'à 300 caractères, saisie bloquée au-delà), puis vérifier
que rien de tout cela n'affecte sa compétence, sa santé ou son rendement (hormis le mode
d'assignation, qui change qui décide de son affectation).

**Acceptance Scenarios**:

1. **Given** le Module de survie est disponible, **When** le joueur ouvre la fenêtre de
   gestion, **Then** la liste des ressources disponibles et la liste des colons par leur nom
   s'affichent.
2. **Given** un colon créé (dotation de départ ou naissance dans la colonie, cf. User Story 7),
   **When** le joueur le sélectionne dans la liste, **Then** sa fiche détaillée affiche son genre,
   sa santé, sa compétence par métier, son éducation/formation en cours, son ethnie (héritée de
   celle de la colonie s'il y est né) et sa biographie, ainsi qu'un contrôle pour choisir son mode
   d'assignation manuel ou automatique.
3. **Given** la fiche d'un colon ouverte, **When** le joueur bascule son mode d'assignation entre
   manuel et automatique, **Then** le nouveau mode est appliqué et respecté par le système
   d'assignation (cf. User Story 6).
4. **Given** la fiche d'un colon ouverte, **When** le joueur le renomme, **Then** le nouveau nom
   est conservé partout où le colon est référencé, y compris après rechargement de la partie.
5. **Given** la biographie vide par défaut d'un colon, **When** le joueur saisit du texte,
   **Then** la saisie est bloquée dès que la limite de 300 caractères est atteinte (sans
   troncature ni message d'erreur après coup), et le texte n'a aucun effet sur la compétence, la
   santé ou le rendement du colon.

---

### Edge Cases

- Que se passe-t-il si le joueur tente de construire un extracteur sur un gisement dont la
  technologie n'est pas débloquée ?
- Que se passe-t-il si aucun colon n'est jamais assigné au métier de chercheur (progression de la
  recherche totalement bloquée) ?
- Que se passe-t-il si toute la capacité de transport disponible est déjà occupée et qu'un nouveau
  site de production entre en service ?
- Comment le système réagit-il si le joueur quitte le jeu pendant qu'une extraction, un transport,
  une construction, un chantier, une production ou une formation est en cours (reprise à la
  relance) ?
- Que se passe-t-il si un bâtiment nécessaire à une chaîne de production ou à un poste d'emploi est
  détruit ou devient indisponible alors qu'il est en activité (colon employé, en formation, ou
  transport en cours) ?
- Que se passe-t-il si le stockage global du joueur atteint sa capacité maximale pour une ressource
  donnée pendant que l'extraction, le transport ou la production continuent ?
- Que se passe-t-il si la trésorerie du joueur devient négative de façon prolongée (impossibilité
  de payer/construire/former, effet en cascade sur le chômage, jusqu'à l'effondrement) ?
- Que se passe-t-il si tous les colons disponibles sont déjà assignés (emploi + transport +
  formation + chantier) et qu'un nouveau poste ou besoin apparaît ?
- Que se passe-t-il si un colon très compétent dans un métier est réassigné à un autre métier dans
  lequel il n'a aucune compétence ?
- Que se passe-t-il si un colon en mode d'assignation automatique est manuellement réassigné par le
  joueur, ou inversement ?
- Que se passe-t-il si le joueur tente de renommer un colon avec un nom vide ?
- Que se passe-t-il si le chantier d'un bâtiment n'a jamais aucun colon assigné (le bâtiment
  reste indéfiniment en chantier) ?
- Que se passe-t-il si le colon assigné à un chantier en est retiré ou réassigné avant la fin de
  la durée de chantier ?
- Que se passe-t-il si un logement héberge plus de deux colons de genres différents en même temps
  (plusieurs couples potentiels dans le même bâtiment) ?
- Que se passe-t-il si la colonie ne dispose plus d'aucun logement disponible pour accueillir un
  nouveau-né ?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT générer procéduralement une planète unique en début de partie,
  composée de zones dotées d'un terrain et, le cas échéant, de gisements de ressources visibles
  dès le début (mais pas nécessairement révélés par le brouillard de guerre).
- **FR-002**: Le joueur DOIT interagir avec la partie exclusivement via une vue de gestion
  d'ensemble de la colonie (god mode), sans incarner ni déplacer de personnage sur la carte.
- **FR-003**: Le système DOIT afficher une zone de départ visible en début de partie et masquer le
  reste de la planète par un brouillard de guerre.
- **FR-004**: Le système DOIT dissiper automatiquement le brouillard de guerre dans un rayon autour
  de tout bâtiment nouvellement construit, révélant le terrain et les gisements de ressources
  visibles dans ce rayon.
- **FR-005**: Le joueur DOIT pouvoir lancer la construction d'un bâtiment sur une zone révélée et
  constructible en dépensant les ressources requises par ce type de bâtiment.
- **FR-006**: Le système DOIT refuser une construction dont le coût en ressources dépasse le stock
  disponible et DOIT indiquer au joueur les ressources manquantes.
- **FR-007**: Le système DOIT fournir au joueur, dès le début de la partie, un Module de survie
  ainsi qu'une dotation de ressources et de colons permettant de démarrer la construction, l'emploi
  et la recherche sans dépendre d'une extraction déjà active.
- **FR-008**: Le joueur DOIT pouvoir assigner un colon au métier de chercheur afin de faire
  progresser le déblocage, ressource par ressource, de la technologie nécessaire à son extraction.
- **FR-009**: Le système NE DOIT PAS permettre la construction d'un extracteur pour une ressource
  dont la technologie d'extraction n'est pas débloquée.
- **FR-010**: Le joueur DOIT pouvoir construire un extracteur sur un gisement de ressource visible
  dont la technologie est débloquée, ce qui démarre le chantier de cet extracteur (cf. FR-042).
- **FR-011**: Le système DOIT arrêter automatiquement l'extraction d'un gisement épuisé et DOIT en
  informer le joueur.
- **FR-012**: Le système NE DOIT PAS déplacer automatiquement une ressource entre deux bâtiments :
  tout déplacement de ressource entre un site de production et un site de stockage ou de
  consommation DOIT passer par un transport assigné (colon ou véhicule).
- **FR-013**: Le joueur DOIT pouvoir assigner un colon ou un véhicule au transport d'une ressource
  entre un site source et un site destination.
- **FR-014**: Le système DOIT accumuler une ressource produite sur son site de production tant
  qu'aucune capacité de transport suffisante n'est assignée pour l'acheminer.
- **FR-015**: Le joueur DOIT pouvoir construire des bâtiments de transformation qui consomment une
  ou plusieurs ressources livrées en entrée et produisent une ou plusieurs ressources en sortie.
- **FR-016**: Le système DOIT interrompre automatiquement la production d'un bâtiment de
  transformation dès que la ressource en entrée manque (production ou transport insuffisants), ou
  que le stock de sortie associé atteint sa capacité maximale, et DOIT reprendre automatiquement la
  production dès que la condition bloquante est levée.
- **FR-017**: Le joueur DOIT pouvoir assigner un colon disponible à un poste d'emploi ouvert dans un
  bâtiment qui en nécessite, y compris le métier de chercheur.
- **FR-018**: Le système DOIT générer un revenu périodique (impôt) pour le joueur tant qu'un colon
  occupe un poste d'emploi assigné, modulé par le rendement de ce colon à ce poste.
- **FR-019**: Le système DOIT déduire un coût périodique de la trésorerie du joueur pour chaque
  colon non assigné à un emploi (chômage).
- **FR-020**: Le système DOIT attribuer à chaque colon un niveau de compétence distinct pour chaque
  métier qu'il est susceptible d'occuper (dont le métier de chercheur).
- **FR-021**: Le système DOIT attribuer à chaque colon de la population initiale un niveau de
  compétence de départ aléatoire et variable selon les métiers.
- **FR-022**: Le système DOIT faire progresser automatiquement la compétence d'un colon dans le
  métier qu'il occupe (apprentissage « sur le tas ») tant qu'il y est assigné, jusqu'à un plafond
  bas propre à ce mode d'apprentissage.
- **FR-023**: Le joueur DOIT pouvoir, une fois l'université débloquée, y assigner un colon à une
  formation ciblant un métier donné, moyennant un coût en argent variable selon le métier visé et
  une durée de formation.
- **FR-024**: La formation à l'université DOIT faire progresser la compétence d'un colon dans le
  métier visé plus rapidement et jusqu'à un plafond plus élevé que l'apprentissage sur le tas.
- **FR-025**: Le joueur DOIT pouvoir retirer un colon d'une formation en cours à tout moment ; la
  compétence déjà acquise DOIT être conservée.
- **FR-026**: Le rendement (production, impôt généré, vitesse de recherche, etc.) d'un colon à son
  poste DOIT dépendre de son niveau de compétence dans ce métier et de sa santé, et non uniquement
  du fait que le poste soit occupé.
- **FR-027**: Le système DOIT permettre qu'un bâtiment ait 100% de ses postes occupés tout en
  produisant un rendement global inférieur à son maximum théorique, si les colons assignés ont une
  compétence ou une santé insuffisantes.
- **FR-028**: Le système DOIT faire évoluer le niveau de développement de la civilisation en
  fonction des ressources produites/consommées et de la trésorerie de la colonie. La population de
  la colonie évolue quant à elle exclusivement via le mécanisme de naissance (FR-047 à FR-049),
  indépendamment des ressources et de la trésorerie.
- **FR-029**: Le système DOIT débloquer au moins un nouveau bâtiment ou une nouvelle capacité
  (dont l'université) lorsque la civilisation franchit un palier de développement.
- **FR-030**: Le système DOIT faire stagner ou régresser le niveau de développement de la
  civilisation lorsque les besoins de base ou la trésorerie de la colonie ne sont plus couverts, et
  DOIT en informer le joueur.
- **FR-031**: Le système DOIT persister l'état complet de la partie (brouillard de guerre, stocks,
  bâtiments et chantiers en cours, technologies débloquées, transport, production, emplois,
  compétences et santé des colons, formations en cours, identité et genre des colons, cohabitations
  de logement en cours, trésorerie, niveau de développement de la civilisation) et le restaurer à
  l'identique lors du chargement d'une partie sauvegardée.
- **FR-032**: Le système DOIT fonctionner intégralement hors ligne, sans nécessiter de connexion
  réseau ni de serveur, conformément au périmètre de la Phase 1.
- **FR-033**: Le système DOIT permettre au joueur de reprendre sans perte de cohérence toute
  extraction, tout transport, toute construction/chantier, toute production ou toute formation qui
  était active au moment de la fermeture du jeu.
- **FR-034**: Le système NE DOIT proposer aucune mécanique militaire ou de combat au cours de la
  Phase 1 (cf. section Out of Scope).
- **FR-035**: Le joueur DOIT pouvoir choisir, individuellement pour chaque colon et à tout moment,
  un mode d'assignation manuel (le joueur assigne lui-même ce colon à un poste ou une tâche de
  transport) ou automatique (le système l'assigne aux postes ou tâches de transport disponibles),
  via un contrôle dédié dans la fiche détaillée du colon.
- **FR-036**: La Phase 1 NE DOIT proposer aucune condition de victoire. Le système DOIT en revanche
  détecter un état d'échec (effondrement de la colonie) lorsque des conditions critiques (par
  exemple une trésorerie ou des ressources vitales à zéro) persistent durablement, et DOIT en
  informer clairement le joueur.
- **FR-037**: Le joueur DOIT pouvoir ouvrir, depuis le Module de survie, une fenêtre affichant
  les ressources disponibles et la liste des colons de la colonie par leur nom.
- **FR-038**: Le joueur DOIT pouvoir sélectionner un colon dans cette liste pour ouvrir sa fiche
  détaillée, affichant son genre, sa santé, son niveau de compétence par métier, son
  éducation/formation en cours, son ethnie et sa biographie éditable.
- **FR-039**: Le système DOIT générer automatiquement un nom et une ethnie pour chaque colon à sa
  création ; le joueur DOIT pouvoir renommer un colon à tout moment.
- **FR-040**: Un colon né au sein de la colonie DOIT hériter automatiquement de l'ethnie de la
  colonie ; le système NE DOIT PAS suivre de généalogie (filiation entre colons).
- **FR-041**: Le joueur DOIT pouvoir éditer, pour chaque colon, un champ biographie libre et
  optionnel, vide par défaut ; la saisie DOIT être bloquée dès que 300 caractères sont atteints
  (sans troncature ni message d'erreur après coup) ; ce champ NE DOIT avoir aucun effet sur les
  mécaniques de jeu.
- **FR-042**: Le système DOIT faire entrer un bâtiment nouvellement lancé en construction dans un
  état « chantier » après déduction du coût en ressources, avant qu'il ne devienne opérationnel ;
  la durée du chantier DOIT dépendre du type de bâtiment (valeur définie dans le catalogue de
  contenu, hors périmètre de cette spécification).
- **FR-043**: Le chantier d'un bâtiment NE DOIT progresser que si au moins un colon lui est
  assigné (au même titre qu'un poste d'emploi) ; en l'absence de colon assigné, le chantier DOIT
  rester à l'arrêt, sans régresser ni être annulé.
- **FR-044**: Le catalogue des bâtiments disponibles (types, coûts, prérequis technologiques
  éventuels, durée de chantier) DOIT être défini dans une structure de données de contenu
  extensible et séparée de cette spécification et du plan technique, afin de pouvoir être enrichi
  sans modification de la spec ou du plan ; seul le mécanisme de déblocage d'un bâtiment (coût en
  ressources et, le cas échéant, prérequis technologique et gisement pour un extracteur) reste
  fixé par cette spécification.
- **FR-045**: Le système DOIT attribuer à chaque colon un genre (homme ou femme), en complément de
  ses autres caractéristiques (nom, ethnie, biographie, santé, compétence).
- **FR-046**: Le système DOIT générer la population initiale de colons avec une répartition
  d'environ 50% hommes / 50% femmes.
- **FR-047**: Le système DOIT déclencher une naissance lorsqu'un homme et une femme cohabitent dans
  le même bâtiment d'habitation pendant une durée continue d'un an (temps de jeu) ; une
  interruption de la cohabitation avant ce délai DOIT réinitialiser le décompte.
- **FR-048**: Le système DOIT déterminer aléatoirement le genre d'un nouveau-né, en favorisant la
  probabilité du genre sous-représenté dans la colonie dès que l'écart entre les deux genres
  dépasse 10% (quota global homme/femme à ± 10%).
- **FR-049**: Le mécanisme de naissance (FR-047/FR-048) NE DOIT PAS dépendre des besoins de base de
  la colonie (nourriture, eau) ni de sa trésorerie ; il DOIT être conditionné uniquement par la
  cohabitation homme/femme en logement sur la durée requise, sans intervention automatique du
  système au-delà de cette règle.
- **FR-050**: Le Module de survie DOIT être traité comme un bâtiment à part entière (au même titre
  que les autres bâtiments du catalogue) ; le joueur DOIT pouvoir cliquer dessus pour ouvrir un
  inventaire consultable listant la dotation initiale de ressources (eau, nourriture) ainsi que les
  Extracteurs multifonction disponibles.
- **FR-051**: Le système DOIT fournir, dans l'inventaire du Module de survie, 5 Extracteurs
  multifonction : des équipements d'extraction portatifs, distincts des extracteurs/pompes fixes
  (cf. FR-010), utilisables uniquement sur un gisement d'eau, de pierre ou de bois.
- **FR-052**: Le joueur DOIT pouvoir placer un Extracteur multifonction disponible sur un gisement
  compatible (eau, pierre ou bois) révélé, puis le déplacer à tout moment vers un autre gisement
  compatible, sans phase de chantier ni destruction/reconstruction de l'équipement ; une fois placé,
  il extrait la ressource de son gisement au même titre qu'un extracteur fixe opérationnel.
- **FR-053**: Le catalogue de bâtiments (FR-044) DOIT pouvoir associer à chaque type de bâtiment,
  en complément de son coût en ressources (FR-005/FR-006), un coût en Crédits Galactiques
  représentant le salaire des ouvriers du chantier ; les deux coûts DOIVENT être déduits ensemble au
  lancement de la construction, et celle-ci DOIT être refusée si l'un ou l'autre des deux coûts
  dépasse ce qui est disponible. Les Crédits Galactiques désignent la même trésorerie que celle
  définie en User Story 6 (FR-018/FR-019) : il ne s'agit pas d'une devise distincte.

### Key Entities

- **Planète** : la carte de jeu générée procéduralement pour la partie en cours ; regroupe
  l'ensemble des zones, leur terrain, leurs gisements et l'état du brouillard de guerre.
- **Zone** : portion de la surface de la planète, caractérisée par un terrain et, le cas échéant,
  un ou plusieurs gisements de ressources ; peut être révélée ou masquée par le brouillard de
  guerre, et constructible ou non.
- **Brouillard de guerre** : état de visibilité de chaque zone de la planète, qui se dissipe
  automatiquement autour des bâtiments construits.
- **Gisement de ressource** : source finie d'une ressource brute présente physiquement dans une
  zone dès le début de la partie, mais non extractible tant que la technologie correspondante n'est
  pas débloquée et qu'aucun extracteur n'y est construit.
- **Technologie** : élément débloqué par la progression de la recherche (portée par les colons
  chercheurs), conditionnant la capacité à construire un type d'extracteur donné, et pouvant
  également être un prérequis pour d'autres types de bâtiments (cf. catalogue de bâtiments).
- **Ressource** : élément brut ou transformé stocké dans l'inventaire du joueur, consommé ou
  produit par l'extraction, le transport, la construction ou la production.
- **Module de survie** : bâtiment de départ fourni au joueur (traité comme un bâtiment à part
  entière), point d'accès à la fenêtre de gestion des ressources et des colons, et à un inventaire
  consultable en cliquant dessus contenant la dotation initiale de ressources (eau, nourriture) et
  les Extracteurs multifonction disponibles.
- **Extracteur multifonction** : équipement d'extraction portatif et déplaçable (distinct des
  extracteurs/pompes fixes), fourni en 5 exemplaires dans l'inventaire du Module de survie,
  utilisable uniquement sur un gisement d'eau, de pierre ou de bois ; le joueur choisit sur quel
  gisement compatible le placer, et peut le déplacer vers un autre gisement compatible à tout
  moment.
- **Bâtiment** : structure lancée en construction par le joueur sur une zone révélée ; passe par un
  état de chantier (dont la progression nécessite un colon assigné et dépend d'une durée propre à
  son type, cf. catalogue de bâtiments) avant de devenir opérationnel ; peut être un bâtiment de
  stockage, un extracteur, un bâtiment de production/transformation, un logement (site du mécanisme
  de naissance), un poste d'emploi (dont la recherche ou l'université), ou lié au développement de
  la civilisation.
- **Catalogue de bâtiments** : structure de données de contenu extensible (hors spec/plan) listant
  les types de bâtiments disponibles, leur coût combiné en ressources et en Crédits Galactiques
  (FR-053), leurs prérequis technologiques éventuels et leur durée de chantier ; s'enrichit sans
  modifier cette spécification.
- **Crédits Galactiques** : désignation narrative de la trésorerie du joueur (cf. entité Trésorerie,
  User Story 6), également mobilisée pour couvrir le coût en main-d'œuvre d'une construction
  (FR-053) ; ce n'est pas une devise distincte de la Trésorerie.
- **Colon** : unité de population possédant un nom, une ethnie et un genre (homme ou femme) générés
  automatiquement (nom renommable, sans généalogie), une biographie libre optionnelle sans effet
  sur le gameplay, un niveau de compétence par métier, un niveau de santé, et un mode d'assignation
  individuel (manuel ou automatique) ; assignable à un poste d'emploi (dont chercheur), à une tâche
  de transport, à un chantier, ou à une formation ; sans assignation, un colon est considéré au
  chômage.
- **Compétence** : niveau de maîtrise d'un colon pour un métier donné, qui influence son rendement
  à ce poste ; progresse sur le tas (plafond bas) ou par formation universitaire (plafond élevé).
- **Santé** : état d'un colon influençant, avec sa compétence, son rendement à son poste.
- **Formation** : processus par lequel un colon assigné à l'université progresse en compétence pour
  un métier ciblé, moyennant un coût en argent et une durée, interruptible à tout moment.
- **Transport** : liaison logistique (colon ou véhicule assigné) entre un site de production et un
  site de stockage ou de consommation, permettant le déplacement effectif d'une ressource.
- **Cohabitation de logement** : présence continue d'au moins un homme et une femme dans le même
  bâtiment d'habitation ; déclenche une naissance après un an de temps de jeu continu, et se
  réinitialise en cas d'interruption.
- **Trésorerie** : montant d'argent du joueur, alimenté par l'impôt des colons employés (modulé par
  leur rendement) et réduit par le coût des colons au chômage et par les formations en cours.
- **Chaîne de production** : relation entre un ou plusieurs bâtiments de transformation, des
  ressources en entrée livrées par transport et des ressources en sortie.
- **Colonie** : représentation globale de la présence du joueur sur la planète, incluant la
  population (dont le ratio hommes/femmes), le niveau de développement de la civilisation, la
  trésorerie, et l'ethnie par défaut héritée par les colons qui y naissent.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un joueur peut, en une seule session, faire passer sa zone visible du seul point de
  départ jusqu'à une portion significative de la planète, uniquement par la construction de
  bâtiments successifs (sans mécanique d'exploration manuelle).
- **SC-002**: Un joueur peut assigner un colon au métier de chercheur, débloquer au moins une
  technologie d'extraction, construire l'extracteur correspondant, et voir la ressource associée
  s'accumuler à son point de production.
- **SC-003**: Un joueur peut organiser le transport d'au moins une ressource depuis son site de
  production jusqu'à un site de stockage, et constater que le stock global n'augmente que lorsque
  ce transport est actif.
- **SC-004**: Un joueur peut construire au moins cinq types de bâtiments différents en utilisant
  uniquement les ressources extraites et transportées sur la planète.
- **SC-005**: Un joueur peut mettre en place au moins une chaîne de production complète (ressource
  brute transportée → bâtiment de transformation → ressource transformée) fonctionnant de façon
  autonome tant que l'approvisionnement suit.
- **SC-006**: Un joueur peut assigner des colons à des emplois et observer sa trésorerie augmenter
  grâce à l'impôt, puis observer sa trésorerie diminuer lorsque des colons restent au chômage.
- **SC-007**: Un joueur peut observer qu'un colon non formé progresse lentement jusqu'à un plafond
  de compétence bas, puis observer qu'un colon envoyé à l'université progresse plus vite et
  au-delà de ce plafond, moyennant un coût en argent.
- **SC-008**: Un joueur peut constater qu'un bâtiment entièrement pourvu en personnel mais occupé
  par des colons peu compétents ou en mauvaise santé produit moins qu'un bâtiment équivalent occupé
  par des colons compétents et en bonne santé.
- **SC-009**: Un joueur peut observer sa colonie franchir au moins un palier de développement de la
  civilisation au cours d'une partie, débloquant un nouveau bâtiment (dont l'université) ou une
  nouvelle capacité.
- **SC-010**: Une partie peut être sauvegardée puis rechargée sans perte ni incohérence du
  brouillard de guerre, des stocks, des bâtiments et chantiers, des technologies débloquées, du
  transport, de la production, des emplois, des compétences/santé/identité/genre des colons, des
  formations et cohabitations en cours, ou de la trésorerie.
- **SC-011**: L'intégralité de la boucle de jeu (fondation/brouillard de guerre, construction,
  recherche/extraction, transport, économie/industrie, emploi/fiscalité/compétences, naissances,
  développement de la civilisation) est jouable de bout en bout sans qu'aucune fonction ne requière
  de connexion réseau, et sans aucune mécanique militaire ou de combat.
- **SC-012**: Un joueur peut choisir individuellement, pour chaque colon depuis sa fiche détaillée,
  un mode d'assignation manuel ou automatique, et constater que ce choix est respecté par le
  système d'assignation.
- **SC-013**: Un joueur peut consulter, depuis le Module de survie, la liste de ses colons par
  nom et la fiche détaillée de chacun (genre, santé, compétence, éducation, ethnie, biographie).
- **SC-014**: Un joueur peut renommer un colon et éditer sa biographie (saisie bloquée à 300
  caractères) sans que cela n'affecte sa compétence, sa santé ou son rendement.
- **SC-015**: Une colonie dont les conditions critiques (trésorerie ou ressources vitales) restent à
  zéro de façon prolongée entre dans un état d'échec clairement signalé au joueur ; aucune
  condition de victoire n'existe en Phase 1.
- **SC-016**: Un joueur peut observer qu'un bâtiment en chantier ne progresse pas tant qu'aucun
  colon n'y est assigné, puis progresse et devient opérationnel une fois un colon assigné pendant
  la durée de chantier requise pour son type.
- **SC-017**: Un joueur peut observer une naissance après qu'un couple homme/femme ait cohabité une
  année de jeu continue dans un même logement, y compris lorsque les ressources ou la trésorerie de
  la colonie sont insuffisantes.
- **SC-018**: Le ratio hommes/femmes de la colonie reste proche de 50/50 (± 10%) au fil des
  naissances successives.

## Out of Scope *(Phase 1)*

- **Militaire et combat** : dans la vision globale du jeu, les troupes militaires seront des colons
  assignés recevant une solde qui limite leur nombre viable, sur le même principe que l'emploi
  civil (US6). Cette mécanique fait partie de la vision long terme du jeu mais N'EST PAS un
  objectif fonctionnel de la Phase 1 ; aucune mécanique militaire ou de combat ne doit être
  implémentée à ce stade, conformément au principe de développement par phases de la constitution
  du projet.
- **Multijoueur et réseau** : reporté à la Phase 2 (multi restreint) et aux phases suivantes.

## Assumptions

- Le joueur incarne une seule colonie sans adversaire IA ni autre joueur sur la planète durant la
  Phase 1, conformément au périmètre solo défini par la constitution du projet. Le nom donné par le
  joueur à la fondation de sa colonie sert aussi d'ethnie par défaut héritée par les colons qui y
  naissent ; un système de fondation de plusieurs colonies distinctes sur la même planète n'est pas
  dans le périmètre de cette spécification sauf demande contraire ultérieure.
- La planète est représentée par un ensemble fini de zones (grille ou pavage), pas par un monde
  ouvert continu sans limites ; ce choix est un détail de conception qui pourra être affiné en
  phase de planification technique.
- Le rayon exact de dissipation du brouillard de guerre autour d'un bâtiment sera défini en phase
  de planification technique (peut varier selon le type de bâtiment).
- Un système de sauvegarde/chargement standard (local, sans compte en ligne) est disponible et
  suffisant pour la Phase 1.
- Le catalogue de bâtiments (types, coûts, prérequis technologiques, durée de chantier — FR-044) et
  les autres catalogues de contenu (ressources, technologies, paliers de développement de la
  civilisation) nécessaires pour valider SC-002 à SC-009 seront définis précisément lors de la
  planification (`/speckit-plan`), via une structure de données extensible (ex. ScriptableObject) ;
  cette spécification fixe seulement les mécaniques attendues, pas le catalogue de contenu final.
- Les valeurs numériques précises du système de compétence/santé (rendement en %, vitesse de
  progression, plafonds exacts, coûts et durées de formation), la durée de chantier par type de
  bâtiment (FR-042), ainsi que les seuils exacts menant à l'état d'échec (FR-036) sont laissés à
  l'ajustement en phase de planification et d'équilibrage ; cette spécification fixe le principe des
  systèmes, pas leurs paramètres finaux. La durée de cohabitation requise pour une naissance (un an,
  FR-047) et la marge du quota de genre (± 10%, FR-048) sont en revanche fixées par cette
  spécification et ne sont pas des paramètres d'équilibrage.
- Le mécanisme précis alimentant la santé d'un colon (nourriture, environnement, soins médicaux...)
  sera défini en planification technique ; cette spécification retient seulement que la santé
  influence le rendement au poste.
- L'algorithme précis d'assignation automatique (comment le système choisit quel colon en mode
  automatique affecter à quel poste/tâche de transport/chantier disponible) sera défini en
  planification technique ; cette spécification retient seulement l'existence d'un mode automatique
  par colon, alternatif au mode manuel.
- Les véhicules de transport progressent en capacité/vitesse au fil de la partie (les colons
  assignés au transport étant la solution de départ avant l'accès à des véhicules plus avancés) ;
  le détail de cette progression sera défini en planification technique.
- La Phase 1 n'introduit aucune notion de temps réel multi-session partagé ni de synchronisation :
  chaque partie est strictement locale à un joueur.
- Un logement ne suit qu'un seul cycle de cohabitation/naissance à la fois, indépendamment du
  nombre de colons supplémentaires qui y résident au-delà du couple minimal homme/femme requis ;
  un logement hébergeant plusieurs couples potentiels ne produit pas plusieurs naissances
  simultanées.
- Une fois les colons relogés dans de véritables logements (US7), le Module de survie pourra être
  démantelé pour récupérer une partie de ses ressources ; cette mécanique de démantèlement n'est
  pas spécifiée ni implémentée en Phase 1 et sera détaillée ultérieurement si besoin.
- Le catalogue de bâtiments (FR-044) est attendu, au minimum, pour matérialiser dès User Story 5
  trois bâtiments primitifs « de fortune », en attendant leurs équivalents avancés : une Ferme
  primitive (coût bois + pierre, consomme de l'eau en continu pour produire de la nourriture, taux
  d'équilibrage à définir), un Puits/citerne primitive (coût bois + pierre, faible capacité de
  stockage d'eau de l'ordre de 5 à 10 m³, en attendant une citerne avancée nécessitant du fer, une
  ressource non extractible en tout début de partie), et un Entrepôt primitif (coût bois + pierre,
  en attendant un entrepôt avancé nécessitant d'autres matériaux). Les valeurs exactes (taux,
  capacités, coûts) restent des paramètres d'équilibrage à définir en planification/contenu, au même
  titre que le reste du catalogue.
