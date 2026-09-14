using System;
using System.IO;
using Game.Building;
using Game.Colonists;
using Game.Core;
using Game.Economy;
using Game.Research;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    // Outil de développement (non exécuté au runtime) : génère le contenu minimal
    // (ScriptableObjects) et la scène Phase1_MVP nécessaires pour dérouler manuellement les
    // scénarios 1 à 4 de specs/001-planet-core-simulation/quickstart.md. Peut être relancé sans
    // risque : réutilise les assets existants au lieu d'en créer des doublons.
    //
    // Contenu : bois (durable), pierre (finie) et eau (finie, pompe/citerne dédiées) — cf.
    // Phase1TestHarness pour les règles d'extraction/stockage correspondantes.
    public static class Phase1DemoSceneBuilder
    {
        private const string ContentRoot = "Assets/_Project/ScriptableObjects";
        private const string ScenePath = "Assets/_Project/Scenes/Phase1_MVP.unity";

        [MenuItem("AstraLink/Build Phase 1 Demo Scene")]
        public static void BuildScene()
        {
            var materials = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Materials.asset", so =>
            {
                so.FindProperty("_id").stringValue = "materials";
                so.FindProperty("_displayName").stringValue = "Matériaux";
                so.FindProperty("_isRaw").boolValue = true;
            });

            var wood = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Wood.asset", so =>
            {
                so.FindProperty("_id").stringValue = "wood";
                so.FindProperty("_displayName").stringValue = "Bois";
                so.FindProperty("_isRaw").boolValue = true;
            });

            var stone = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Stone.asset", so =>
            {
                so.FindProperty("_id").stringValue = "stone";
                so.FindProperty("_displayName").stringValue = "Pierre";
                so.FindProperty("_isRaw").boolValue = true;
            });

            var water = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Water.asset", so =>
            {
                so.FindProperty("_id").stringValue = "water";
                so.FindProperty("_displayName").stringValue = "Eau";
                so.FindProperty("_isRaw").boolValue = true;
            });

            var researcherJob = CreateOrLoad<JobDefinition>($"{ContentRoot}/Jobs/Researcher.asset", so =>
            {
                so.FindProperty("_id").stringValue = JobDefinition.ResearcherJobId;
                so.FindProperty("_displayName").stringValue = "Chercheur";
            });

            var woodTechnology = CreateOrLoad<TechnologyDefinition>($"{ContentRoot}/Technologies/WoodHarvesting.asset", so =>
            {
                so.FindProperty("_id").stringValue = "wood-harvesting";
                so.FindProperty("_targetResourceId").stringValue = "wood";
                so.FindProperty("_progressRequired").floatValue = 15f;
            });

            var stoneTechnology = CreateOrLoad<TechnologyDefinition>($"{ContentRoot}/Technologies/StoneExtraction.asset", so =>
            {
                so.FindProperty("_id").stringValue = "stone-extraction";
                so.FindProperty("_targetResourceId").stringValue = "stone";
                so.FindProperty("_progressRequired").floatValue = 20f;
            });

            var waterTechnology = CreateOrLoad<TechnologyDefinition>($"{ContentRoot}/Technologies/WaterPumping.asset", so =>
            {
                so.FindProperty("_id").stringValue = "water-pumping";
                so.FindProperty("_targetResourceId").stringValue = "water";
                so.FindProperty("_progressRequired").floatValue = 20f;
            });

            var ethnicity = CreateOrLoad<EthnicityDefinition>($"{ContentRoot}/Identity/HumanEthnicity.asset", so =>
            {
                so.FindProperty("_id").stringValue = "human";
                SetStringArray(so.FindProperty("_maleFirstNames"), new[] { "Alex", "Milo", "Noah", "Theo" });
                SetStringArray(so.FindProperty("_femaleFirstNames"), new[] { "Aria", "Nova", "Lina", "Yuki" });
                SetStringArray(so.FindProperty("_lastNames"), new[] { "Voss", "Kade", "Ren", "Solis" });
            });

            var shelter = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/StartingShelter.asset", so =>
            {
                so.FindProperty("_id").stringValue = "starting-shelter";
                so.FindProperty("_displayName").stringValue = "Abri de secours";
                so.FindProperty("_constructionDuration").floatValue = 0f;
                so.FindProperty("_fogRadius").intValue = 0;
            });

            var storage = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Storage.asset", so =>
            {
                so.FindProperty("_id").stringValue = "storage";
                so.FindProperty("_displayName").stringValue = "Entrepôt";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                SetCost(so, ("materials", 20f));
            });

            var cistern = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Cistern.asset", so =>
            {
                so.FindProperty("_id").stringValue = "cistern";
                so.FindProperty("_displayName").stringValue = "Citerne";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                SetCost(so, ("materials", 20f));
            });

            var extractor = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Extractor.asset", so =>
            {
                so.FindProperty("_id").stringValue = "extractor";
                so.FindProperty("_displayName").stringValue = "Extracteur";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                SetCost(so, ("materials", 40f));
            });

            var pump = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Pump.asset", so =>
            {
                so.FindProperty("_id").stringValue = "pump";
                so.FindProperty("_displayName").stringValue = "Pompe";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                so.FindProperty("_canBuildOnWater").boolValue = true;
                SetCost(so, ("materials", 40f));
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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
                shelter, storage, cistern, extractor, pump,
                materials, wood, stone, water,
                woodTechnology, stoneTechnology, waterTechnology,
                researcherJob, ethnicity);
            EditorUtility.SetDirty(harness);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("Phase1 demo scene generated at " + ScenePath);
        }

        private static T CreateOrLoad<T>(string path, Action<SerializedObject> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<T>();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(existing, path);
            }

            var so = new SerializedObject(existing);
            configure(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void SetStringArray(SerializedProperty arrayProp, string[] values)
        {
            arrayProp.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                arrayProp.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetCost(SerializedObject so, params (string resourceId, float quantity)[] costs)
        {
            var arrayProp = so.FindProperty("_cost");
            arrayProp.arraySize = costs.Length;
            for (var i = 0; i < costs.Length; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("ResourceId").stringValue = costs[i].resourceId;
                element.FindPropertyRelative("Quantity").floatValue = costs[i].quantity;
            }
        }
    }
}
