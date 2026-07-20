using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ExiledAlvaston.Data;
using ExiledAlvaston.World;
using ExiledAlvaston.Combat;

/// <summary>
/// One-off setup: activates the chunk-streaming overworld (Home_Alvaston + its 4 neighbors,
/// already cross-wired in their MapChunkData assets) in the currently open scene, without
/// touching the existing Manor Cellars dungeon or TestEnemy.
/// Run via Tools/Exiled Alvaston/Setup Overworld Chunks with c.unity open.
/// </summary>
public static class OverworldChunkSetup
{
    private const string HomeDataPath = "Assets/Data/Chunks/Home_Alvaston_Data.asset";

    [MenuItem("Tools/Exiled Alvaston/Setup Overworld Chunks")]
    public static void Run()
    {
        DeactivateDungeon();

        MapChunkData home = AssetDatabase.LoadAssetAtPath<MapChunkData>(HomeDataPath);
        if (home == null)
        {
            Debug.LogError($"OverworldChunkSetup: couldn't load {HomeDataPath}");
            return;
        }
        if (home.ChunkPrefab == null)
        {
            Debug.LogError("OverworldChunkSetup: Home_Alvaston_Data has no ChunkPrefab assigned.");
            return;
        }

        ChunkManager chunkMgr = Object.FindObjectOfType<ChunkManager>();
        if (chunkMgr == null)
        {
            var mgrGo = new GameObject("ChunkManager");
            Undo.RegisterCreatedObjectUndo(mgrGo, "Setup Overworld Chunks");
            chunkMgr = mgrGo.AddComponent<ChunkManager>();
        }
        else
        {
            Undo.RecordObject(chunkMgr, "Setup Overworld Chunks");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            var combat = Object.FindObjectOfType<CombatController>();
            if (combat != null) player = combat.gameObject;
        }

        if (player == null)
            Debug.LogWarning("OverworldChunkSetup: couldn't find Player in the scene — ChunkManager.PlayerTransform left unset.");
        else
            chunkMgr.PlayerTransform = player.transform;

        chunkMgr.CurrentChunkData = home;

        if (chunkMgr.CurrentChunkInstance == null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(home.ChunkPrefab);
            Undo.RegisterCreatedObjectUndo(instance, "Setup Overworld Chunks");
            instance.transform.position = Vector3.zero;
            instance.name = home.ChunkPrefab.name;
            chunkMgr.CurrentChunkInstance = instance;
        }

        if (player != null)
        {
            Undo.RecordObject(player.transform, "Setup Overworld Chunks");
            player.transform.position = new Vector3(0f, 1f, 0f);
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.velocity = Vector3.zero;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Overworld chunk setup complete. Home_Alvaston is active with North/South/East/West already wired " +
                  "(walk to any edge to cross into the next chunk). Manor Cellars was deactivated, not deleted. " +
                  "Run Tools/World/Bake Navigation Mesh so TestEnemy can path on the new floor, then Ctrl+S.");
    }

    private static void DeactivateDungeon()
    {
        GameObject dungeon = GameObject.Find("Dungeon_NayauAnnex");
        if (dungeon == null)
        {
            Debug.Log("OverworldChunkSetup: no Dungeon_NayauAnnex found in scene — nothing to deactivate.");
            return;
        }

        // Safety: don't disable it if the Player or TestEnemy live under it — that would hide them too.
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        GameObject enemy = GameObject.Find("TestEnemy");
        if ((player != null && player.transform.IsChildOf(dungeon.transform))
            || (enemy != null && enemy.transform.IsChildOf(dungeon.transform)))
        {
            Debug.LogWarning("OverworldChunkSetup: Player or TestEnemy is parented under Dungeon_NayauAnnex — skipped deactivating it to avoid hiding them. Handle manually.");
            return;
        }

        Undo.RecordObject(dungeon, "Setup Overworld Chunks");
        dungeon.SetActive(false);
    }
}
