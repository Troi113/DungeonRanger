using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporary runtime setup for the procedural terrain movement test.
/// Builds a 32x32 room with doorways, then places the player inside it.
/// </summary>
public sealed class PrototypeBootstrap : MonoBehaviour
{
    private const string RootName = "ProceduralTerrainTest";
    private static readonly Color PlayerColor = new(0.85f, 0.66f, 0.18f);

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
        var playerMaterial = CreateMaterial("Prototype Player Material", PlayerColor);

        new PrototypeTerrainGenerator().BuildRoom(root);

        CreatePlayer(root, playerMaterial);
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
        camera.farClipPlane = 200f;

        var follow = camera.GetComponent<PrototypeCameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<PrototypeCameraFollow>();
        }

        var player = root.Find("Player_Ranger_Test");
        follow.Target = player;
        follow.Offset = new Vector3(0f, 14f, -14f);
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
