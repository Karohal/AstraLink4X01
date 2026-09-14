using System.IO;
using Game.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    // Outil de développement (non exécuté au runtime) : génère la scène de jeu visuelle
    // Phase1_Game (formes primitives, couleurs unies — aucun asset externe) qui réutilise le même
    // contenu que Phase1_MVP (Phase1ContentLibrary) et les mêmes services Game.<Module> déjà
    // testés, via Phase1GameController/Phase1GameView. Peut être relancé sans risque. La scène de
    // debug Phase1_MVP reste inchangée et disponible pour des tests rapides sans rendu.
    public static class Phase1GameSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Phase1_Game.unity";
        private const string PumpModelPath = "Assets/_Project/Art/pompe_a_eau.fbx";

        [MenuItem("AstraLink/Build Phase 1 Game Scene")]
        public static void BuildScene()
        {
            var content = Phase1ContentLibrary.EnsureContent();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Vue god mode plongeante/isométrique au-dessus du centre de la grille (largeur/hauteur
            // par défaut de Phase1GameView : 100x100, abri de départ au centre), dans l'esprit
            // SimCity — pas de personnage incarné (FR-002).
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = false;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 800f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<Game.Core.GodModeCameraController>();
            cameraGo.transform.position = new Vector3(50f, 55f, 15f);
            cameraGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            // Optionnel : si le modèle n'est pas (encore) présent, la Pompe retombe sur le cylindre
            // primitif comme avant (cf. Phase1GameView.SyncBuildingViews).
            var pumpModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PumpModelPath);
            if (pumpModelPrefab == null)
                Debug.LogWarning($"Modèle de pompe introuvable à {PumpModelPath} — la Pompe utilisera le cylindre primitif.");

            var viewGo = new GameObject("Phase1GameView");
            var view = viewGo.AddComponent<Phase1GameView>();
            view.ConfigureContentForEditorTooling(content, pumpModelPrefab);
            EditorUtility.SetDirty(view);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("Phase1 game scene generated at " + ScenePath);
        }
    }
}
