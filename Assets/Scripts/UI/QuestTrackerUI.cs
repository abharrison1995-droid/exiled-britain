using UnityEngine;
using TMPro;
using ExiledAlvaston.Quests;

namespace ExiledAlvaston.UI
{
    /// <summary>
    /// Simple on-HUD quest tracker (title + current objective).
    /// Keep this component on an always-active object; toggle <see cref="Root"/> only.
    /// </summary>
    public class QuestTrackerUI : MonoBehaviour
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ObjectiveText;
        public GameObject Root;

        private void OnEnable()
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestsChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestsChanged -= Refresh;
        }

        private void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestsChanged -= Refresh;
                QuestManager.Instance.OnQuestsChanged += Refresh;
            }
            Refresh();
        }

        public void Refresh()
        {
            var q = QuestManager.Instance != null ? QuestManager.Instance.GetActiveQuest() : null;
            bool show = q != null;

            if (Root != null && Root != gameObject)
                Root.SetActive(show);

            if (!show) return;

            if (TitleText != null) TitleText.text = q.Title;
            if (ObjectiveText != null) ObjectiveText.text = q.Objective;
        }
    }
}
