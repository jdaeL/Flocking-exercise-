using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates the isolated Smart Enemies adaptation scene without replacing existing scenes.</summary>
public static class SmartEnemySceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SmartEnemiesDemo.unity";

    [MenuItem("Tools/Lab08/Build Smart Enemies Scene")]
    public static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            const string message = ScenePath + " already exists. Open it or rename it manually before building another copy.";
            if (Application.isBatchMode) Debug.LogWarning(message);
            else EditorUtility.DisplayDialog("Smart Enemies scene already exists", message, "OK");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateEnvironment();
        PlayerTarget player = CreatePlayer();
        SmartEnemyFlock flock = CreateEnemyGroup(player);
        CreateObstacles();
        CreateLighting();
        CreateCamera(player, flock.transform);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();

        if (Application.isBatchMode) Debug.Log("Saved " + ScenePath);
        else EditorUtility.DisplayDialog("Smart Enemies scene created", "Saved " + ScenePath, "OK");
    }

    private static void CreateEnvironment()
    {
        GameObject environment = new GameObject("Environment");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Arena Ground";
        ground.transform.SetParent(environment.transform);
        ground.transform.position = new Vector3(0f, -0.1f, 0f);
        ground.transform.localScale = new Vector3(6f, 1f, 6f);

        // The player controller clamps movement to a 25-unit radius on this 60-unit plane.
    }

    private static PlayerTarget CreatePlayer()
    {
        GameObject playerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObject.name = "Player";
        playerObject.transform.position = new Vector3(0f, 1f, 0f);
        playerObject.transform.localScale = new Vector3(0.95f, 1f, 0.95f);
        return playerObject.AddComponent<PlayerTarget>();
    }

    private static SmartEnemyFlock CreateEnemyGroup(PlayerTarget player)
    {
        GameObject group = new GameObject("Smart Enemy Flock");
        group.transform.position = new Vector3(0f, 0f, 13f);
        SmartEnemyFlock flock = group.AddComponent<SmartEnemyFlock>();

        SerializedObject settings = new SerializedObject(flock);
        settings.FindProperty("enemyCount").intValue = 18;
        settings.FindProperty("player").objectReferenceValue = player;
        settings.FindProperty("spawnRadius").vector2Value = new Vector2(3f, 3f);
        settings.FindProperty("movementSpeed").floatValue = 4f;
        settings.FindProperty("steeringResponsiveness").floatValue = 3f;
        settings.FindProperty("neighborRadius").floatValue = 7f;
        settings.FindProperty("minimumSeparationDistance").floatValue = 1.8f;
        settings.FindProperty("playerKeepDistance").floatValue = 2.4f;
        settings.FindProperty("cohesionWeight").floatValue = 0.85f;
        settings.FindProperty("separationWeight").floatValue = 2.4f;
        settings.FindProperty("alignmentWeight").floatValue = 1f;
        settings.FindProperty("pursuitWeight").floatValue = 2f;
        settings.FindProperty("obstacleAvoidanceWeight").floatValue = 3f;
        settings.FindProperty("obstacleDetectionDistance").floatValue = 3.5f;
        settings.FindProperty("obstacleProbeRadius").floatValue = 0.4f;
        settings.FindProperty("obstacleMask").intValue = 1 << 9;
        settings.FindProperty("collisionRecoveryStrength").floatValue = 4f;
        settings.FindProperty("collisionResponseDuration").floatValue = 0.65f;
        settings.FindProperty("captureRadius").floatValue = 3f;
        settings.FindProperty("captureCooldown").floatValue = 4f;
        settings.FindProperty("captureDuration").floatValue = 2.5f;
        settings.FindProperty("captureRearmDistance").floatValue = 5f;
        settings.FindProperty("captureColor").colorValue = new Color(1f, 0.12f, 0.06f);
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(flock);
        return flock;
    }

    private static void CreateObstacles()
    {
        GameObject root = new GameObject("Obstacles");
        Vector3[] positions =
        {
            new Vector3(-3f, 1.5f, 7f), new Vector3(3.5f, 1.5f, 10f),
            new Vector3(-6f, 1.5f, 14f), new Vector3(7f, 1.5f, 17f)
        };
        Vector3[] scales =
        {
            new Vector3(1.3f, 3f, 1.3f), new Vector3(1.6f, 3f, 1.6f),
            new Vector3(1.5f, 3f, 1.5f), new Vector3(1.4f, 3f, 1.4f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Obstacle " + (i + 1);
            obstacle.layer = 9;
            obstacle.transform.SetParent(root.transform);
            obstacle.transform.position = positions[i];
            obstacle.transform.localScale = scales[i];
        }
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.7f);
    }

    private static void CreateCamera(PlayerTarget player, Transform flock)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 18f, -22f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1f, 6f) - cameraObject.transform.position);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;
        cameraObject.AddComponent<SmartEnemyCameraController>().Configure(player, flock);
    }
}
