using UnityEngine;
using UnityEditor;
using ExiledAlvaston.Combat;
using ExiledAlvaston.Data;
using ExiledAlvaston.World;
using ExiledAlvaston.Vibe;
using ExiledAlvaston.Systems;

/// <summary>
/// Boots a clean playable scene around your chunk prefabs (no EKWorld placeholders).
/// </summary>
public class SceneSetup : EditorWindow
{
    [MenuItem("Tools/World/Setup Chunk Scene")]
    public static void SetupScene()
    {
        WipeLegacyJunk();

        // Camera
        UnityEngine.Camera mainCam = UnityEngine.Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            mainCam = camGO.AddComponent<UnityEngine.Camera>();
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = EKVibe.CameraOrthoSize;
        mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.1f, 1f);
        mainCam.transform.rotation = Quaternion.Euler(EKVibe.CameraPitch, EKVibe.CameraYaw, 0f);

        Vector3 camDir = mainCam.transform.rotation * Vector3.back;
        mainCam.transform.position = Vector3.zero + camDir * EKVibe.CameraDistance;

        IsometricCameraFollow follow = mainCam.GetComponent<IsometricCameraFollow>();
        if (follow == null) follow = mainCam.gameObject.AddComponent<IsometricCameraFollow>();
        follow.OrthoSize = EKVibe.CameraOrthoSize;
        follow.ApplyVibeLock();

        // Light
        Light sun = Object.FindObjectOfType<Light>();
        if (sun == null)
        {
            GameObject lightGO = new GameObject("Directional Light");
            sun = lightGO.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = EKVibe.DirectionalLight;
        sun.intensity = 1.05f;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        sun.shadows = LightShadows.Soft;

        // Player stats
        string assetPath = "Assets/Data/PlayerStats.asset";
        CharacterData playerStats = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
        if (playerStats == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            playerStats = ScriptableObject.CreateInstance<CharacterData>();
            playerStats.CharacterName = "Hero";
            playerStats.MaxHealth = 100;
            playerStats.MaxManaStamina = 50;
            playerStats.BaseMovementSpeed = 5;
            playerStats.BaseTraits = new CoreTraits
            {
                Strength = 5, Endurance = 5, Agility = 5,
                Intelligence = 5, Awareness = 5, Perception = 5
            };
            AssetDatabase.CreateAsset(playerStats, assetPath);
            AssetDatabase.SaveAssets();
        }

        // Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);

            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            CombatController combat = player.AddComponent<CombatController>();
            combat.PlayerData = playerStats;
            combat.MovementSpeed = 5f;

            Health health = player.AddComponent<Health>();
            health.MaxHealth = playerStats.MaxHealth;
            health.DisplayName = playerStats.CharacterName;
        }

        follow.SetTarget(player.transform);

        // Wanted manager
        if (Object.FindObjectOfType<WantedManager>() == null)
            new GameObject("WantedManager").AddComponent<WantedManager>();

        // Chunk manager + Home Alvaston
        MapChunkData home = AssetDatabase.LoadAssetAtPath<MapChunkData>("Assets/Data/Chunks/Home_Alvaston_Data.asset");
        ChunkManager chunkMgr = Object.FindObjectOfType<ChunkManager>();
        if (chunkMgr == null)
            chunkMgr = new GameObject("ChunkManager").AddComponent<ChunkManager>();

        chunkMgr.PlayerTransform = player.transform;
        if (home != null)
        {
            chunkMgr.CurrentChunkData = home;

            // Clear any stale chunk instances in the scene
            foreach (var existing in Object.FindObjectsOfType<ChunkEdge>())
            {
                if (existing != null && existing.transform.root != null)
                {
                    string rootName = existing.transform.root.name;
                    if (rootName.Contains("_Prefab") || rootName.Contains("Alvaston") || rootName.Contains("Wasteland")
                        || rootName.Contains("Slums") || rootName.Contains("Retail") || rootName.Contains("Canal"))
                    {
                        Object.DestroyImmediate(existing.transform.root.gameObject);
                    }
                }
            }

            if (home.ChunkPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(home.ChunkPrefab);
                instance.transform.position = Vector3.zero;
                chunkMgr.CurrentChunkInstance = instance;
                Selection.activeGameObject = instance;
            }
        }
        else
        {
            Debug.LogWarning("Home_Alvaston_Data.asset not found under Assets/Data/Chunks.");
        }

        EditorUtility.SetDirty(chunkMgr);
        Debug.Log("Chunk scene ready — Home Alvaston loaded. Run Tools/Generate Exiled UI if HUD is missing, then Bake Navigation Mesh.");
    }

    private static void WipeLegacyJunk()
    {
        string[] junkNames = { "EKWorld", "TestEnemy", "Ground", "Outdoor_Kingsbridge", "Dungeon_NayauAnnex" };
        foreach (string n in junkNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                // Don't delete Ground if it's inside a chunk prefab
                if (n == "Ground" && go.transform.parent != null) continue;
                Object.DestroyImmediate(go);
            }
        }
    }
}
