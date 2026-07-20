using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Fixes 3 HUD elements built by UISetup.cs with a center pivot instead of a pivot matching
/// their corner anchor, which pushed roughly half of each element past the screen edge.
/// </summary>
public static class FixHudCornerPivots
{
    [MenuItem("Tools/Exiled Alvaston/Fix HUD Corner Pivots")]
    public static void Run()
    {
        Fix("TopLeftPortraits", new Vector2(0, 1));
        Fix("MapBagShortcut", new Vector2(1, 1));
        Fix("ActionCluster", new Vector2(1, 0));
        Fix("VirtualJoystick", new Vector2(0, 0));

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("HUD corner pivots fixed — TopLeftPortraits, MapBagShortcut, ActionCluster, and VirtualJoystick should now sit fully on-screen.");
    }

    private static void Fix(string name, Vector2 pivot)
    {
        GameObject go = GameObject.Find(name);
        if (go == null)
        {
            Debug.LogWarning($"FixHudCornerPivots: couldn't find '{name}' in the open scene.");
            return;
        }

        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogWarning($"FixHudCornerPivots: '{name}' has no RectTransform.");
            return;
        }

        Undo.RecordObject(rt, "Fix HUD Corner Pivot");
        rt.pivot = pivot;
    }
}
