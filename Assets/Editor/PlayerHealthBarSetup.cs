using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ExiledAlvaston.World;

public static class PlayerHealthBarSetup
{
    [MenuItem("Tools/Exiled Alvaston/Add Player Health Bar")]
    public static void Run()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("PlayerHealthBarSetup: couldn't find 'Player' in the open scene.");
            return;
        }

        if (player.GetComponent<PlayerHealthBar>() == null)
        {
            Undo.AddComponent<PlayerHealthBar>(player);
            Debug.Log("Player health bar added — it'll appear above your head whenever you take damage.");
        }
        else
        {
            Debug.Log("Player already has a health bar.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
