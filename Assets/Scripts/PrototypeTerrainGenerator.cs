using UnityEngine;

/// <summary>
/// Generates the first procedural terrain test: one 32x32 room with four doorways.
/// This intentionally contains only floor, walls, and doors.
/// </summary>
public sealed class PrototypeTerrainGenerator
{
    public const float RoomSize = 32f;

    private const float WallThickness = 0.25f;
    private const float WallHeight = 2.5f;
    private const float DoorWidth = 2f;
    private const float DoorHeight = 2.4f;
    private const float DoorFrameThickness = 0.15f;
    private const float DoorFrameDepth = 0.35f;

    private static readonly Color FloorColor = new(0.24f, 0.28f, 0.34f);
    private static readonly Color WallColor = new(0.12f, 0.15f, 0.2f);
    private static readonly Color DoorColor = new(0.48f, 0.27f, 0.12f);

    public void BuildRoom(Transform root)
    {
        var floorMaterial = CreateMaterial("Prototype Floor Material", FloorColor);
        var wallMaterial = CreateMaterial("Prototype Wall Material", WallColor);
        var doorMaterial = CreateMaterial("Prototype Door Material", DoorColor);

        CreateCube(
            "Floor_32x32",
            root,
            new Vector3(0f, -0.1f, 0f),
            new Vector3(RoomSize, 0.2f, RoomSize),
            floorMaterial);

        CreateWallsWithDoorways(root, wallMaterial);
        CreateDoorFrame("Door_North", root, new Vector3(0f, 0f, RoomSize * 0.5f), true, doorMaterial);
        CreateDoorFrame("Door_South", root, new Vector3(0f, 0f, -RoomSize * 0.5f), true, doorMaterial);
        CreateDoorFrame("Door_East", root, new Vector3(RoomSize * 0.5f, 0f, 0f), false, doorMaterial);
        CreateDoorFrame("Door_West", root, new Vector3(-RoomSize * 0.5f, 0f, 0f), false, doorMaterial);

        new PrototypeTrapShowcase().Build(root);
    }

    private static void CreateWallsWithDoorways(Transform root, Material material)
    {
        var wallY = WallHeight * 0.5f;
        var segmentLength = (RoomSize - DoorWidth) * 0.5f;
        var segmentOffset = (DoorWidth + segmentLength) * 0.5f;
        var northSouthZ = RoomSize * 0.5f + WallThickness * 0.5f;
        var eastWestX = RoomSize * 0.5f + WallThickness * 0.5f;

        CreateHorizontalWallSegments("Wall_North", root, northSouthZ, wallY, segmentLength, segmentOffset, material);
        CreateHorizontalWallSegments("Wall_South", root, -northSouthZ, wallY, segmentLength, segmentOffset, material);
        CreateVerticalWallSegments("Wall_East", root, eastWestX, wallY, segmentLength, segmentOffset, material);
        CreateVerticalWallSegments("Wall_West", root, -eastWestX, wallY, segmentLength, segmentOffset, material);
    }

    private static void CreateHorizontalWallSegments(
        string wallName,
        Transform root,
        float z,
        float y,
        float segmentLength,
        float segmentOffset,
        Material material)
    {
        CreateCube(
            $"{wallName}_West",
            root,
            new Vector3(-segmentOffset, y, z),
            new Vector3(segmentLength, WallHeight, WallThickness),
            material);
        CreateCube(
            $"{wallName}_East",
            root,
            new Vector3(segmentOffset, y, z),
            new Vector3(segmentLength, WallHeight, WallThickness),
            material);
    }

    private static void CreateVerticalWallSegments(
        string wallName,
        Transform root,
        float x,
        float y,
        float segmentLength,
        float segmentOffset,
        Material material)
    {
        CreateCube(
            $"{wallName}_South",
            root,
            new Vector3(x, y, -segmentOffset),
            new Vector3(WallThickness, WallHeight, segmentLength),
            material);
        CreateCube(
            $"{wallName}_North",
            root,
            new Vector3(x, y, segmentOffset),
            new Vector3(WallThickness, WallHeight, segmentLength),
            material);
    }

    private static void CreateDoorFrame(
        string doorName,
        Transform root,
        Vector3 center,
        bool horizontal,
        Material material)
    {
        var doorRoot = new GameObject(doorName);
        doorRoot.transform.SetParent(root, false);
        doorRoot.transform.localPosition = center;

        if (horizontal)
        {
            CreateCube(
                "Frame_West",
                doorRoot.transform,
                new Vector3(-DoorWidth * 0.5f + DoorFrameThickness * 0.5f, DoorHeight * 0.5f, 0f),
                new Vector3(DoorFrameThickness, DoorHeight, DoorFrameDepth),
                material);
            CreateCube(
                "Frame_East",
                doorRoot.transform,
                new Vector3(DoorWidth * 0.5f - DoorFrameThickness * 0.5f, DoorHeight * 0.5f, 0f),
                new Vector3(DoorFrameThickness, DoorHeight, DoorFrameDepth),
                material);
            CreateCube(
                "Frame_Header",
                doorRoot.transform,
                new Vector3(0f, DoorHeight - DoorFrameThickness * 0.5f, 0f),
                new Vector3(DoorWidth, DoorFrameThickness, DoorFrameDepth),
                material);
        }
        else
        {
            CreateCube(
                "Frame_South",
                doorRoot.transform,
                new Vector3(0f, DoorHeight * 0.5f, -DoorWidth * 0.5f + DoorFrameThickness * 0.5f),
                new Vector3(DoorFrameDepth, DoorHeight, DoorFrameThickness),
                material);
            CreateCube(
                "Frame_North",
                doorRoot.transform,
                new Vector3(0f, DoorHeight * 0.5f, DoorWidth * 0.5f - DoorFrameThickness * 0.5f),
                new Vector3(DoorFrameDepth, DoorHeight, DoorFrameThickness),
                material);
            CreateCube(
                "Frame_Header",
                doorRoot.transform,
                new Vector3(0f, DoorHeight - DoorFrameThickness * 0.5f, 0f),
                new Vector3(DoorFrameDepth, DoorFrameThickness, DoorWidth),
                material);
        }
    }

    private static GameObject CreateCube(
        string objectName,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
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
