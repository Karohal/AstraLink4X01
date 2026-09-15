using System;
using System.IO;
using Game.Building;
using Game.Colonists;
using Game.Core;
using Game.Economy;
using Game.Logistics;
using Game.Procedural;
using Game.Research;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    // Création/chargement idempotent du contenu (ScriptableObjects) partagé par les scènes Phase 1
    // (debug Phase1_MVP et jeu Phase1_Game) : garantit que les deux scènes référencent exactement
    // les mêmes assets de contenu plutôt que des doublons divergents. Peut être relancé sans risque
    // (réutilise les assets existants).
    //
    // Contenu : bois (durable), pierre (finie) et eau (finie, pompe/citerne dédiées).
    public static class Phase1ContentLibrary
    {
        private const string ContentRoot = "Assets/_Project/ScriptableObjects";

        public static Phase1GameContent EnsureContent()
        {
            var content = new Phase1GameContent();

            content.MaterialsResource = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Materials.asset", so =>
            {
                so.FindProperty("_id").stringValue = "materials";
                so.FindProperty("_displayName").stringValue = "Matériaux";
                so.FindProperty("_isRaw").boolValue = true;
                so.FindProperty("_densityKgPerCubicMeter").floatValue = 1000f; // ressource abstraite, non transportée
            });

            content.WoodResource = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Wood.asset", so =>
            {
                so.FindProperty("_id").stringValue = "wood";
                so.FindProperty("_displayName").stringValue = "Bois";
                so.FindProperty("_isRaw").boolValue = true;
                so.FindProperty("_densityKgPerCubicMeter").floatValue = 650f; // densité réelle du bois
            });

            content.StoneResource = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Stone.asset", so =>
            {
                so.FindProperty("_id").stringValue = "stone";
                so.FindProperty("_displayName").stringValue = "Pierre";
                so.FindProperty("_isRaw").boolValue = true;
                so.FindProperty("_densityKgPerCubicMeter").floatValue = 2600f; // densité réelle de la pierre
            });

            content.WaterResource = CreateOrLoad<ResourceDefinition>($"{ContentRoot}/Resources/Water.asset", so =>
            {
                so.FindProperty("_id").stringValue = "water";
                so.FindProperty("_displayName").stringValue = "Eau";
                so.FindProperty("_isRaw").boolValue = true;
                so.FindProperty("_densityKgPerCubicMeter").floatValue = 1000f; // densité réelle de l'eau
            });

            content.ResearcherJobDefinition = CreateOrLoad<JobDefinition>($"{ContentRoot}/Jobs/Researcher.asset", so =>
            {
                so.FindProperty("_id").stringValue = JobDefinition.ResearcherJobId;
                so.FindProperty("_displayName").stringValue = "Chercheur";
            });

            content.WoodTechnologyDefinition = CreateOrLoad<TechnologyDefinition>($"{ContentRoot}/Technologies/WoodHarvesting.asset", so =>
            {
                so.FindProperty("_id").stringValue = "wood-harvesting";
                so.FindProperty("_targetResourceId").stringValue = "wood";
                so.FindProperty("_progressRequired").floatValue = 15f;
            });

            content.StoneTechnologyDefinition = CreateOrLoad<TechnologyDefinition>($"{ContentRoot}/Technologies/StoneExtraction.asset", so =>
            {
                so.FindProperty("_id").stringValue = "stone-extraction";
                so.FindProperty("_targetResourceId").stringValue = "stone";
                so.FindProperty("_progressRequired").floatValue = 20f;
            });

            content.WaterTechnologyDefinition = CreateOrLoad<TechnologyDefinition>($"{ContentRoot}/Technologies/WaterPumping.asset", so =>
            {
                so.FindProperty("_id").stringValue = "water-pumping";
                so.FindProperty("_targetResourceId").stringValue = "water";
                so.FindProperty("_progressRequired").floatValue = 20f;
            });

            content.StartingEthnicity = CreateOrLoad<EthnicityDefinition>($"{ContentRoot}/Identity/HumanEthnicity.asset", so =>
            {
                so.FindProperty("_id").stringValue = "human";
                SetStringArray(so.FindProperty("_maleFirstNames"), new[] { "Alex", "Milo", "Noah", "Theo" });
                SetStringArray(so.FindProperty("_femaleFirstNames"), new[] { "Aria", "Nova", "Lina", "Yuki" });
                SetStringArray(so.FindProperty("_lastNames"), new[] { "Voss", "Kade", "Ren", "Solis" });
            });

            content.ShelterDefinition = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/StartingShelter.asset", so =>
            {
                so.FindProperty("_id").stringValue = "starting-shelter";
                so.FindProperty("_displayName").stringValue = "Module de survie";
                so.FindProperty("_constructionDuration").floatValue = 0f;
                so.FindProperty("_fogRadius").intValue = 0;
            });

            content.StorageDefinition = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Storage.asset", so =>
            {
                so.FindProperty("_id").stringValue = "storage";
                so.FindProperty("_displayName").stringValue = "Entrepôt";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                SetCost(so, ("materials", 20f));
            });

            content.CisternDefinition = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Cistern.asset", so =>
            {
                so.FindProperty("_id").stringValue = "cistern";
                so.FindProperty("_displayName").stringValue = "Citerne";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                SetCost(so, ("materials", 20f));
            });

            content.ExtractorDefinition = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Extractor.asset", so =>
            {
                so.FindProperty("_id").stringValue = "extractor";
                so.FindProperty("_displayName").stringValue = "Extracteur";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                SetCost(so, ("materials", 40f));
            });

            content.PumpDefinition = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/Pump.asset", so =>
            {
                so.FindProperty("_id").stringValue = "pump";
                so.FindProperty("_displayName").stringValue = "Pompe";
                so.FindProperty("_constructionDuration").floatValue = 6f;
                so.FindProperty("_fogRadius").intValue = 4;
                so.FindProperty("_canBuildOnWater").boolValue = true;
                SetCost(so, ("materials", 40f));
            });

            content.HousingDefinition = CreateOrLoad<BuildingDefinition>($"{ContentRoot}/Buildings/BasicShelter.asset", so =>
            {
                so.FindProperty("_id").stringValue = "basic-shelter";
                so.FindProperty("_displayName").stringValue = "Abri basique";
                so.FindProperty("_constructionDuration").floatValue = 8f;
                so.FindProperty("_fogRadius").intValue = 3;
                so.FindProperty("_isHousing").boolValue = true;
                so.FindProperty("_maxAdultResidents").intValue = 2;
                so.FindProperty("_maxChildResidents").intValue = 2;
                so.FindProperty("_birthAttemptIntervalSeconds").floatValue = 20f; // équilibrage provisoire
                so.FindProperty("_birthAttemptSuccessChance").floatValue = 0.08f; // taux plus faible (premier type de logement)
                SetCost(so, ("materials", 30f), ("wood", 20f));
            });

            content.ColonistTransporterDefinition = CreateOrLoad<TransporterDefinition>($"{ContentRoot}/Logistics/ColonistTransporter.asset", so =>
            {
                so.FindProperty("_id").stringValue = "colonist";
                so.FindProperty("_displayName").stringValue = "Colon";
                so.FindProperty("_massCapacityKg").floatValue = 12f;
                so.FindProperty("_volumeCapacityCubicMeters").floatValue = 0.012f; // 12 kg d'eau
                so.FindProperty("_speedTilesPerSecond").floatValue = 2f; // valeur d'équilibrage provisoire
            });

            content.TerrainSpeedCatalog = CreateOrLoad<TerrainSpeedCatalog>($"{ContentRoot}/Procedural/TerrainSpeedCatalog.asset", so =>
            {
                // Valeurs d'équilibrage provisoires (principe : vitesse variable selon le terrain
                // traversé, à affiner ultérieurement dans le catalogue de contenu).
                SetTerrainSpeedEntries(so, (TerrainType.Plains, 1f), (TerrainType.Hills, 0.7f), (TerrainType.Mountains, 0.4f), (TerrainType.Water, 0.5f));
                so.FindProperty("_defaultSpeedModifier").floatValue = 1f;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return content;
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

        private static void SetTerrainSpeedEntries(SerializedObject so, params (TerrainType terrain, float speedModifier)[] entries)
        {
            var arrayProp = so.FindProperty("_entries");
            arrayProp.arraySize = entries.Length;
            for (var i = 0; i < entries.Length; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Terrain").enumValueIndex = (int)entries[i].terrain;
                element.FindPropertyRelative("SpeedModifier").floatValue = entries[i].speedModifier;
            }
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
