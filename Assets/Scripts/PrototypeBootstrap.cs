using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporary runtime setup for the first movement test.
/// Builds a simple 6x6 room in SampleScene without requiring scene authoring yet.
/// </summary>
public sealed class PrototypeBootstrap : MonoBehaviour
{
    private const string RootName = "DungeonMovementTest";
    private const float RoomSize = 6f;
    private const float WallThickness = 0.25f;
    private const float WallHeight = 2.5f;

    private static readonly Color FloorColor = new(0.24f, 0.28f, 0.34f);
    private static readonly Color WallColor = new(0.12f, 0.15f, 0.2f);
    private static readonly Color PlayerColor = new(0.85f, 0.66f, 0.18f);
    private static readonly Color EnemyColor = new(0.72f, 0.18f, 0.2f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePrototype()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "SampleScene" && sceneName != "MoveCombatTest")
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        var root = new GameObject(RootName);
        var bootstrap = root.AddComponent<PrototypeBootstrap>();
        bootstrap.BuildRoom(root.transform);
    }

    private void BuildRoom(Transform root)
    {
        var floorMaterial = CreateMaterial("Prototype Floor Material", FloorColor);
        var wallMaterial = CreateMaterial("Prototype Wall Material", WallColor);
        var playerMaterial = CreateMaterial("Prototype Player Material", PlayerColor);
        var enemyMaterial = CreateMaterial("Prototype Enemy Material", EnemyColor);

        CreateCube("Floor_6x6", root, new Vector3(0f, -0.1f, 0f), new Vector3(RoomSize, 0.2f, RoomSize), floorMaterial);

        var wallSpan = RoomSize + WallThickness * 2f;
        var wallY = WallHeight * 0.5f;

        CreateCube("Wall_North", root, new Vector3(0f, wallY, RoomSize * 0.5f + WallThickness * 0.5f), new Vector3(wallSpan, WallHeight, WallThickness), wallMaterial);
        CreateCube("Wall_South", root, new Vector3(0f, wallY, -RoomSize * 0.5f - WallThickness * 0.5f), new Vector3(wallSpan, WallHeight, WallThickness), wallMaterial);
        CreateCube("Wall_East", root, new Vector3(RoomSize * 0.5f + WallThickness * 0.5f, wallY, 0f), new Vector3(WallThickness, WallHeight, RoomSize), wallMaterial);
        CreateCube("Wall_West", root, new Vector3(-RoomSize * 0.5f - WallThickness * 0.5f, wallY, 0f), new Vector3(WallThickness, WallHeight, RoomSize), wallMaterial);

        CreatePlayer(root, playerMaterial);
        CreateEnemies(root, enemyMaterial);
        SetupCamera(root);
        SetupLighting();
    }

    private static void CreatePlayer(Transform root, Material material)
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Ranger_Test";
        player.transform.SetParent(root);
        player.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
        player.GetComponent<Renderer>().sharedMaterial = material;

        var capsuleCollider = player.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            Destroy(capsuleCollider);
        }

        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.45f;
        controller.center = Vector3.zero;
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.05f;
        controller.minMoveDistance = 0f;

        player.AddComponent<PrototypePlayerMovement>();
        player.AddComponent<PrototypeCombat>();
    }

    private static void CreateEnemies(Transform root, Material material)
    {
        CreateEnemy("Enemy_NorthWest", root, new Vector3(-1.8f, 0.5f, 1.7f), material);
        CreateEnemy("Enemy_NorthEast", root, new Vector3(1.7f, 0.5f, 1.5f), material);
        CreateEnemy("Enemy_South", root, new Vector3(0f, 0.5f, -2f), material);
    }

    private static void CreateEnemy(string objectName, Transform root, Vector3 position, Material material)
    {
        var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = objectName;
        enemy.transform.SetParent(root);
        enemy.transform.SetPositionAndRotation(position, Quaternion.identity);
        enemy.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        enemy.GetComponent<Renderer>().sharedMaterial = material;
        enemy.AddComponent<PrototypeEnemy>();
    }

    private static void SetupCamera(Transform root)
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;

        var follow = camera.GetComponent<PrototypeCameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<PrototypeCameraFollow>();
        }

        var player = root.Find("Player_Ranger_Test");
        follow.Target = player;
        follow.Offset = new Vector3(0f, 9f, -8f);
    }

    private static void SetupLighting()
    {
        var light = FindAnyObjectByType<Light>();
        if (light == null)
        {
            var lightObject = new GameObject("Directional Light");
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static GameObject CreateCube(string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent);
        cube.transform.SetPositionAndRotation(position, Quaternion.identity);
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static Material CreateMaterial(string materialName, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = materialName,
            color = color
        };
        material.SetFloat("_Smoothness", 0f);
        return material;
    }
}
