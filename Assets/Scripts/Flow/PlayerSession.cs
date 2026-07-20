using UnityEngine;
using ExiledAlvaston.Data;

namespace ExiledAlvaston.Flow
{
    /// <summary>
    /// Runtime session for Discover England — created character + tutorial flags.
    /// Survives scene loads via DontDestroyOnLoad.
    /// </summary>
    public class PlayerSession : MonoBehaviour
    {
        public static PlayerSession Instance { get; private set; }

        [Header("Created Character")]
        public string CharacterName = "Exile";
        public PlayerClass Class = PlayerClass.YoungDriller;
        public CharacterData RuntimeStats;

        [Header("Progress")]
        public bool TutorialComplete;
        public bool HasStartedNewGame;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void BeginNewGame(string characterName, PlayerClass playerClass, CharacterData template)
        {
            CharacterName = string.IsNullOrWhiteSpace(characterName) ? "Exile" : characterName.Trim();
            Class = playerClass;
            TutorialComplete = false;
            HasStartedNewGame = true;

            if (RuntimeStats == null)
                RuntimeStats = ScriptableObject.CreateInstance<CharacterData>();

            if (template != null)
            {
                RuntimeStats.Portrait = template.Portrait;
                RuntimeStats.BaseResistances = template.BaseResistances;
            }

            RuntimeStats.CharacterName = CharacterName;
            RuntimeStats.ApplyClassDefaults(Class);
        }

        public void CompleteTutorial()
        {
            TutorialComplete = true;
        }
    }
}
