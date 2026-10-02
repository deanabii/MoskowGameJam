#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MoskowGameJam.Interaction;
using MoskowGameJam.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace MoskowGameJam.Editor
{
    public static class SetupGrabbableDemo
    {
        [MenuItem("Tools/MoskowGameJam/Setup Grabbable Demo & Additive Loader")]
        public static void CreateDemoSetup()
        {
            EnsureHeldObjectLayerExists();

            // 1. Ensure Directories exist
            if (!Directory.Exists("Assets/Settings"))
            {
                Directory.CreateDirectory("Assets/Settings");
            }
            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
            }

            AssetDatabase.Refresh();

            // 2. Create or Load SceneLoadConfig Asset
            string configPath = "Assets/Settings/DefaultSceneLoadConfig.asset";
            SceneLoadConfig config = AssetDatabase.LoadAssetAtPath<SceneLoadConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SceneLoadConfig>();
                config.scenesToLoad = new List<string> { "GrabbableInteractionScene" };
                config.activeSceneName = "GrabbableInteractionScene";
                config.loadAsynchronously = true;

                AssetDatabase.CreateAsset(config, configPath);
                AssetDatabase.SaveAssets();
            }

            // 3. Create GrabbableInteractionScene
            string interactionScenePath = "Assets/Scenes/GrabbableInteractionScene.unity";
            Scene interactionScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Setup Camera & Player Interaction
            Camera mainCam = Camera.main;
            GameObject playerObj = new GameObject("PlayerManager");
            playerObj.tag = "Player";
            if (mainCam != null)
            {
                mainCam.transform.SetParent(playerObj.transform);
                mainCam.transform.localPosition = new Vector3(0, 1.6f, 0);
            }

            // Create UI Prompt Canvas
            GameObject canvasObj = new GameObject("InteractionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject promptPanel = new GameObject("UIPromptPanel", typeof(RectTransform), typeof(Image));
            promptPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = promptPanel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(200, 50);
            panelRect.anchoredPosition = new Vector2(0, -150);
            Image panelImg = promptPanel.GetComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0.7f);

            GameObject textObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(promptPanel.transform, false);
            TextMeshProUGUI promptText = textObj.GetComponent<TextMeshProUGUI>();
            promptText.text = "[E] Ambil";
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
            promptText.fontSize = 20;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // Create Interaction Manager Component
            GameObject interactionManager = new GameObject("PlayerGrabbableInteraction");
            PlayerGrabbableInteraction interaction = interactionManager.AddComponent<PlayerGrabbableInteraction>();
            
            // Assign fields via SerializedObject
            SerializedObject interactionSO = new SerializedObject(interaction);
            interactionSO.FindProperty("playerCamera").objectReferenceValue = mainCam;
            interactionSO.FindProperty("playerTransform").objectReferenceValue = playerObj.transform;
            interactionSO.FindProperty("uiPromptPanel").objectReferenceValue = promptPanel;
            interactionSO.FindProperty("uiPromptImage").objectReferenceValue = panelImg;
            interactionSO.FindProperty("uiPromptText").objectReferenceValue = promptText;
            interactionSO.ApplyModifiedProperties();
            
            interaction.EnsureGrabHoldTransform();

            // Create Grabbable Demo Objects
            GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeObj.name = "GrabbableCube";
            cubeObj.transform.position = new Vector3(0, 0.5f, 3.0f);
            cubeObj.AddComponent<Rigidbody>();
            cubeObj.AddComponent<Grabbable>();

            GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObj.name = "GrabbableSphere";
            sphereObj.transform.position = new Vector3(1.5f, 0.5f, 3.0f);
            sphereObj.AddComponent<Rigidbody>();
            sphereObj.AddComponent<Grabbable>();

            EditorSceneManager.SaveScene(interactionScene, interactionScenePath);

            // 4. Create Main Bootstrap Scene
            string mainScenePath = "Assets/Scenes/MainBootstrapScene.unity";
            Scene mainScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject loaderObj = new GameObject("[SceneLoader]");
            AdditiveSceneLoader loader = loaderObj.AddComponent<AdditiveSceneLoader>();
            SerializedObject loaderSO = new SerializedObject(loader);
            loaderSO.FindProperty("sceneConfig").objectReferenceValue = config;
            loaderSO.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(mainScene, mainScenePath);

            // 5. Update Build Settings
            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(mainScenePath, true),
                new EditorBuildSettingsScene(interactionScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>[SetupGrabbableDemo] Scene Utama (MainBootstrapScene) dan Scene Interaksi (GrabbableInteractionScene) berhasil dibuat & dikonfigurasi!</color>");
        }

        private static void EnsureHeldObjectLayerExists()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            if (layersProp == null || !layersProp.isArray) return;

            string targetLayerName = "HeldObject";

            for (int i = 0; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerElem = layersProp.GetArrayElementAtIndex(i);
                if (layerElem.stringValue == targetLayerName)
                {
                    return; // Layer already exists
                }
            }

            for (int i = 6; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerElem = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerElem.stringValue))
                {
                    layerElem.stringValue = targetLayerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[SetupGrabbableDemo] Layer '{targetLayerName}' berhasil ditambahkan pada index {i}.");
                    return;
                }
            }
        }
    }
}
#endif
