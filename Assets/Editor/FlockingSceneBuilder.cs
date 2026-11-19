using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds the lab demonstration scene once, without overwriting an existing scene.</summary>
public static class FlockingSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FlockingDemo.unity";
    private const string TerrainDataPath = "Assets/Scenes/FlockingDemo_Terrain.asset";
    private const string FishPrefabPath = "Assets/Prefabs/Fish.prefab";

    [MenuItem("Tools/Lab08/Build Flocking Scene")]
    public static void BuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            string message = ScenePath + " already exists. Move or rename it manually if you want to build a new copy.";
            if (Application.isBatchMode) Debug.LogWarning(message);
            else EditorUtility.DisplayDialog("Flocking scene already exists", message, "OK");
            return;
        }

        FlockUnit fish = AssetDatabase.LoadAssetAtPath<FlockUnit>(FishPrefabPath);
        if (fish == null)
        {
            if (Application.isBatchMode) Debug.LogError("Could not load " + FishPrefabPath);
            else EditorUtility.DisplayDialog("Fish prefab missing", "Could not load " + FishPrefabPath, "OK");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateTerrain();
        CreateFlock(fish);
        CreateObstacles();
        CreateLighting();
        CreateCamera();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        if (Application.isBatchMode) Debug.Log("Saved " + ScenePath);
        else EditorUtility.DisplayDialog("Flocking scene created", "Saved " + ScenePath, "OK");
    }

    private static void CreateTerrain()
    {
        TerrainData data = new TerrainData();
        data.heightmapResolution = 129;
        data.size = new Vector3(60f, 5f, 60f);
        data.SetHeights(0, 0, new float[129, 129]);
        AssetDatabase.CreateAsset(data, TerrainDataPath);
        GameObject terrain = Terrain.CreateTerrainGameObject(data);
        terrain.name = "Terrain";
        terrain.transform.position = new Vector3(-30f, -1f, -30f);
    }

    private static void CreateFlock(FlockUnit fish)
    {
        GameObject root = new GameObject("Flock");
        root.transform.position = new Vector3(0f, 8f, 0f);
        Flock flock = root.AddComponent<Flock>();
        SerializedObject settings = new SerializedObject(flock);
        settings.FindProperty("flockUnitPrefab").objectReferenceValue = fish;
        settings.FindProperty("flockSize").intValue = 100;
        settings.FindProperty("spawnBounds").vector3Value = new Vector3(10f, 3f, 10f);
        settings.FindProperty("minSpeed").floatValue = 3f;
        settings.FindProperty("maxSpeed").floatValue = 5f;
        settings.FindProperty("steeringResponsiveness").floatValue = 2.5f;
        settings.FindProperty("fieldOfView").floatValue = 270f;
        settings.FindProperty("separationWeight").floatValue = 2.2f;
        settings.FindProperty("alignmentWeight").floatValue = 1f;
        settings.FindProperty("cohesionWeight").floatValue = 0.8f;
        settings.FindProperty("boundaryWeight").floatValue = 1.5f;
        settings.FindProperty("obstacleWeight").floatValue = 3f;
        settings.FindProperty("neighborRadius").floatValue = 6f;
        settings.FindProperty("minimumSeparationDistance").floatValue = 1.5f;
        settings.FindProperty("boundaryRadius").floatValue = 22f;
        settings.FindProperty("obstacleDetectionDistance").floatValue = 5f;
        settings.FindProperty("obstacleProbeRadius").floatValue = 0.45f;
        settings.FindProperty("obstacleMask").intValue = 1 << 8;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(flock);
    }

    private static void CreateObstacles()
    {
        GameObject root = new GameObject("Obstacles");
        Vector3[] positions = { new Vector3(0f, 5f, 9f), new Vector3(-8f, 4f, -5f),
            new Vector3(8f, 4f, -7f), new Vector3(0f, 3f, -13f) };
        Vector3[] scales = { new Vector3(2f, 10f, 2f), new Vector3(3f, 8f, 3f),
            new Vector3(2.5f, 8f, 2.5f), new Vector3(4f, 6f, 4f) };
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obstacle.name = "Obstacle " + (i + 1);
            obstacle.layer = 8;
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
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.62f, 0.7f);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener),
            typeof(UnityTemplateProjects.SimpleCameraController));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 27f, -40f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 5f, 0f) - cameraObject.transform.position);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 55f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 250f;
    }
}
