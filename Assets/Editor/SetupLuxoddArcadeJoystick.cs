#if UNITY_EDITOR
using Luxodd.Game.Scripts.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// User-run, idempotent scene setup for Luxodd arcade joystick controls.
///
/// Run: RealBuca -> Setup Luxodd Arcade Joystick
///
/// The command creates one ArcadeJoystickInput object, assigns the Luxodd input
/// configuration, connects it to the puck, and saves the Game scene. It never
/// enters Play Mode and it is safe to run again.
/// </summary>
public static class SetupLuxoddArcadeJoystick
{
    const string MenuPath = "RealBuca/Setup Luxodd Arcade Joystick";
    const string GameScenePath = "Assets/Scenes/Game.unity";
    const string ConfigPath = "Assets/Luxodd.Game/Editor/DefaultAssets/ArcadeInputConfigAsset.asset";
    const string InputObjectName = "ArcadeJoystickInput";

    [MenuItem(MenuPath)]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Stop Play Mode",
                "Exit Play Mode, then run the joystick setup command again.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        PuckController puck = Object.FindFirstObjectByType<PuckController>(FindObjectsInactive.Include);
        if (puck == null)
        {
            EditorUtility.DisplayDialog("Puck not found",
                "No PuckController exists in Assets/Scenes/Game.unity. Nothing was changed.", "OK");
            return;
        }

        ArcadeInputConfigAsset config = AssetDatabase.LoadAssetAtPath<ArcadeInputConfigAsset>(ConfigPath);
        if (config == null)
        {
            EditorUtility.DisplayDialog("Luxodd config not found",
                $"Could not find {ConfigPath}. Make sure the Luxodd Unity Plugin is installed.", "OK");
            return;
        }

        // Configure the plugin for both the real cabinet and ordinary gamepads.
        config.HorizontalAxisName = "Horizontal";
        config.VerticalAxisName = "Vertical";
        config.DeadZone = 0.15f;
        config.InvertX = false;
        config.InvertY = false;
        EditorUtility.SetDirty(config);

        BucaArcadeControlAdapter adapter = Object.FindFirstObjectByType<BucaArcadeControlAdapter>(FindObjectsInactive.Include);
        if (adapter == null)
        {
            GameObject inputObject = GameObject.Find(InputObjectName);
            if (inputObject == null)
            {
                inputObject = new GameObject(InputObjectName);
                Undo.RegisterCreatedObjectUndo(inputObject, "Create Luxodd arcade joystick input");
                SceneManager.MoveGameObjectToScene(inputObject, scene);
            }

            adapter = inputObject.GetComponent<BucaArcadeControlAdapter>();
            if (adapter == null)
                adapter = Undo.AddComponent<BucaArcadeControlAdapter>(inputObject);
        }

        SerializedObject adapterData = new SerializedObject(adapter);
        adapterData.FindProperty("inputConfig").objectReferenceValue = config;
        adapterData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(adapter);

        SerializedObject puckData = new SerializedObject(puck);
        puckData.FindProperty("_arcadeInput").objectReferenceValue = adapter;
        puckData.FindProperty("arcadeDeadzone").floatValue = 0.2f;
        puckData.FindProperty("arcadeAimRotationSpeed").floatValue = 100f;
        puckData.FindProperty("arcadePreviewRefreshRate").floatValue = 30f;
        puckData.FindProperty("chargeTimeToMax").floatValue = 2f;
        puckData.FindProperty("aimGuideMinFraction").floatValue = 0.35f;
        puckData.FindProperty("minChargeToFire").floatValue = 0.05f;
        puckData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(puck);

        // Rebuild the visible control legend as part of the same user-run setup,
        // so existing scenes are migrated from the old Red icon to Black.
        BucaSetupHelper helper = Object.FindFirstObjectByType<BucaSetupHelper>(FindObjectsInactive.Include);
        if (helper != null)
        {
            helper.controlHints = new[]
            {
                new BucaSetupHelper.HintItemConfig
                {
                    icon = BucaSetupHelper.ControlIcon.JoystickStick,
                    label = "ROTATE AIM"
                },
                new BucaSetupHelper.HintItemConfig
                {
                    icon = BucaSetupHelper.ControlIcon.BlackButton,
                    label = "BLACK: CHARGE / FIRE"
                }
            };
            helper.SpawnControlHintBar();
            EditorUtility.SetDirty(helper);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = adapter.gameObject;
        EditorGUIUtility.PingObject(adapter.gameObject);

        EditorUtility.DisplayDialog("Luxodd joystick created",
            "Created/configured ArcadeJoystickInput and connected it to the puck.\n\n" +
            "Controls:\n" +
            "• Joystick Left / Right: rotate aim through 360°\n" +
            "• Hold Black: build shot power\n" +
            "• Release Black: shoot\n" +
            "• White: cancel\n\n" +
            "Orange remains reserved for the Luxodd system overlay.", "OK");
    }
}
#endif
