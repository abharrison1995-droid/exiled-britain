using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExiledAlvaston.Quests
{
    [Serializable]
    public class QuestProgress
    {
        public string Id;
        public string Title;
        public string Objective;
        public bool IsActive;
        public bool IsComplete;
    }

    /// <summary>
    /// Tiny quest list for Discover England — start/complete/track active quests.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public event Action OnQuestsChanged;

        private readonly List<QuestProgress> _quests = new List<QuestProgress>(8);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public IReadOnlyList<QuestProgress> Quests => _quests;

        public QuestProgress GetActiveQuest()
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].IsActive && !_quests[i].IsComplete)
                    return _quests[i];
            }
            return null;
        }

        public void StartQuest(string id, string title, string objective)
        {
            var existing = Find(id);
            if (existing != null)
            {
                if (existing.IsComplete) return;
                existing.IsActive = true;
                existing.Title = title;
                existing.Objective = objective;
            }
            else
            {
                _quests.Add(new QuestProgress
                {
                    Id = id,
                    Title = title,
                    Objective = objective,
                    IsActive = true,
                    IsComplete = false
                });
            }

            OnQuestsChanged?.Invoke();
        }

        public void UpdateObjective(string id, string objective)
        {
            var q = Find(id);
            if (q == null || q.IsComplete) return;
            q.Objective = objective;
            OnQuestsChanged?.Invoke();
        }

        public void CompleteQuest(string id)
        {
            var q = Find(id);
            if (q == null)
            {
                _quests.Add(new QuestProgress
                {
                    Id = id,
                    Title = id,
                    Objective = "",
                    IsActive = false,
                    IsComplete = true
                });
            }
            else
            {
                q.IsComplete = true;
                q.IsActive = false;
            }
            OnQuestsChanged?.Invoke();
        }

        public bool IsComplete(string id)
        {
            var q = Find(id);
            return q != null && q.IsComplete;
        }

        public void ClearAll()
        {
            _quests.Clear();
            OnQuestsChanged?.Invoke();
        }

        private QuestProgress Find(string id)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Id == id) return _quests[i];
            }
            return null;
        }
    }
}
