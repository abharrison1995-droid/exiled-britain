using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ExiledAlvaston.Vibe;

/// <summary>
/// Fixes the Home_Alvaston ground: deactivates the legacy Outdoor_Kingsbridge object that
/// was z-fighting with the chunk floor, swaps Ground to a solid grass-green material, and
/// adds 4 grey path strips from the center spawn out to each edge.
/// </summary>
public static class GroundAndPathSetup
{
    private const string HomePrefabPath = "Assets/Prefabs/Chunks/Home_Alvaston_Prefab.prefab";
    private const string GrassMatPath = "Assets/Materials/Grass.mat";
    private const string AsphaltMatPath = "Assets/Materials/Asphalt.mat";

    [MenuItem("Tools/Exiled Alvaston/Fix Home Ground And Add Path")]
    public static void Run()
    {
        DeactivateInScene("Outdoor_Kingsbridge");
        UpdateHomePrefab();
    }

    private static void DeactivateInScene(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            Debug.Log($"GroundAndPathSetup: no '{name}' found in the open scene — nothing to deactivate.");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        GameObject enemy = GameObject.Find("TestEnemy");
        if ((player != null && player.transform.IsChildOf(go.transform))
            || (enemy != null && enemy.transform.IsChildOf(go.transform)))
        {
            Debug.LogWarning($"GroundAndPathSetup: Player or TestEnemy is parented under '{name}' — skipped deactivating it.");
            return;
        }

        Undo.RecordObject(go, "Fix Home Ground");
        go.SetActive(false);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static void UpdateHomePrefab()
    {
        Material grass = GetOrCreateGrassMaterial();
        Material asphalt = AssetDatabase.LoadAssetAtPath<Material>(AsphaltMatPath);
        if (asphalt == null)
        {
            Debug.LogError($"GroundAndPathSetup: couldn't load {AsphaltMatPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(HomePrefabPath);
        try
        {
            Transform ground = root.transform.Find("Ground");
            if (ground == null)
            {
                Debug.LogError("GroundAndPathSetup: Home_Alvaston_Prefab has no 'Ground' child.");
                return;
            }

            var groundRenderer = ground.GetComponent<MeshRenderer>();
            if (groundRenderer != null)
                groundRenderer.sharedMaterial = grass;

            // Clear any previously-generated path pieces so re-running this is safe
            Transform oldPaths = root.transform.Find("Paths");
            if (oldPaths != null)
                Object.DestroyImmediate(oldPaths.gameObject);

            var pathsRoot = new GameObject("Paths");
            pathsRoot.transform.SetParent(root.transform, false);

            float half = EKVibe.ChunkSize * 0.5f; // 110
            float pathWidth = 6f;
            float pathLength = half; // center to edge
            float y = 0.03f; // sits just above the ground plane, no z-fighting

            BuildPathStrip(pathsRoot.transform, "Path_North", asphalt,
                center: new Vector3(0f, y, pathLength * 0.5f), size: new Vector3(pathWidth, pathLength));
            BuildPathStrip(pathsRoot.transform, "Path_South", asphalt,
                center: new Vector3(0f, y, -pathLength * 0.5f), size: new Vector3(pathWidth, pathLength));
            BuildPathStrip(pathsRoot.transform, "Path_East", asphalt,
                center: new Vector3(pathLength * 0.5f, y, 0f), size: new Vector3(pathLength, pathWidth));
            BuildPathStrip(pathsRoot.transform, "Path_West", asphalt,
                center: new Vector3(-pathLength * 0.5f, y, 0f), size: new Vector3(pathLength, pathWidth));

            PrefabUtility.SaveAsPrefabAsset(root, HomePrefabPath);
            Debug.Log("Home_Alvaston ground fixed: solid grass green, 4 grey paths added from spawn to each edge.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    /// <summary>size.x/size.y map to world X/Z width; built-in Plane mesh is 10x10 at scale 1.</summary>
    private static void BuildPathStrip(Transform parent, string name, Material mat, Vector3 center, Vector2 size)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Plane);
        strip.name = name;
        Object.DestroyImmediate(strip.GetComponent<Collider>());

        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = center;
        strip.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);

        strip.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    private static Material GetOrCreateGrassMaterial()
    {
        Material grass = AssetDatabase.LoadAssetAtPath<Material>(GrassMatPath);
        if (grass != null) return grass;

        Shader shader = Shader.Find("Standard");
        grass = new Material(shader) { color = EKVibe.GroundGrass };
        AssetDatabase.CreateAsset(grass, GrassMatPath);
        return grass;
    }
}
