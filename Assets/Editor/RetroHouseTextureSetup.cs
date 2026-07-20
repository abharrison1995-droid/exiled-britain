using UnityEngine;
using UnityEditor;

/// <summary>
/// Assigns each retro_house_pack house's tex1 texture as its Albedo, for house2-house5
/// (house1 was already done by hand). Uses ModelImporter.AddRemap to bind a real external
/// material to the FBX's internal material slot (named "house2".."house5", confirmed from
/// the pack's .mtl files) — this is the correct API for this pack's import mode; a plain
/// AssetDatabase sub-asset extraction doesn't find anything to extract on these files.
/// </summary>
public static class RetroHouseTextureSetup
{
    private const string FbxFolder = "Assets/3DModels/retro_house_pack/retro_house_pack/models/fbx";
    private const string TextureFolder = "Assets/3DModels/retro_house_pack/retro_house_pack/textures/512x512";
    private const string MaterialOutFolder = "Assets/Materials/RetroHouses";

    [MenuItem("Tools/Exiled Alvaston/Texture Retro Houses (2-5)")]
    public static void Run()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MaterialOutFolder))
            AssetDatabase.CreateFolder("Assets/Materials", "RetroHouses");

        for (int i = 2; i <= 5; i++)
        {
            ProcessHouse($"house{i}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Retro house texturing complete for house2-house5 (tex1 assigned via ModelImporter remap). house1 left untouched.");
    }

    private static void ProcessHouse(string houseName)
    {
        string fbxPath = $"{FbxFolder}/{houseName}.fbx";
        string texPath = $"{TextureFolder}/{houseName}_tex1.png";
        string matPath = $"{MaterialOutFolder}/{houseName}.mat";

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (texture == null)
        {
            Debug.LogWarning($"RetroHouseTextureSetup: missing texture at {texPath} — skipped {houseName}.");
            return;
        }

        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"RetroHouseTextureSetup: couldn't get ModelImporter for {fbxPath} — skipped.");
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { name = houseName };
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.mainTexture = texture;
        EditorUtility.SetDirty(mat);

        var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), houseName);
        importer.AddRemap(id, mat);
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
}
