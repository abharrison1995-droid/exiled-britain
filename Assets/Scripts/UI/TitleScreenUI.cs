using UnityEngine;
using UnityEngine.UI;
using ExiledAlvaston.Flow;

namespace ExiledAlvaston.UI
{
    public class TitleScreenUI : MonoBehaviour
    {
        public Button NewGameButton;
        public Button QuitButton;

        private void Awake()
        {
            if (NewGameButton != null)
            {
                NewGameButton.onClick.RemoveAllListeners();
                NewGameButton.onClick.AddListener(OnNewGame);
            }
            if (QuitButton != null)
            {
                QuitButton.onClick.RemoveAllListeners();
                QuitButton.onClick.AddListener(OnQuit);
            }
        }

        private void OnNewGame()
        {
            if (GameFlowController.Instance != null)
                GameFlowController.Instance.ShowCreator();
            else
                Debug.LogWarning("TitleScreen: no GameFlowController.");
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
