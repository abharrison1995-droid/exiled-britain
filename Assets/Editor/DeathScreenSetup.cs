using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ExiledAlvaston.Vibe;
using ExiledAlvaston.World;
using ExiledAlvaston.Data;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Builds the death screen UI (dark overlay + You Died + 3 buttons) under UICanvas, wires it to
/// DeathScreenUI, and populates ChunkManager.AllChunks so Save/Load can resolve chunks by name.
/// </summary>
public static class DeathScreenSetup
{
    [MenuItem("Tools/Exiled Alvaston/Setup Death Screen")]
    public static void Run()
    {
        GameObject canvasGO = GameObject.Find("UICanvas");
        if (canvasGO == null)
        {
            Debug.LogWarning("DeathScreenSetup: no 'UICanvas' found in the open scene.");
            return;
        }

        PopulateChunkRegistry();

        GameObject existing = GameObject.Find("DeathScreen");
        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject panel = CreateImage("DeathScreen", canvasGO.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.05f, 0.03f, 0.02f, 0.88f));
        CanvasGroup group = panel.AddComponent<CanvasGroup>();

        CreateTMP("DeathTitle", panel.transform, "YOU DIED",
            new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.78f), EKVibe.HealthBar, 56, TextAlignmentOptions.Center);

        GameObject loadBtn = CreateButton("LoadLastGameButton", panel.transform, "Load Last Game", 0.46f);
        GameObject newBtn = CreateButton("NewGameButton", panel.transform, "New Game", 0.34f);
        GameObject quitBtn = CreateButton("QuitButton", panel.transform, "Quit", 0.22f);

        var death = panel.AddComponent<ExiledAlvaston.UI.DeathScreenUI>();
        death.Root = group;
        death.LoadLastGameButton = loadBtn.GetComponent<Button>();
        death.NewGameButton = newBtn.GetComponent<Button>();
        death.QuitButton = quitBtn.GetComponent<Button>();

        // Hidden via CanvasGroup, not SetActive(false) — the GameObject must stay active so its
        // Awake() actually runs at Play start and registers DeathScreenUI.Instance.
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Death screen built and wired. Player.CombatController will show it automatically on death (falls back to it when there's no GameFlowController).");
    }

    private static void PopulateChunkRegistry()
    {
        ChunkManager chunkMgr = Object.FindObjectOfType<ChunkManager>();
        if (chunkMgr == null)
        {
            Debug.LogWarning("DeathScreenSetup: no ChunkManager in the open scene — AllChunks not populated.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:MapChunkData", new[] { "Assets/Data/Chunks" });
        List<MapChunkData> chunks = guids
            .Select(g => AssetDatabase.LoadAssetAtPath<MapChunkData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(c => c != null)
            .ToList();

        Undo.RecordObject(chunkMgr, "Setup Death Screen");
        chunkMgr.AllChunks = chunks.ToArray();
        Debug.Log($"DeathScreenSetup: ChunkManager.AllChunks populated with {chunks.Count} chunk(s): {string.Join(", ", chunks.Select(c => c.ChunkName))}");
    }

    private static GameObject CreateButton(string name, Transform parent, string label, float centerYNorm)
    {
        float halfW = 0.14f, halfH = 0.045f;
        GameObject go = CreateImage(name, parent,
            new Vector2(0.5f - halfW, centerYNorm - halfH), new Vector2(0.5f + halfW, centerYNorm + halfH),
            Vector2.zero, Vector2.zero, EKVibe.ButtonBrown);
        go.AddComponent<Button>();
        CreateTMP(name + "Label", go.transform, label, Vector2.zero, Vector2.one,
            EKVibe.TextLight, 22, TextAlignmentOptions.Center);
        return go;
    }

    private static GameObject CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, Color color, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }
}
