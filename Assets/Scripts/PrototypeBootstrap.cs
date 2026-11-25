using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Temporary runtime setup for the procedural movement and combat tests.
/// Builds either the original room test or the full procedural LevelTest floor.
/// </summary>
public sealed class PrototypeBootstrap : MonoBehaviour
{
    private const string RootName = "ProceduralTerrainTest";
    private static readonly Color PlayerColor = new(0.85f, 0.66f, 0.18f);
    private static readonly Color SleepingEnemyColor = new(0.18f, 0.22f, 0.3f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreatePrototype()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != "SampleScene" && sceneName != "MoveCombatTest" && sceneName != "TrapsSleep" && sceneName != "LevelTest")
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        var root = new GameObject(RootName);
        var bootstrap = root.AddComponent<PrototypeBootstrap>();
        bootstrap.BuildScene(root.transform, sceneName == "LevelTest");
    }

    private void BuildScene(Transform root, bool fullLevel)
    {
        var playerMaterial = CreateMaterial("Prototype Player Material", PlayerColor);
        var enemyMaterial = CreateMaterial("Prototype Sleeping Enemy Material", SleepingEnemyColor);

        if (fullLevel)
        {
            var layout = new PrototypeLevelGenerator().Build(root);
            var player = CreatePlayer(root, playerMaterial, layout.PlayerSpawn);
            var enemies = CreateEnemies(root, enemyMaterial, layout.EnemySpawns, player, layout.WalkableCells);
            var wakeDirector = root.gameObject.AddComponent<PrototypeEnemyWakeDirector>();
            wakeDirector.Initialize(enemies, PrototypeLevelGenerator.Seed + 1000);
            var fogOfWar = root.gameObject.AddComponent<PrototypeFogOfWar>();
            fogOfWar.Initialize(player, layout.WalkableCells, root);
            var gameUI = root.gameObject.AddComponent<PrototypeGameUI>();
            gameUI.Initialize(player.gameObject);
        }
        else
        {
            new PrototypeTerrainGenerator().BuildRoom(root);
            var player = CreatePlayer(root, playerMaterial, new Vector3(0f, 1f, 0f));
            CreateEnemies(root, enemyMaterial, new[]
            {
                new Vector3(-4f, 1f, 8f),
                new Vector3(4f, 1f, 8f)
            }, null, null);
            var gameUI = root.gameObject.AddComponent<PrototypeGameUI>();
            gameUI.Initialize(player.gameObject);
        }

        SetupCamera(root, fullLevel);
        SetupLighting();
    }

    private static PrototypePlayerMovement CreatePlayer(Transform root, Material material, Vector3 position)
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_Ranger_Test";
        player.transform.SetParent(root);
        player.transform.SetPositionAndRotation(position, Quaternion.identity);
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

        var movement = player.AddComponent<PrototypePlayerMovement>();
        player.AddComponent<PrototypePlayerStats>();
        player.AddComponent<PrototypeInventory>();
        player.AddComponent<PrototypeStatusEffects>();
        player.AddComponent<PrototypeHunger>();
        player.AddComponent<PrototypePlayerInteraction>();
        player.AddComponent<PrototypeCombat>();
        return movement;
    }

    private static List<PrototypeEnemy> CreateEnemies(
        Transform root,
        Material material,
        IEnumerable<Vector3> positions,
        PrototypePlayerMovement player,
        IReadOnlyList<Vector2Int> walkableCells)
    {
        var enemies = new List<PrototypeEnemy>();
        var index = 1;
        foreach (var position in positions)
        {
            var enemy = CreateEnemy($"Enemy_Sleeping_{index:00}", root, position, material);
            if (player != null && walkableCells != null)
            {
                enemy.Initialize(player, walkableCells, PrototypeLevelGenerator.Seed + index);
            }

            enemies.Add(enemy);
            index++;
        }

        return enemies;
    }

    private static PrototypeEnemy CreateEnemy(string objectName, Transform root, Vector3 position, Material material)
    {
        var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = objectName;
        enemy.transform.SetParent(root);
        enemy.transform.SetPositionAndRotation(position, Quaternion.identity);
        enemy.transform.localScale = new Vector3(0.9f, 2f, 0.9f);
        enemy.GetComponent<Renderer>().sharedMaterial = material;
        return enemy.AddComponent<PrototypeEnemy>();
    }

    private static void SetupCamera(Transform root, bool fullLevel)
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
        follow.Offset = fullLevel
            ? new Vector3(0f, 24f, -24f)
            : new Vector3(0f, 14f, -14f);
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
