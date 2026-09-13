# CLAUDE.md — Contexte projet pour Claude Code

## Le projet
Jeu de stratégie 4X spatial en Unity (C#), à terme un univers persistant en ligne
(1000 systèmes procéduraux, jusqu'à 200 joueurs par galaxie/serveur). Développement
solo, long terme, orienté qualité plutôt que vitesse.

## Règle absolue : développement par phases
Ne JAMAIS essayer d'implémenter le jeu final (galaxie complète, 200 joueurs) directement.
Le projet avance par phases, chacune complète et jouable avant de passer à la suivante :

1. **Phase 1 — Cœur de simulation (solo)** : un seul système solaire généré
   procéduralement, exploration, construction de bâtiments, économie simple.
   Aucun réseau.
2. **Phase 2 — Multi restreint** : même contenu, serveur dédié séparé, 4-10 joueurs
   sur un système partagé.
3. **Phase 3 — Extension** : plusieurs systèmes connectés, conquête, montée en charge.
4. **Phase 4 — Échelle galaxie** : 1000 systèmes, 200 joueurs persistants.

Si une demande dépasse la phase en cours, le signaler avant d'implémenter et proposer
de la découper.

## Conventions de code C#
- Namespace racine : `Game.<Module>` (ex: `Game.Economy`, `Game.Building`, `Game.Procedural`)
- PascalCase pour classes/méthodes publiques, camelCase pour variables privées avec préfixe `_`
- Un ScriptableObject par type de donnée de configuration (ressources, bâtiments, unités)
- Pas de logique métier dans les MonoBehaviour : les MonoBehaviour orchestrent, la logique
  vit dans des classes C# pures (facilite les tests unitaires et un futur portage serveur)
- Commentaires en français, noms de code en anglais

## Structure des dossiers Unity
```
Assets/
  _Project/
    Scripts/
      Economy/
      Building/
      Procedural/
      Core/
    ScriptableObjects/
    Prefabs/
    Scenes/
  Art/
  Plugins/
```

## Git
- Ne jamais modifier les fichiers `.meta` manuellement
- Un commit = une tâche Spec Kit complète et fonctionnelle
- Utiliser Git LFS pour tout asset binaire (modèles, textures, audio)

## Spec-Driven Development
Ce projet utilise GitHub Spec Kit. Chaque feature vit dans `specs/00X-nom-feature/`.
Toujours suivre le cycle : specify → plan → tasks → implement. Ne pas coder sans spec
validée pour les features de la phase en cours.
