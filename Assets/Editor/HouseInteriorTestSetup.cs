using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ExiledAlvaston.Data;
using ExiledAlvaston.World;
using ExiledAlvaston.Vibe;

/// <summary>
/// Builds a small enclosed interior room, wires an entry door on the "house1" object already
/// in the scene, and an exit door back to Home_Alvaston inside the room — proving out the
/// "instanced interior" pattern (ChunkTransitionDoor) for one house as a test.
/// </summary>
public static class HouseInteriorTestSetup
{
    private const string InteriorPrefabPath = "Assets/Prefabs/Chunks/House1_Interior_Prefab.prefab";
    private const string InteriorDataPath = "Assets/Data/Chunks/House1_Interior_Data.asset";
    private const string FloorMatPath = "Assets/Materials/InteriorFloor.mat";
    private const string WallMatPath = "Assets/Materials/InteriorWall.mat";
    private const string HomeDataPath = "Assets/Data/Chunks/Home_Alvaston_Data.asset";

    private const float RoomSize = 8f;
    private const float WallHeight = 3f;
    private const float DoorGap = 2.2f;

    [MenuItem("Tools/Exiled Alvaston/Setup House1 Interior Test")]
    public static void Run()
    {
        MapChunkData interiorData = BuildInteriorChunk();
        if (interiorData == null) return;

        WireEntryDoorOnHouse1(interiorData);

        Debug.Log("House1 interior test wired up. Walk into house1's front (the open side of the room, spawn is just inside it) " +
                  "to go in, and back through the opening to return to Home_Alvaston. Positions are rough first guesses — " +
                  "check in Play mode and nudge EntryDoor's Transform / TargetSpawnPosition fields if the trigger doesn't line up.");
    }

    private static MapChunkData BuildInteriorChunk()
    {
        MapChunkData homeData = AssetDatabase.LoadAssetAtPath<MapChunkData>(HomeDataPath);
        if (homeData == null)
        {
            Debug.LogError($"HouseInteriorTestSetup: couldn't load {HomeDataPath}");
            return null;
        }

        Material floorMat = GetOrCreateMaterial(FloorMatPath, EKVibe.DungeonFloor);
        Material wallMat = GetOrCreateMaterial(WallMatPath, EKVibe.DungeonWall);

        GameObject root = new GameObject("House1_Interior_Prefab");
        try
        {
            float half = RoomSize * 0.5f;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(RoomSize / 10f, 1f, RoomSize / 10f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            // CreatePrimitive(Plane) already includes a MeshCollider sized to match — walkable as-is.

            // North, East, West walls fully enclose; South is left as the doorway opening.
            BuildWall(root.transform, "Wall_North", wallMat, new Vector3(0f, WallHeight * 0.5f, half), new Vector3(RoomSize, WallHeight, 0.3f));
            BuildWall(root.transform, "Wall_East", wallMat, new Vector3(half, WallHeight * 0.5f, 0f), new Vector3(0.3f, WallHeight, RoomSize));
            BuildWall(root.transform, "Wall_West", wallMat, new Vector3(-half, WallHeight * 0.5f, 0f), new Vector3(0.3f, WallHeight, RoomSize));

            // South wall has a doorway gap in the middle — two short stub segments either side.
            float stub = (RoomSize - DoorGap) * 0.5f;
            BuildWall(root.transform, "Wall_South_A", wallMat,
                new Vector3(-(DoorGap * 0.5f + stub * 0.5f), WallHeight * 0.5f, -half), new Vector3(stub, WallHeight, 0.3f));
            BuildWall(root.transform, "Wall_South_B", wallMat,
                new Vector3((DoorGap * 0.5f + stub * 0.5f), WallHeight * 0.5f, -half), new Vector3(stub, WallHeight, 0.3f));

            GameObject exitGO = new GameObject("ExitDoor");
            exitGO.transform.SetParent(root.transform, false);
            exitGO.transform.localPosition = new Vector3(0f, 1f, -half);
            var exitCol = exitGO.AddComponent<BoxCollider>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector3(DoorGap, 2.5f, 1.5f);

            var exitDoor = exitGO.AddComponent<ChunkTransitionDoor>();
            exitDoor.TargetChunk = homeData;
            // Rough guess just outside house1's front — nudge after seeing it in Play mode.
            exitDoor.TargetSpawnPosition = new Vector3(-12.78f, 1f, 19f);
            exitDoor.Prompt = "You step back outside.";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPrefabPath) != null)
                AssetDatabase.DeleteAsset(InteriorPrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, InteriorPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(InteriorPrefabPath);

        MapChunkData interiorData = AssetDatabase.LoadAssetAtPath<MapChunkData>(InteriorDataPath);
        if (interiorData == null)
        {
            interiorData = ScriptableObject.CreateInstance<MapChunkData>();
            AssetDatabase.CreateAsset(interiorData, InteriorDataPath);
        }
        interiorData.ChunkName = "House1_Interior";
        interiorData.IsCity = false;
        interiorData.ChunkPrefab = prefabAsset;
        EditorUtility.SetDirty(interiorData);
        AssetDatabase.SaveAssets();

        return interiorData;
    }

    private static void BuildWall(Transform parent, string name, Material mat, Vector3 localPos, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = localPos;
        wall.transform.localScale = size;
        wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void WireEntryDoorOnHouse1(MapChunkData interiorData)
    {
        GameObject house1 = GameObject.Find("house1");
        if (house1 == null)
        {
            Debug.LogWarning("HouseInteriorTestSetup: couldn't find 'house1' in the open scene — entry door not placed. " +
                              "Make sure c.unity is open with house1 in it.");
            return;
        }

        Transform existing = house1.transform.Find("EntryDoor");
        GameObject entryGO;
        if (existing != null)
        {
            entryGO = existing.gameObject;
        }
        else
        {
            entryGO = new GameObject("EntryDoor");
            Undo.RegisterCreatedObjectUndo(entryGO, "Setup House1 Interior Test");
            entryGO.transform.SetParent(house1.transform, false);
        }

        // Rough placeholder in front of the house, local to house1 — nudge once you can see the door mesh.
        entryGO.transform.localPosition = new Vector3(0f, 1f, 3f);

        var col = entryGO.GetComponent<BoxCollider>();
        if (col == null) col = entryGO.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.5f, 2.5f, 1.5f);

        var door = entryGO.GetComponent<ChunkTransitionDoor>();
        if (door == null) door = entryGO.AddComponent<ChunkTransitionDoor>();
        door.TargetChunk = interiorData;
        door.TargetSpawnPosition = new Vector3(0f, 1f, -2f); // just inside the doorway gap
        door.Prompt = "You step inside.";

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
