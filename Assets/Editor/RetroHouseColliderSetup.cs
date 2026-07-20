using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns on mesh collider generation for the retro_house_pack FBX models so they
/// physically block movement instead of being walk-through visuals.
/// </summary>
public static class RetroHouseColliderSetup
{
    private const string FbxFolder = "Assets/3DModels/retro_house_pack/retro_house_pack/models/fbx";

    [MenuItem("Tools/Exiled Alvaston/Add Colliders To Retro Houses")]
    public static void Run()
    {
        int changed = 0;
        for (int i = 1; i <= 5; i++)
        {
            string path = $"{FbxFolder}/house{i}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"RetroHouseColliderSetup: couldn't get ModelImporter for {path}");
                continue;
            }

            if (!importer.addCollider)
            {
                importer.addCollider = true;
                importer.SaveAndReimport();
                changed++;
            }
        }

        Debug.Log($"Retro house colliders enabled on {changed} model(s) (house1-house5). " +
                  "Existing instances in the scene should update automatically; if any still feel walk-through, re-drag a fresh copy from the Project window.");
    }
}
