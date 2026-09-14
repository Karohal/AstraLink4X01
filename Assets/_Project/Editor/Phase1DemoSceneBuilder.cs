using System.IO;
using Game.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    // Outil de développement (non exécuté au runtime) : génère la scène de debug Phase1_MVP
    // (interface OnGUI texte, sans rendu) nécessaire pour dérouler manuellement les scénarios 1 à 4
    // de specs/001-planet-core-simulation/quickstart.md. Peut être relancé sans risque (réutilise
    // les assets/le contenu existants via Phase1ContentLibrary). Pour la vue de jeu visuelle, voir
    // AstraLink > Build Phase 1 Game Scene (Phase1GameSceneBuilder).
    public static class Phase1DemoSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Phase1_MVP.unity";

        [MenuItem("AstraLink/Build Phase 1 Demo Scene")]
        public static void BuildScene()
        {
            var content = Phase1ContentLibrary.EnsureContent();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.backgroundColor = Color.black;
            cameraGo.AddComponent<AudioListener>();

            var harnessGo = new GameObject("Phase1TestHarness");
            var harness = harnessGo.AddComponent<Phase1TestHarness>();
            harness.ConfigureContentForEditorTooling(
                content.ShelterDefinition, content.StorageDefinition, content.CisternDefinition, content.ExtractorDefinition, content.PumpDefinition,
                content.MaterialsResource, content.WoodResource, content.StoneResource, content.WaterResource,
                content.WoodTechnologyDefinition, content.StoneTechnologyDefinition, content.WaterTechnologyDefinition,
                content.ResearcherJobDefinition, content.StartingEthnicity,
                content.ColonistTransporterDefinition, content.TerrainSpeedCatalog);
            EditorUtility.SetDirty(harness);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("Phase1 demo scene generated at " + ScenePath);
        }
    }
}
