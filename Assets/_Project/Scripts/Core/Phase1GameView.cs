using System.Collections.Generic;
using System.Linq;
using Game.Building;
using Game.Colonists;
using Game.Economy;
using Game.Procedural;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Première vue visuelle (formes primitives, couleurs unies — aucun asset externe) de la
    // simulation Phase 1 : grille de tuiles colorées par terrain/brouillard de guerre, bâtiments en
    // primitives 3D, caméra god mode, sélection/construction au clic souris. Orchestration pure
    // (Principe II) : toute la logique de jeu vit dans Phase1GameController (déjà couvert par les
    // tests EditMode via les services Game.<Module> qu'il assemble) ; ce composant ne fait
    // qu'afficher son état et lui transmettre les clics.
    //
    // Duplique volontairement une partie de l'orchestration déjà présente dans Phase1TestHarness
    // (scène de debug Phase1_MVP) plutôt que de la modifier, pour ne rien risquer sur cet outil de
    // test qui fonctionne déjà — Phase1GameController factorise au moins la logique de jeu
    // elle-même entre les deux.
    public sealed class Phase1GameView : MonoBehaviour
    {
        [SerializeField] private Phase1GameContent _content;

        [Header("Génération de planète")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private int _width = 100;
        [SerializeField] private int _height = 100;
        [SerializeField] private int _startingRevealRadius = 4;
        [SerializeField] private float _startingMaterials = 500f;
        [SerializeField] private float _loadingDurationSeconds = 5f;
        [SerializeField] private float _unloadingDurationSeconds = 5f;

        // Durée d'une "année de jeu" utilisée par la cohabitation/natalité (FR-047) : un an continu
        // avant la première tentative de naissance, 18 ans avant la majorité d'un enfant. Valeur
        // d'équilibrage courte par défaut pour rester testable manuellement.
        [SerializeField] private float _secondsPerGameYear = 60f;

        // Modèle réel (FBX) utilisé uniquement pour la Pompe — tous les autres bâtiments restent en
        // primitives pour l'instant (cf. demande explicite). Optionnel : si non assigné, la Pompe
        // retombe sur le cylindre primitif comme avant.
        [Header("Modèle 3D — Pompe")]
        [SerializeField] private GameObject _pumpModelPrefab;

        // La couleur terrain/brouillard de chaque case n'a pas besoin d'être recalculée à chaque
        // frame (le brouillard ne se révèle jamais en continu, FR-004) : ce rafraîchissement
        // périodique évite de réécrire 10 000 couleurs/frame sur une carte 100x100 (perf).
        [Header("Rendu de la grille")]
        [SerializeField] private float _fogRefreshIntervalSeconds = 0.25f;

        private Phase1GameController _controller;
        private Renderer[,] _tileRenderers;
        private Color[,] _baseTileColors;
        private readonly Dictionary<System.Guid, GameObject> _buildingViews = new Dictionary<System.Guid, GameObject>();

        // Décors de repère visuel (arbres/rochers) sur les cases à gisement de bois/pierre : posés
        // une fois à la génération (le gisement ne bouge pas), visibilité togglée avec le
        // brouillard de guerre pour ne rien révéler avant exploration.
        private GameObject[,] _decorationRoots;

        // Fantôme de placement (mode construire) : suit la case survolée, aligné sur la grille ;
        // recréé seulement quand le type de bâtiment sélectionné change de forme.
        private GameObject _ghostObject;
        private BuildingDefinition _ghostForDefinition;

        // Fiche colon ouverte (panel Colons, navigation à deux niveaux : tableau -> fiche).
        private System.Guid? _selectedColonistId;

        // Liste filtrée "Assigner un colon" actuellement dépliée sur la fiche bâtiment (au plus une
        // à la fois) ; null tant qu'aucune n'est ouverte.
        private System.Guid? _assignListBuildingId;
        private Vector2 _buildingActionsScroll;

        private float _fogRefreshTimer;
        private int _hoveredX = -1;
        private int _hoveredY = -1;
        private int _previousHoveredX = -1;
        private int _previousHoveredY = -1;

        // Position fractionnaire (0..1) du curseur dans la case survolée (FR-054) : dérivée du point
        // d'impact du raycast sur le quad de sol, réutilisée telle quelle comme offset de
        // positionnement libre plutôt que d'exposer des sliders dédiés — 0.5/0.5 = centré.
        private float _hoveredOffsetX = 0.5f;
        private float _hoveredOffsetY = 0.5f;

        private GUIStyle _titleStyle;
        private GUIStyle _discreetInfoStyle;
        private Vector2 _categoryScroll;
        private readonly List<Rect> _uiRects = new List<Rect>(); // zones d'UI réservées ce frame (survol/clic 3D désactivés dessus)
        private TaskbarCategory? _openCategory;
        private bool _settingsPanelOpen;

        // Barre de fonctionnalités en bas de l'écran : chaque catégorie n'est qu'une entrée de ce
        // tableau + un cas dans le switch de DrawOpenCategoryPanel — ajouter un futur bouton ne
        // demande pas de redessiner la barre elle-même.
        private enum TaskbarCategory
        {
            Construction,
            Research,
            Logistics,
            Management,
            Colonists,
            Project,
            Map,
            System
        }

        private static readonly TaskbarCategory[] TaskbarOrder =
        {
            TaskbarCategory.Construction,
            TaskbarCategory.Research,
            TaskbarCategory.Logistics,
            TaskbarCategory.Management,
            TaskbarCategory.Colonists,
            TaskbarCategory.Project,
            TaskbarCategory.Map,
            TaskbarCategory.System
        };

        private static string GetCategoryLabel(TaskbarCategory category)
        {
            return category switch
            {
                TaskbarCategory.Construction => "Construction",
                TaskbarCategory.Research => "Recherche",
                TaskbarCategory.Logistics => "Logistique",
                TaskbarCategory.Management => "Gestion",
                TaskbarCategory.Colonists => "Colons",
                TaskbarCategory.Project => "Projet",
                TaskbarCategory.Map => "Map",
                TaskbarCategory.System => "Système",
                _ => category.ToString()
            };
        }

        // Assignation directe (hors Inspector) pour l'outil éditeur Phase1GameSceneBuilder : plus
        // fiable qu'un SerializedObject sur un composant tout juste ajouté à une scène non encore
        // sauvegardée (cf. Phase1TestHarness, même constat).
        public void ConfigureContentForEditorTooling(Phase1GameContent content, GameObject pumpModelPrefab = null)
        {
            _content = content;
            _pumpModelPrefab = pumpModelPrefab;
        }

        private void Awake()
        {
            _controller = new Phase1GameController(_content, _seed, _width, _height,
                _startingRevealRadius, _startingMaterials, _loadingDurationSeconds, _unloadingDurationSeconds,
                _secondsPerGameYear);
        }

        private void Start()
        {
            _controller.NewGame();
            if (_controller.IsReady)
                SpawnTiles();
        }

        private void Update()
        {
            _controller.Update(Time.deltaTime);
            if (!_controller.IsReady) return;

            if (_controller.PendingAssignmentColonistId.HasValue && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _controller.CancelPendingAssignment();

            if (_controller.PendingMultiPurposeExtractorId.HasValue && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _controller.CancelPendingMultiPurposeExtractorPlacement();

            // FR-054 : touche R pour faire pivoter le bâtiment en cours de placement (0°/90°/180°/270°).
            if (_controller.SelectedBuildToPlace != null && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                _controller.RotatePendingBuild();

            _fogRefreshTimer -= Time.deltaTime;
            if (_fogRefreshTimer <= 0f)
            {
                _fogRefreshTimer = _fogRefreshIntervalSeconds;
                RefreshBaseTileColors();
            }

            UpdateHoveredTile();
            ApplyHoverHighlight();
            SyncBuildingViews();
            SyncGhost();
            HandleMouseClick();
        }

        private void SpawnTiles()
        {
            var planet = _controller.Planet;
            _tileRenderers = new Renderer[planet.Width, planet.Height];
            _baseTileColors = new Color[planet.Width, planet.Height];
            _decorationRoots = new GameObject[planet.Width, planet.Height];

            var root = new GameObject("Tiles").transform;
            root.SetParent(transform, false);

            var decorationsRoot = new GameObject("Decorations").transform;
            decorationsRoot.SetParent(transform, false);

            for (var x = 0; x < planet.Width; x++)
            {
                for (var y = 0; y < planet.Height; y++)
                {
                    // Quad à plat, échelle 1 bord à bord avec ses voisins : surface continue par
                    // biome, sans quadrillage visible (contrairement à des cubes espacés).
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.SetParent(root, false);
                    tile.transform.localPosition = new Vector3(x, 0f, y);
                    tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    tile.transform.localScale = Vector3.one;

                    var marker = tile.AddComponent<TileMarker>();
                    marker.X = x;
                    marker.Y = y;

                    var renderer = tile.GetComponent<Renderer>();
                    MakeMatte(renderer.material); // sans quoi le brouillard noir a un reflet spéculaire brillant
                    _tileRenderers[x, y] = renderer;

                    if (!planet.TryGetZone(x, y, out var zone) || zone.Deposit == null) continue;

                    if (zone.Deposit.ResourceId == _controller.WoodResource.Id)
                        _decorationRoots[x, y] = SpawnTreeDecoration(x, y, decorationsRoot);
                    else if (zone.Deposit.ResourceId == _controller.StoneResource.Id)
                        _decorationRoots[x, y] = SpawnRockDecoration(x, y, decorationsRoot);
                }
            }

            RefreshBaseTileColors();
        }

        // Repère visuel rapide (formes primitives, pas de vrai modèle) sur une case à gisement de
        // bois : tronc (cylindre) + feuillage (sphère) — Unity n'a pas de primitive cône native.
        private GameObject SpawnTreeDecoration(int x, int y, Transform parent)
        {
            var decoRoot = new GameObject($"Deco_Tree_{x}_{y}");
            decoRoot.transform.SetParent(parent, false);
            decoRoot.transform.localPosition = new Vector3(x, 0f, y);

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(decoRoot.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            trunk.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            SetupDecorationRenderer(trunk, new Color(0.40f, 0.26f, 0.13f));

            var foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            foliage.name = "Foliage";
            foliage.transform.SetParent(decoRoot.transform, false);
            foliage.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            foliage.transform.localScale = Vector3.one * 0.4f;
            SetupDecorationRenderer(foliage, new Color(0.16f, 0.50f, 0.20f));

            return decoRoot;
        }

        // Repère visuel rapide sur une case à gisement de pierre : 2-3 petits cubes gris, taille/
        // position/rotation légèrement randomisées (seed dérivée des coordonnées, déterministe)
        // pour un aspect "tas de rochers" plutôt qu'un cube unique trop régulier.
        private GameObject SpawnRockDecoration(int x, int y, Transform parent)
        {
            var decoRoot = new GameObject($"Deco_Rock_{x}_{y}");
            decoRoot.transform.SetParent(parent, false);
            decoRoot.transform.localPosition = new Vector3(x, 0f, y);

            var random = new System.Random(x * 92821 + y * 68917);
            var rockCount = 2 + random.Next(2);

            for (var i = 0; i < rockCount; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = $"Rock_{i}";
                rock.transform.SetParent(decoRoot.transform, false);

                var offsetX = (float)(random.NextDouble() - 0.5) * 0.5f;
                var offsetZ = (float)(random.NextDouble() - 0.5) * 0.5f;
                var scale = 0.15f + (float)random.NextDouble() * 0.15f;
                rock.transform.localPosition = new Vector3(offsetX, scale / 2f, offsetZ);
                rock.transform.localScale = new Vector3(scale, scale * 0.8f, scale);
                rock.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

                var gray = 0.45f + (float)random.NextDouble() * 0.15f;
                SetupDecorationRenderer(rock, new Color(gray, gray, gray * 0.95f));
            }

            return decoRoot;
        }

        // Retire le collider (le décor ne doit jamais intercepter un raycast destiné à la tuile ou
        // au bâtiment) et applique la couleur + le rendu mat.
        private static void SetupDecorationRenderer(GameObject go, Color color)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var material = go.GetComponent<Renderer>().material;
            material.color = color;
            MakeMatte(material);
        }

        // Retire le reflet spéculaire par défaut du matériau URP Lit (visible surtout sur le
        // brouillard de guerre noir, qui doit lire comme "vide/inconnu" plutôt que comme une
        // surface brillante) : aucun effet sur la teinte, seulement sur la brillance/le métal.
        private static void MakeMatte(Material material)
        {
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        }

        // Rafraîchissement peu fréquent (cf. _fogRefreshIntervalSeconds) de la couleur terrain/
        // brouillard de toutes les cases. N'écrase pas la surbrillance de la case actuellement
        // survolée pour éviter un scintillement d'une frame.
        private void RefreshBaseTileColors()
        {
            var planet = _controller.Planet;
            for (var x = 0; x < planet.Width; x++)
            {
                for (var y = 0; y < planet.Height; y++)
                {
                    if (!planet.TryGetZone(x, y, out var zone)) continue;

                    var color = GetBaseZoneColor(zone);
                    _baseTileColors[x, y] = color;

                    if (_decorationRoots[x, y] != null)
                        _decorationRoots[x, y].SetActive(zone.IsRevealed); // rien à voir avant exploration (FR-004)

                    if (x == _hoveredX && y == _hoveredY) continue;
                    _tileRenderers[x, y].material.color = color;
                }
            }
        }

        private void UpdateHoveredTile()
        {
            _hoveredX = -1;
            _hoveredY = -1;

            var mouse = Mouse.current;
            var camera = Camera.main;
            if (mouse == null || camera == null || IsPointerOverUI()) return;

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 1000f)) return;

            var tileMarker = hit.collider.GetComponent<TileMarker>();
            if (tileMarker != null)
            {
                _hoveredX = tileMarker.X;
                _hoveredY = tileMarker.Y;
                _hoveredOffsetX = Mathf.Clamp01(hit.point.x - tileMarker.X + 0.5f);
                _hoveredOffsetY = Mathf.Clamp01(hit.point.z - tileMarker.Y + 0.5f);
                return;
            }

            var buildingMarker = hit.collider.GetComponent<BuildingMarker>();
            if (buildingMarker != null)
            {
                _hoveredX = buildingMarker.X;
                _hoveredY = buildingMarker.Y;
                _hoveredOffsetX = 0.5f;
                _hoveredOffsetY = 0.5f;
            }
        }

        // Éclaircit la case survolée pour indiquer qu'elle est sélectionnable — jamais sur une
        // case non révélée (brouillard de guerre opaque, aucun survol possible tant qu'elle n'est
        // pas explorée). Ne touche que les deux cases concernées (ancienne/nouvelle survolée),
        // pas toute la grille : coût O(1) par frame même sur une carte 100x100.
        private void ApplyHoverHighlight()
        {
            if (_previousHoveredX >= 0 && (_previousHoveredX != _hoveredX || _previousHoveredY != _hoveredY))
                _tileRenderers[_previousHoveredX, _previousHoveredY].material.color = _baseTileColors[_previousHoveredX, _previousHoveredY];

            if (_hoveredX >= 0 && _controller.Planet.TryGetZone(_hoveredX, _hoveredY, out var zone) && zone.IsRevealed)
                _tileRenderers[_hoveredX, _hoveredY].material.color = Color.Lerp(_baseTileColors[_hoveredX, _hoveredY], Color.white, 0.35f);

            _previousHoveredX = _hoveredX;
            _previousHoveredY = _hoveredY;
        }

        private static Color GetBaseZoneColor(Zone zone)
        {
            // Brouillard de guerre opaque : aucune teinte de terrain visible tant que la case
            // n'est pas révélée.
            return zone.IsRevealed ? GetTerrainColor(zone.Terrain) : Color.black;
        }

        private static Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plains => new Color(0.36f, 0.62f, 0.29f),
                TerrainType.Hills => new Color(0.55f, 0.50f, 0.25f),
                TerrainType.Mountains => new Color(0.5f, 0.5f, 0.5f),
                TerrainType.Water => new Color(0.20f, 0.45f, 0.85f),
                _ => Color.magenta
            };
        }

        private void SyncBuildingViews()
        {
            var currentIds = new HashSet<System.Guid>(_controller.Buildings.Select(b => b.Id));

            var staleIds = _buildingViews.Keys.Where(id => !currentIds.Contains(id)).ToList();
            foreach (var id in staleIds)
            {
                if (_buildingViews[id] != null) Destroy(_buildingViews[id]);
                _buildingViews.Remove(id);
            }

            foreach (var building in _controller.Buildings)
            {
                if (!_controller.DefinitionsById.TryGetValue(building.Id, out var definition)) continue;

                var usesPumpModel = definition == _controller.PumpDefinition && _pumpModelPrefab != null;

                if (!_buildingViews.TryGetValue(building.Id, out var go) || go == null)
                {
                    if (usesPumpModel)
                    {
                        go = Instantiate(_pumpModelPrefab);
                        FitColliderToRenderers(go);
                    }
                    else
                    {
                        var isExtractorLike = definition == _controller.ExtractorDefinition || definition == _controller.PumpDefinition;
                        go = GameObject.CreatePrimitive(isExtractorLike ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                    }

                    go.name = $"Building_{definition.Id}_{building.X}_{building.Y}";
                    go.transform.SetParent(transform, false);

                    var marker = go.AddComponent<BuildingMarker>();
                    marker.BuildingId = building.Id;
                    marker.X = building.X;
                    marker.Y = building.Y;

                    _buildingViews[building.Id] = go;
                }

                var isOperational = building.IsOperational;

                // FR-054 : positionnement libre dans la case + rotation, purement cosmétiques.
                var posX = building.X + building.OffsetX - 0.5f;
                var posZ = building.Y + building.OffsetY - 0.5f;
                go.transform.localRotation = Quaternion.Euler(0f, (float)building.Rotation, 0f);

                if (usesPumpModel)
                {
                    // Modèle réel : un étirement non-uniforme (comme les primitives) déformerait sa
                    // géométrie — seule une réduction d'échelle uniforme distingue le chantier.
                    go.transform.localPosition = new Vector3(posX, 0f, posZ);
                    go.transform.localScale = Vector3.one * (isOperational ? 1f : 0.6f);
                }
                else
                {
                    var height = isOperational ? 1.2f : 0.6f; // chantier : visuellement plus bas (distinct de l'opérationnel)
                    go.transform.localPosition = new Vector3(posX, height / 2f, posZ);
                    go.transform.localScale = new Vector3(0.7f, height, 0.7f);

                    var color = GetBuildingColor(definition);
                    if (!isOperational) color = Color.Lerp(color, Color.gray, 0.5f); // chantier : couleur désaturée
                    go.GetComponent<Renderer>().material.color = color;
                }
            }
        }

        // Ajoute un BoxCollider ajusté aux Renderer(s) du modèle importé (un FBX n'a généralement
        // aucun collider par défaut, contrairement aux primitives créées via CreatePrimitive) — sans
        // ça, les clics 3D (HandleMouseClick) ne détecteraient jamais la Pompe. Calculé avant tout
        // repositionnement/redimensionnement de go, donc en l'espace local du prefab tel qu'importé.
        private static void FitColliderToRenderers(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var scale = go.transform.lossyScale;
            var collider = go.AddComponent<BoxCollider>();
            collider.center = go.transform.InverseTransformPoint(bounds.center);
            collider.size = new Vector3(
                scale.x == 0f ? bounds.size.x : bounds.size.x / scale.x,
                scale.y == 0f ? bounds.size.y : bounds.size.y / scale.y,
                scale.z == 0f ? bounds.size.z : bounds.size.z / scale.z);
        }

        private Color GetBuildingColor(BuildingDefinition definition)
        {
            if (definition == _controller.ShelterDefinition) return Color.white;
            if (definition == _controller.StorageDefinition) return new Color(0.65f, 0.50f, 0.30f);
            if (definition == _controller.CisternDefinition) return new Color(0.20f, 0.70f, 0.90f);
            if (definition == _controller.ExtractorDefinition) return new Color(0.90f, 0.55f, 0.15f);
            if (definition == _controller.PumpDefinition) return new Color(0.15f, 0.35f, 0.90f);
            if (definition == _controller.HousingDefinition) return new Color(0.85f, 0.70f, 0.45f);
            return Color.gray;
        }

        private static bool IsCylinderShaped(Phase1GameController controller, BuildingDefinition definition)
        {
            return definition == controller.ExtractorDefinition || definition == controller.PumpDefinition;
        }

        // Fantôme de placement (mode construire) : suit la case survolée, aligné sur la grille.
        // Recréé seulement quand la forme/le bâtiment sélectionné change, pas à chaque frame. La
        // Pompe utilise le modèle réel (pompe_a_eau.fbx) comme le bâtiment placé (cf. usesPumpModel
        // dans SyncBuildingViews) plutôt qu'un cylindre, pour un aperçu fidèle avant validation.
        private void SyncGhost()
        {
            var selected = _controller.SelectedBuildToPlace;
            if (selected == null)
            {
                if (_ghostObject != null) _ghostObject.SetActive(false);
                return;
            }

            var usesPumpModel = selected == _controller.PumpDefinition && _pumpModelPrefab != null;

            if (_ghostObject == null || _ghostForDefinition != selected)
            {
                if (_ghostObject != null) Destroy(_ghostObject);

                if (usesPumpModel)
                {
                    _ghostObject = Instantiate(_pumpModelPrefab);

                    // Le fantôme ne doit jamais intercepter un raycast (clics/survol de la grille) —
                    // le modèle importé peut porter ses propres colliders, contrairement à une
                    // primitive dont on en détruit un seul.
                    foreach (var modelCollider in _ghostObject.GetComponentsInChildren<Collider>())
                        Destroy(modelCollider);

                    // Instances par renderer (Renderer.materials, pas sharedMaterial) pour ne jamais
                    // modifier le matériau partagé du prefab/du modèle déjà placé.
                    foreach (var renderer in _ghostObject.GetComponentsInChildren<Renderer>())
                        foreach (var material in renderer.materials)
                            MakeGhostTransparentPreservingLook(material, 0.45f);
                }
                else
                {
                    var primitive = IsCylinderShaped(_controller, selected) ? PrimitiveType.Cylinder : PrimitiveType.Cube;
                    _ghostObject = GameObject.CreatePrimitive(primitive);

                    var ghostCollider = _ghostObject.GetComponent<Collider>();
                    if (ghostCollider != null) Destroy(ghostCollider); // le fantôme ne doit jamais intercepter un raycast

                    var color = GetBuildingColor(selected);
                    color.a = 0.45f;
                    MakeTransparent(_ghostObject.GetComponent<Renderer>().material, color);
                }

                _ghostObject.name = "PlacementGhost";
                _ghostObject.transform.SetParent(transform, false);
                _ghostForDefinition = selected;
            }

            if (_hoveredX < 0)
            {
                _ghostObject.SetActive(false);
                return;
            }

            _ghostObject.SetActive(true);

            // FR-054 : le fantôme suit l'offset fractionnaire du curseur dans la case et l'orientation
            // choisie (touche R), pour prévisualiser le placement avant validation.
            var posX = _hoveredX + _hoveredOffsetX - 0.5f;
            var posZ = _hoveredY + _hoveredOffsetY - 0.5f;
            _ghostObject.transform.localRotation = Quaternion.Euler(0f, (float)_controller.PendingBuildRotation, 0f);

            if (usesPumpModel)
            {
                // Modèle réel : aucun étirement, comme le bâtiment une fois placé (cf. usesPumpModel
                // dans SyncBuildingViews).
                _ghostObject.transform.localPosition = new Vector3(posX, 0f, posZ);
                _ghostObject.transform.localScale = Vector3.one;
            }
            else
            {
                const float height = 1.2f;
                _ghostObject.transform.localPosition = new Vector3(posX, height / 2f, posZ);
                _ghostObject.transform.localScale = new Vector3(0.7f, height, 0.7f);
            }
        }

        // Configure un matériau URP Lit en mode transparent (surface + blend + render queue), pour
        // le fantôme de placement — recette standard URP.
        private static void ApplyTransparentSurfaceSettings(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f); // 1 = Transparent
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f); // 0 = Alpha
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // Fantôme en primitive (matériau par défaut, sans texture propre) : couleur imposée
        // entièrement (cf. GetBuildingColor) et rendu mat (MakeMatte), comme avant.
        private static void MakeTransparent(Material material, Color color)
        {
            ApplyTransparentSurfaceSettings(material);
            material.color = color;
            MakeMatte(material);
        }

        // Fantôme utilisant un modèle réel importé (pompe_a_eau.fbx) : conserve la teinte et le
        // rendu (métallique/brillance) d'origine du matériau, ne réduit que son opacité — un flat
        // color ou MakeMatte écraserait l'aspect du modèle importé.
        private static void MakeGhostTransparentPreservingLook(Material material, float alpha)
        {
            ApplyTransparentSurfaceSettings(material);
            var color = material.color;
            color.a = alpha;
            material.color = color;
        }

        private void HandleMouseClick()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Clic droit : désélectionne le bâtiment actuellement sélectionné (ferme sa fiche) ou,
            // si un type de bâtiment est en cours de placement (fantôme), annule ce placement.
            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (_controller.SelectedBuildToPlace != null)
                {
                    _controller.CancelBuildSelection();
                    return;
                }

                if (_controller.GetSelectedBuilding() != null)
                {
                    _controller.SelectedX = -1;
                    _controller.SelectedY = -1;
                    return;
                }
            }

            if (!mouse.leftButton.wasPressedThisFrame) return;
            if (IsPointerOverUI()) return;

            var camera = Camera.main;
            if (camera == null) return;

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, 1000f)) return;

            var buildingMarker = hit.collider.GetComponent<BuildingMarker>();
            if (buildingMarker != null)
            {
                if (_controller.PendingMultiPurposeExtractorId.HasValue)
                {
                    ResolvePendingMultiPurposeExtractorPlacement(buildingMarker.X, buildingMarker.Y);
                }
                else if (_controller.PendingAssignmentColonistId.HasValue)
                {
                    ResolvePendingAssignment(buildingMarker.BuildingId);
                }
                else if (_controller.SelectedBuildToPlace != null)
                {
                    _controller.TryBuild(buildingMarker.X, buildingMarker.Y, _hoveredOffsetX, _hoveredOffsetY, _controller.PendingBuildRotation); // occupé : refusé par le service, message explicite
                }
                else
                {
                    _controller.SelectedX = buildingMarker.X;
                    _controller.SelectedY = buildingMarker.Y;
                }
                return;
            }

            var tileMarker = hit.collider.GetComponent<TileMarker>();
            if (tileMarker != null)
            {
                if (_controller.PendingMultiPurposeExtractorId.HasValue)
                {
                    ResolvePendingMultiPurposeExtractorPlacement(tileMarker.X, tileMarker.Y);
                }
                else if (_controller.PendingAssignmentColonistId.HasValue)
                {
                    _controller.LastMessage = "Sélectionnez un bâtiment (pas une case vide) pour l'assignation manuelle.";
                }
                else if (_controller.SelectedBuildToPlace != null)
                {
                    _controller.TryBuild(tileMarker.X, tileMarker.Y, _hoveredOffsetX, _hoveredOffsetY, _controller.PendingBuildRotation);
                }
                else
                {
                    _controller.SelectedX = tileMarker.X;
                    _controller.SelectedY = tileMarker.Y;
                }
            }
        }

        // Valide le bouton "Placer"/"Déplacer" du panneau du Module de survie (FR-052) : la case
        // cliquée (bâtiment ou case vide, peu importe) est proposée comme cible, le contrôleur/
        // service valide le gisement.
        private void ResolvePendingMultiPurposeExtractorPlacement(int x, int y)
        {
            var extractorId = _controller.PendingMultiPurposeExtractorId;
            _controller.PendingMultiPurposeExtractorId = null;

            var extractor = _controller.MultiPurposeExtractors.FirstOrDefault(e => e.Id == extractorId);
            if (extractor == null) return;

            _controller.TryPlaceMultiPurposeExtractor(extractor, x, y);
        }

        // Valide le bouton "Assigner" de la fiche colon (point 3) : le colon en attente est lié au
        // bâtiment cliqué, quel que soit son type (le contrôleur détermine le poste approprié).
        private void ResolvePendingAssignment(System.Guid buildingId)
        {
            var colonistId = _controller.PendingAssignmentColonistId;
            _controller.PendingAssignmentColonistId = null;

            var colonist = _controller.Colonists.FirstOrDefault(c => c.Id == colonistId);
            var building = _controller.Buildings.FirstOrDefault(b => b.Id == buildingId);
            if (colonist == null || building == null) return;

            _controller.TryAssignColonistToBuilding(colonist, building);
        }

        private bool IsPointerOverUI()
        {
            var mouse = Mouse.current;
            if (mouse == null) return false;

            var screenPos = mouse.position.ReadValue();
            var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y); // écran (bas-gauche) -> GUI (haut-gauche)
            foreach (var rect in _uiRects)
            {
                if (rect.Contains(guiPos)) return true;
            }
            return false;
        }

        private void OnGUI()
        {
            if (!_controller.IsReady)
            {
                GUILayout.Label("Phase1GameView : contenu manquant, voir la Console (assignez Phase1GameContent dans l'Inspector).");
                return;
            }

            _titleStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 };
            _discreetInfoStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = new Color(1f, 1f, 1f, 0.75f) } };
            _uiRects.Clear();

            DrawHeaderPanel();
            DrawSettingsPanel();
            DrawBuildingActionsPanel();
            DrawHoverInfoPanel();
            DrawTaskbar();
            DrawOpenCategoryPanel();
        }

        private const float HeaderHeight = 56f;

        // Bandeau du haut : ressources actuelles (retirées du panel Construction) + icône rouage —
        // regroupés ici plutôt qu'éparpillés, cf. demande de réorganisation.
        private void DrawHeaderPanel()
        {
            var rect = new Rect(0, 0, Screen.width, HeaderHeight);
            _uiRects.Add(rect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("⚙", GUILayout.Width(44), GUILayout.Height(44)))
                _settingsPanelOpen = !_settingsPanelOpen;

            GUILayout.Space(16);
            GUILayout.Label($"Colons : {_controller.Colonists.Count}");
            GUILayout.Space(10);
            GUILayout.Label($"{_controller.GalacticCredits:0} CG");

            GUILayout.Space(16);
            DrawResourceSummary();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawResourceSummary()
        {
            GUILayout.Label("Matériaux :");
            foreach (var kvp in _controller.Warehouse.Quantities)
                GUILayout.Label($"{_controller.ResourceDisplayName(kvp.Key)} {kvp.Value:0.0}");

            GUILayout.Space(14);
            GUILayout.Label("Entrepôt :");
            foreach (var kvp in _controller.AggregateStorageQuantities(_controller.StorageDefinition))
                GUILayout.Label($"{_controller.ResourceDisplayName(kvp.Key)} {kvp.Value:0.0}");

            GUILayout.Space(14);
            GUILayout.Label("Citerne :");
            foreach (var kvp in _controller.AggregateStorageQuantities(_controller.CisternDefinition))
                GUILayout.Label($"{_controller.ResourceDisplayName(kvp.Key)} {kvp.Value:0.0}");
        }

        // Tableau de bord global de la colonie (panel Gestion) : vue d'ensemble uniquement, les
        // autres panels (Colons, Logistique...) restent focalisés sur leur gestion spécifique.
        private void DrawManagementCategory()
        {
            GUILayout.Label($"Colons : {_controller.Colonists.Count}");
            GUILayout.Label($"Santé globale : {_controller.ComputeAveragePopulationHealth():P0}");
            GUILayout.Label($"Taux de naissance estimé : {_controller.EstimateExpectedBirthsPerYear():0.0} / an");

            GUILayout.Space(10);
            // Revenus/Dépenses : placeholder inerte tant que la mécanique des Crédits Galactiques
            // n'est pas définie (cf. Phase1GameController.GalacticCredits).
            GUILayout.Label($"Trésorerie : {_controller.GalacticCredits:0} CG");
            GUILayout.Label("Revenus : 0 CG/an (à venir)");
            GUILayout.Label("Dépenses : 0 CG/an (à venir)");

            GUILayout.Space(10);
            GUILayout.Label("Stock de ressources :", _titleStyle);
            DrawResourceSummary();

            GUILayout.Space(10);
            GUILayout.Label($"Véhicules (transporteurs logistiques) : {_controller.TransportTasks.Count}");
        }

        private void DrawSettingsPanel()
        {
            if (!_settingsPanelOpen) return;

            var rect = new Rect(Screen.width - 330, HeaderHeight + 10f, 320, 320);
            _uiRects.Add(rect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Réglages", _titleStyle);
            GUILayout.Space(6);
            DrawSystemCategory();
            GUILayout.EndArea();
        }

        // Actions sur le bâtiment cliqué (recycler, cibler comme destination, effectif/rendement,
        // transport, résidents...) : reste déclenché par clic (ce sont des actions engageantes, pas
        // de simples informations) — contrairement aux infos passives de case/bâtiment, désormais
        // dans DrawHoverInfoPanel. N'occupe de place que si un bâtiment est effectivement cliqué.
        private void DrawBuildingActionsPanel()
        {
            if (_controller.SelectedX < 0 || !_controller.Planet.TryGetZone(_controller.SelectedX, _controller.SelectedY, out var zone) || !zone.BuildingId.HasValue)
                return;

            var building = _controller.Buildings.FirstOrDefault(b => b.Id == zone.BuildingId.Value);
            if (building == null || !_controller.DefinitionsById.TryGetValue(building.Id, out var def)) return;

            var rect = new Rect(10, HeaderHeight + 10f, 340, 420);
            _uiRects.Add(rect);

            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label($"{building.State}", _discreetInfoStyle);
            var displayedName = string.IsNullOrWhiteSpace(building.CustomName) ? def.DisplayName : building.CustomName;
            var newBuildingName = GUILayout.TextField(displayedName, _titleStyle);
            if (newBuildingName != displayedName)
                _controller.SetBuildingName(building, newBuildingName);

            var scroll = GUILayout.BeginScrollView(_buildingActionsScroll);
            DrawBuildingActions(building, def);
            _buildingActionsScroll = scroll;
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // Affichage continu (mis à jour à chaque frame, pas seulement au clic) de la case
        // actuellement survolée par la souris : terrain, ressources présentes, bâtiment le cas
        // échéant. Respecte le brouillard de guerre (aucune info sur une case non explorée).
        private void DrawHoverInfoPanel()
        {
            var rect = new Rect(10, Screen.height - 120f, 420, 110f);
            _uiRects.Add(rect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            DrawHoveredTileInfo();

            if (!string.IsNullOrEmpty(_controller.LastMessage))
            {
                GUILayout.Space(4);
                GUILayout.Label(_controller.LastMessage, _discreetInfoStyle);
            }
            GUILayout.EndArea();
        }

        private void DrawHoveredTileInfo()
        {
            if (_hoveredX < 0 || !_controller.Planet.TryGetZone(_hoveredX, _hoveredY, out var zone))
            {
                GUILayout.Label("Survolez une case pour voir ses informations.", _discreetInfoStyle);
                return;
            }

            if (!zone.IsRevealed)
            {
                GUILayout.Label($"Case ({_hoveredX},{_hoveredY}) — non explorée", _discreetInfoStyle);
                return;
            }

            GUILayout.Label($"Case ({_hoveredX},{_hoveredY}) — {zone.Terrain}", _discreetInfoStyle);

            if (zone.Deposit != null)
            {
                var remaining = zone.Deposit.IsInfinite ? "illimité" : $"{zone.Deposit.RemainingQuantity:0.0} restant";
                GUILayout.Label($"Gisement: {_controller.ResourceDisplayName(zone.Deposit.ResourceId)} ({remaining})", _discreetInfoStyle);
            }

            if (zone.BuildingId.HasValue)
            {
                var building = _controller.Buildings.FirstOrDefault(b => b.Id == zone.BuildingId.Value);
                if (building != null && _controller.DefinitionsById.TryGetValue(building.Id, out var def))
                    GUILayout.Label($"Bâtiment: {def.DisplayName} — {building.State}", _discreetInfoStyle);
            }
        }

        private void DrawTaskbar()
        {
            const float buttonSize = 64f;
            const float spacing = 6f;
            var barWidth = TaskbarOrder.Length * (buttonSize + spacing) + spacing;
            var barHeight = buttonSize + spacing * 2f;
            var rect = new Rect((Screen.width - barWidth) / 2f, Screen.height - barHeight - 10f, barWidth, barHeight);
            _uiRects.Add(rect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Space(spacing);
            foreach (var category in TaskbarOrder)
            {
                var isOpen = _openCategory == category;
                if (GUILayout.Button(GetCategoryLabel(category), GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                    _openCategory = isOpen ? (TaskbarCategory?)null : category;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // Contenu de la catégorie ouverte. Les catégories sans contenu détaillé pour l'instant
        // (Logistique/Projet/Map) affichent un simple placeholder, précisé au fil des prochains
        // échanges — la structure n'a pas besoin de changer pour les remplir.
        private void DrawOpenCategoryPanel()
        {
            if (!_openCategory.HasValue) return;

            const float panelWidth = 360f;
            const float panelHeight = 320f;
            const float reservedBottom = 96f; // doit couvrir la hauteur de la barre (DrawTaskbar) + sa marge
            var rect = new Rect((Screen.width - panelWidth) / 2f, Screen.height - reservedBottom - panelHeight - 10f, panelWidth, panelHeight);
            _uiRects.Add(rect);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(GetCategoryLabel(_openCategory.Value), _titleStyle);
            GUILayout.Space(6);

            var scroll = GUILayout.BeginScrollView(_categoryScroll);
            switch (_openCategory.Value)
            {
                case TaskbarCategory.Construction:
                    DrawConstructionCategory();
                    break;
                case TaskbarCategory.Research:
                    DrawResearchCategory();
                    break;
                case TaskbarCategory.Management:
                    DrawManagementCategory();
                    break;
                case TaskbarCategory.Colonists:
                    DrawColonists();
                    break;
                // TaskbarCategory.System : dédié à la future carte du système solaire (vision à
                // venir) — placeholder pour l'instant comme Logistique/Projet/Map, cf. default.
                // Vitesse de simulation + sauvegarde/chargement ont déménagé dans le panel
                // Réglages (icône rouage), cf. DrawSettingsPanel.
                default:
                    GUILayout.Label("Contenu à venir — sera précisé dans une prochaine passe.");
                    break;
            }
            _categoryScroll = scroll;
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // Navigation à deux niveaux (catégorie de bâtiment -> bâtiment -> mode placement).
        // Ajouter un bâtiment à une catégorie existante ne touche que
        // GetBuildingsForConstructionCategory ; ajouter une catégorie n'ajoute qu'une entrée à
        // ConstructionCategoryOrder (+ son libellé et son contenu) — pas de refonte de l'UI.
        private enum ConstructionCategory
        {
            Housing,
            Production,
            Storage,
            Mining,
            Energy
        }

        private static readonly ConstructionCategory[] ConstructionCategoryOrder =
        {
            ConstructionCategory.Housing,
            ConstructionCategory.Production,
            ConstructionCategory.Storage,
            ConstructionCategory.Mining,
            ConstructionCategory.Energy
        };

        private static string GetConstructionCategoryLabel(ConstructionCategory category)
        {
            return category switch
            {
                ConstructionCategory.Housing => "Habitation",
                ConstructionCategory.Production => "Production",
                ConstructionCategory.Storage => "Stockage",
                ConstructionCategory.Mining => "Minière",
                ConstructionCategory.Energy => "Energie",
                _ => category.ToString()
            };
        }

        private ConstructionCategory? _openConstructionCategory;

        private List<BuildingDefinition> GetBuildingsForConstructionCategory(ConstructionCategory category)
        {
            return category switch
            {
                ConstructionCategory.Housing => new List<BuildingDefinition> { _controller.HousingDefinition },
                ConstructionCategory.Storage => new List<BuildingDefinition> { _controller.StorageDefinition, _controller.CisternDefinition },
                ConstructionCategory.Mining => new List<BuildingDefinition> { _controller.ExtractorDefinition, _controller.PumpDefinition },
                // Production/Energie : contenu à définir dans un prochain échange.
                _ => new List<BuildingDefinition>()
            };
        }

        // Les ressources (trésor/entrepôts/citernes) sont affichées dans le bandeau du haut
        // (DrawHeaderPanel), pas ici.
        private void DrawConstructionCategory()
        {
            if (_openConstructionCategory.HasValue)
                DrawConstructionCategoryBuildings(_openConstructionCategory.Value);
            else
                DrawConstructionCategoryList();

        }

        private void DrawConstructionCategoryList()
        {
            GUILayout.Label("Catégories de bâtiments (puis choisir un bâtiment) :");
            foreach (var category in ConstructionCategoryOrder)
            {
                if (GUILayout.Button(GetConstructionCategoryLabel(category)))
                    _openConstructionCategory = category;
            }
        }

        private void DrawConstructionCategoryBuildings(ConstructionCategory category)
        {
            if (GUILayout.Button("← Retour aux catégories"))
            {
                _openConstructionCategory = null;
                return;
            }

            GUILayout.Label(GetConstructionCategoryLabel(category) + " — puis cliquer une case révélée dans la vue :");

            var buildings = GetBuildingsForConstructionCategory(category);
            if (buildings.Count == 0)
            {
                GUILayout.Label("Aucun bâtiment pour l'instant — sera précisé dans un prochain échange.");
                return;
            }

            foreach (var definition in buildings)
                DrawBuildButton(definition);
        }

        private void DrawResearchCategory()
        {
            GUILayout.Label("Cible active (un chercheur à la fois) :");
            GUILayout.BeginHorizontal();
            DrawResearchTargetButton(_controller.WoodResource.Id, "Bois");
            DrawResearchTargetButton(_controller.StoneResource.Id, "Pierre");
            DrawResearchTargetButton(_controller.WaterResource.Id, "Eau");
            GUILayout.EndHorizontal();
            foreach (var kvp in _controller.TechnologiesByResourceId)
            {
                var marker = kvp.Key == _controller.SelectedResearchResourceId ? "▶ " : "  ";
                GUILayout.Label($"{marker}{_controller.ResourceDisplayName(kvp.Key)} : {kvp.Value.ProgressCurrent:0.0}/{kvp.Value.ProgressRequired}{(kvp.Value.IsUnlocked ? " (débloquée)" : "")}");
            }
        }

        private void DrawSystemCategory()
        {
            GUILayout.Label($"Simulation : x{_controller.SpeedMultiplier:0.#} {(_controller.IsPaused ? "(pause)" : "")}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_controller.IsPaused ? "Reprendre" : "Pause"))
            {
                if (_controller.IsPaused) _controller.Resume(); else _controller.Pause();
            }
            if (GUILayout.Button("x1")) _controller.SetSpeed(1f);
            if (GUILayout.Button("x2")) _controller.SetSpeed(2f);
            if (GUILayout.Button("x3")) _controller.SetSpeed(3f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Mode d'assignation par défaut (nouveaux colons — n'affecte jamais un colon déjà existant) :");
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_controller.DefaultAssignmentMode == AssignmentMode.Manual, "Manuel", "Button"))
                _controller.DefaultAssignmentMode = AssignmentMode.Manual;
            if (GUILayout.Toggle(_controller.DefaultAssignmentMode == AssignmentMode.Automatic, "Automatique", "Button"))
                _controller.DefaultAssignmentMode = AssignmentMode.Automatic;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Sauvegarde : planète/fog, bâtiments, technologies, transport uniquement (pas colons/stocks, cf. US6).");
            _controller.SaveSlotName = GUILayout.TextField(_controller.SaveSlotName);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sauvegarder")) _controller.SaveGame();
            if (GUILayout.Button("Charger")) _controller.LoadGame();
            if (GUILayout.Button("Nouvelle partie"))
            {
                _controller.NewGame();
                _selectedColonistId = null;
                _buildingViews.Clear(); // les GameObjects de l'ancienne partie seront recréés par SyncBuildingViews
                foreach (Transform child in transform)
                    if (child.name.StartsWith("Building_")) Destroy(child.gameObject);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawBuildingActions(BuildingInstance building, BuildingDefinition def)
        {
            GUILayout.Label($"Chantier: {building.ConstructionProgress:0.0}/{def.ConstructionDuration}");

            if (building.State == BuildingState.UnderConstruction)
            {
                if (GUILayout.Button("Construire (assigne automatiquement un Maçon disponible)"))
                    _controller.TryAutoAssignMason(building);
            }

            DrawAssignColonistSection(building);

            if (def == _controller.ExtractorDefinition || def == _controller.PumpDefinition)
            {
                var workers = _controller.GetAssignedWorkers(building.Id);
                _controller.Planet.TryGetZone(building.DepositX, building.DepositY, out var zone);
                var extractionJobId = zone?.Deposit != null && zone.Deposit.ResourceId == _controller.WoodResource.Id
                    ? JobDefinition.WoodcutterJobId
                    : JobDefinition.MinerJobId;
                var buildingYield = ExtractorYield.ComputeBuildingYield(workers, extractionJobId);
                GUILayout.Label($"Effectif: {workers.Count}/{ExtractorYield.MaxWorkerSlots} — rendement: {buildingYield:P0}");
                if (_controller.ExtractorOutputBuffers.TryGetValue(building.Id, out var buffer))
                {
                    foreach (var kvp in buffer.Quantities)
                        GUILayout.Label($"buffer: {_controller.ResourceDisplayName(kvp.Key)} = {kvp.Value:0.0}");
                }

                GUILayout.Label(_controller.DescribeTransportStatus(building));
            }

            if (def == _controller.StorageDefinition || def == _controller.CisternDefinition)
            {
                if (_controller.StorageInventoriesByBuildingId.TryGetValue(building.Id, out var storageInventory))
                {
                    GUILayout.Label("Contenu :");
                    foreach (var kvp in storageInventory.Quantities)
                        GUILayout.Label($"  {_controller.ResourceDisplayName(kvp.Key)} : {kvp.Value:0.0}");
                }

                if (building.State == BuildingState.Operational)
                {
                    var isCurrentDestination = _controller.SelectedDestinationBuildingId == building.Id;
                    if (GUILayout.Button(isCurrentDestination ? "Destination ciblée ✓" : "Cibler comme destination"))
                        _controller.SelectedDestinationBuildingId = building.Id;
                }
            }

            if (def.IsHousing)
            {
                var residents = _controller.Colonists.Where(c => c.HousingId == building.Id).ToList();
                var adults = residents.Where(c => !c.IsChild).ToList();
                var children = residents.Count(c => c.IsChild);
                GUILayout.Label($"Résidents: {adults.Count}/{def.MaxAdultResidents} adulte(s), {children}/{def.MaxChildResidents} enfant(s)");
                foreach (var adult in adults)
                    GUILayout.Label($"  {adult.Name} ({adult.Gender})");

                if (adults.Count == 2 && _controller.HousingCohabitationsByBuildingId.TryGetValue(building.Id, out var cohabitation))
                {
                    GUILayout.Label($"Cohabitation: {cohabitation.ContinuousDuration:0.0}s (1 an = {_secondsPerGameYear:0}s)");
                }
            }

            if (building.IsStartingShelter)
                DrawSurvivalModuleInventory();

            if (!building.IsStartingShelter)
            {
                var chantierNonCommence = building.State == BuildingState.UnderConstruction && building.ConstructionProgress <= 0f;
                if (chantierNonCommence)
                {
                    if (GUILayout.Button("Annuler le chantier (remboursement intégral)"))
                        _controller.CancelSelectedConstruction();
                }
                else if (GUILayout.Button($"Recycler ({def.RecycleRefundRatio:P0} remboursé)"))
                {
                    _controller.RecycleSelectedBuilding();
                }
            }
        }

        // Inventaire consultable du Module de survie (FR-050) : dotation de ressources (Warehouse,
        // le même stock global que celui consommé par les constructions — le Module de survie n'a
        // pas de réserve isolée) et les 5 Extracteurs multifonction (FR-051/FR-052).
        private void DrawSurvivalModuleInventory()
        {
            GUILayout.Label("Inventaire du Module de survie :");
            foreach (var kvp in _controller.Warehouse.Quantities)
                GUILayout.Label($"  {_controller.ResourceDisplayName(kvp.Key)} : {kvp.Value:0.0}");

            GUILayout.Space(6);
            GUILayout.Label("Extracteurs multifonction :");
            foreach (var extractor in _controller.MultiPurposeExtractors)
            {
                var isPending = _controller.PendingMultiPurposeExtractorId == extractor.Id;
                string status;
                if (extractor.IsPlaced)
                {
                    _controller.Planet.TryGetZone(extractor.TargetX.Value, extractor.TargetY.Value, out var zone);
                    var resourceLabel = zone?.Deposit != null ? _controller.ResourceDisplayName(zone.Deposit.ResourceId) : "?";
                    status = $"en ({extractor.TargetX},{extractor.TargetY}) — {resourceLabel}";
                }
                else
                {
                    status = "dans l'inventaire";
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {status}", GUILayout.Width(220));
                if (GUILayout.Button(isPending ? "Cliquez une case…" : (extractor.IsPlaced ? "Déplacer" : "Placer")))
                {
                    if (isPending) _controller.CancelPendingMultiPurposeExtractorPlacement();
                    else _controller.BeginMultiPurposeExtractorPlacement(extractor);
                }
                GUILayout.EndHorizontal();
            }
        }

        // Bouton "Assigner un colon" de la fiche bâtiment : ouvre une liste ne montrant que les
        // colons compatibles (adultes, triés par compétence pertinente), plutôt que la liste
        // complète des colons — n'apparaît pas du tout si ce bâtiment n'a aucun poste disponible.
        private void DrawAssignColonistSection(BuildingInstance building)
        {
            var candidates = _controller.GetAssignableColonists(building);
            if (candidates.Count == 0) return;

            var isOpen = _assignListBuildingId == building.Id;
            if (GUILayout.Button(isOpen ? "Assigner un colon ▲" : "Assigner un colon ▼"))
                _assignListBuildingId = isOpen ? (System.Guid?)null : building.Id;

            if (!isOpen) return;

            foreach (var colonist in candidates)
            {
                if (GUILayout.Button($"  {colonist.Name} — {_controller.DescribeSpecialization(colonist)}"))
                {
                    _controller.TryAssignColonistToBuilding(colonist, building);
                    _assignListBuildingId = null;
                }
            }
        }

        private void DrawResearchTargetButton(string resourceId, string label)
        {
            var marker = resourceId == _controller.SelectedResearchResourceId ? " ✓" : "";
            if (GUILayout.Button(label + marker))
                _controller.SelectedResearchResourceId = resourceId;
        }

        private void DrawBuildButton(BuildingDefinition definition)
        {
            var marker = _controller.SelectedBuildToPlace == definition ? " ✓" : "";
            if (GUILayout.Button(definition.DisplayName + marker))
            {
                _controller.SelectedBuildToPlace = definition;
                _openCategory = null; // referme le panel Construction : ne bloque plus la vue pendant le placement
                _controller.LastMessage = "Déplacez la souris dans la case pour positionner, R pour pivoter (FR-054), clic pour valider.";
            }
        }

        // Pas de scrollview propre ici : DrawOpenCategoryPanel en fournit déjà une commune à
        // toutes les catégories.
        // Navigation à deux niveaux (tableau -> fiche détaillée), même principe que la catégorie
        // Construction.
        private void DrawColonists()
        {
            if (_selectedColonistId.HasValue)
            {
                var colonist = _controller.Colonists.FirstOrDefault(c => c.Id == _selectedColonistId.Value);
                if (colonist == null)
                    _selectedColonistId = null; // colon disparu entre-temps (ne devrait pas arriver en Phase 1, pas de mort) : repli sur le tableau
                else
                {
                    DrawColonistDetailSheet(colonist);
                    return;
                }
            }

            DrawColonistTable();
        }

        private void DrawColonistTable()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nom", GUILayout.Width(110));
            GUILayout.Label("Genre", GUILayout.Width(50));
            GUILayout.Label("Âge", GUILayout.Width(40));
            GUILayout.Label("Spécialisation", GUILayout.Width(120));
            GUILayout.EndHorizontal();

            foreach (var colonist in _controller.Colonists)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(colonist.Name, GUILayout.Width(110)))
                    _selectedColonistId = colonist.Id;

                GUILayout.Label(colonist.Gender == Gender.Male ? "H" : "F", GUILayout.Width(50));
                GUILayout.Label($"{colonist.AgeSeconds / _secondsPerGameYear:0}", GUILayout.Width(40));
                GUILayout.Label(_controller.DescribeSpecialization(colonist), GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }
        }

        private void DrawColonistDetailSheet(Colonist colonist)
        {
            if (GUILayout.Button("← Retour à la liste"))
            {
                _selectedColonistId = null;
                return;
            }

            GUILayout.Label("Nom :");
            var newName = GUILayout.TextField(colonist.Name);
            if (newName != colonist.Name)
                _controller.SetColonistName(colonist, newName);

            GUILayout.Label($"Genre : {colonist.Gender}");
            GUILayout.Label($"Âge : {colonist.AgeSeconds / _secondsPerGameYear:0} ans{(colonist.IsChild ? " (enfant)" : "")}");
            GUILayout.Label($"Ethnie : {colonist.EthnicityId}");
            GUILayout.Label($"Santé : {colonist.Health:P0}");

            GUILayout.Space(6);
            GUILayout.Label("Compétences :");
            if (colonist.Skills.Count == 0)
                GUILayout.Label("  (aucune)");
            foreach (var skill in colonist.Skills.Values)
                GUILayout.Label($"  {skill.JobId} : {skill.Value:P0} ({skill.Source})");

            // Formation universitaire (US8) pas encore implémentée : placeholder honnête plutôt que
            // d'inventer un système.
            GUILayout.Label("Formation : aucune");

            GUILayout.Space(6);
            GUILayout.Label("Biographie (≤300 caractères, sans effet sur le jeu) :");
            var newBiography = GUILayout.TextArea(colonist.Biography ?? string.Empty, 300, GUILayout.Height(50));
            if (newBiography != colonist.Biography)
                _controller.SetColonistBiography(colonist, newBiography);

            GUILayout.Space(6);
            GUILayout.Label("Mode d'assignation :");
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(colonist.AssignmentMode == AssignmentMode.Manual, "Manuel", "Button"))
                _controller.SetAssignmentMode(colonist, AssignmentMode.Manual);
            if (GUILayout.Toggle(colonist.AssignmentMode == AssignmentMode.Automatic, "Automatique", "Button"))
                _controller.SetAssignmentMode(colonist, AssignmentMode.Automatic);
            GUILayout.EndHorizontal();

            if (colonist.IsChild) return; // pas d'affectation au travail avant la majorité (FR-047)

            GUILayout.Space(6);
            GUILayout.Label($"Affectation actuelle : {DescribeAssignment(colonist)}");

            var isPendingThisColonist = _controller.PendingAssignmentColonistId == colonist.Id;
            if (GUILayout.Button(isPendingThisColonist ? "Cliquez un bâtiment dans la vue..." : "Assigner à un bâtiment..."))
                _controller.BeginManualAssignment(colonist);

            // Cas particulier du transport : nécessite un extracteur/une pompe déjà sélectionné(e)
            // sur la carte ET une destination déjà ciblée ("Cibler comme destination" sur un
            // entrepôt/une citerne) — ne rentre pas dans le clic unique d'"Assigner à un bâtiment".
            if (GUILayout.Button("Assigner au transport (bâtiment source sélectionné sur la carte)"))
                _controller.AssignToTransport(colonist);

            if (GUILayout.Button("Libérer (sans affectation)"))
                _controller.Unassign(colonist);
        }

        private static string DescribeAssignment(Colonist colonist)
        {
            if (colonist.CurrentAssignment == null) return "Sans affectation";
            return $"{colonist.CurrentAssignment.Type} → {colonist.CurrentAssignment.TargetId.ToString().Substring(0, 8)}";
        }
    }
}
