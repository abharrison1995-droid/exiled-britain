using UnityEngine;
using ExiledAlvaston.Data;
using ExiledAlvaston.World;
using ExiledAlvaston.Combat;
using ExiledAlvaston.UI;
using ExiledAlvaston.Quests;
using ExiledAlvaston.Vibe;

namespace ExiledAlvaston.Flow
{
    public enum GameFlowState
    {
        Title,
        CharacterCreator,
        Playing
    }

    /// <summary>
    /// Discover England bootstrap: Title → Creator → Manor Cellars → London gates.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        public const string EscapeManorQuestId = "escape_manor";

        public static GameFlowController Instance { get; private set; }

        [Header("UI Roots")]
        public GameObject TitleRoot;
        public GameObject CreatorRoot;
        public GameObject HudRoot;

        [Header("World")]
        public MapChunkData ManorCellarsChunk;
        public MapChunkData LondonChunk;
        public ChunkManager ChunkManager;

        [Header("Spawn")]
        public Vector3 ManorSpawnPosition = new Vector3(0f, 0f, -8f);

        /// <summary>Blocks InstanceDoor briefly after exiting so spawn doesn't re-enter.</summary>
        public float InstanceDoorReadyAt { get; private set; }

        public GameFlowState State { get; private set; } = GameFlowState.Title;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsureSession();
            EnsureQuestManager();
            ShowTitle();
        }

        private void EnsureSession()
        {
            if (PlayerSession.Instance == null)
                new GameObject("PlayerSession").AddComponent<PlayerSession>();
        }

        private void EnsureQuestManager()
        {
            if (QuestManager.Instance == null)
                new GameObject("QuestManager").AddComponent<QuestManager>();
        }

        public bool CanUseInstanceDoors => Time.unscaledTime >= InstanceDoorReadyAt;

        public void ShowTitle()
        {
            State = GameFlowState.Title;
            SetUi(title: true, creator: false, hud: false);
            ExiledAlvaston.Systems.PauseManager.Reset();
        }

        public void ShowCreator()
        {
            State = GameFlowState.CharacterCreator;
            SetUi(title: false, creator: true, hud: false);
        }

        public void StartNewGame(string characterName, PlayerClass playerClass)
        {
            EnsureSession();
            EnsureQuestManager();
            QuestManager.Instance.ClearAll();

            var existing = CombatController.Instance ?? FindObjectOfType<CombatController>();
            CharacterData templateData = existing != null ? existing.PlayerData : null;
            PlayerSession.Instance.BeginNewGame(characterName, playerClass, templateData);

            if (existing != null && PlayerSession.Instance.RuntimeStats != null)
            {
                existing.PlayerData = PlayerSession.Instance.RuntimeStats;
                existing.CurrentHealth = PlayerSession.Instance.RuntimeStats.MaxHealth;
                existing.CurrentMana = PlayerSession.Instance.RuntimeStats.MaxManaStamina;
                var hp = existing.GetComponent<Health>();
                if (hp != null)
                {
                    hp.MaxHealth = PlayerSession.Instance.RuntimeStats.MaxHealth;
                    hp.CurrentHealth = hp.MaxHealth;
                    hp.DisplayName = PlayerSession.Instance.CharacterName;
                }
            }

            var inventory = FindObjectOfType<UI.InventoryController>(true);
            if (inventory != null)
                inventory.BindCharacter(PlayerSession.Instance.RuntimeStats);

            EnterManorCellars(isTutorial: true);
        }

        public void EnterManorCellars()
        {
            EnterManorCellars(isTutorial: true);
        }

        /// <summary>Optional revisit from London west door (post-tutorial).</summary>
        public void EnterManorCellarsOptional()
        {
            EnterManorCellars(isTutorial: false);
        }

        private void EnterManorCellars(bool isTutorial)
        {
            State = GameFlowState.Playing;
            SetUi(title: false, creator: false, hud: true);
            InstanceDoorReadyAt = Time.unscaledTime + 1.25f;

            if (ChunkManager == null)
                ChunkManager = FindObjectOfType<ChunkManager>();

            if (ChunkManager != null && ManorCellarsChunk != null && ManorCellarsChunk.ChunkPrefab != null)
            {
                if (ChunkManager.CurrentChunkInstance != null)
                    Destroy(ChunkManager.CurrentChunkInstance);

                ChunkManager.CurrentChunkData = ManorCellarsChunk;
                ChunkManager.CurrentChunkInstance = Instantiate(
                    ManorCellarsChunk.ChunkPrefab, Vector3.zero, Quaternion.identity);
                ChunkManager.CurrentChunkInstance.name = ManorCellarsChunk.ChunkPrefab.name;

                var player = CombatController.Instance ?? FindObjectOfType<CombatController>();
                if (player != null)
                {
                    ChunkManager.PlayerTransform = player.transform;
                    ChunkManager.TeleportPlayer(ManorSpawnPosition);
                }
            }

            EnsureQuestManager();
            if (isTutorial)
            {
                QuestManager.Instance.StartQuest(
                    EscapeManorQuestId,
                    "Escape the Cellars",
                    "Find the manor gate and get out.");
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetLocationTime("Manor Cellars", 1, "11 PM");
                UIManager.Instance.LogCombat(isTutorial
                    ? "You wake in the Manor Cellars. Find a way out."
                    : "Back in the Manor Cellars. Watch the corners.");
            }

            var tracker = FindObjectOfType<QuestTrackerUI>();
            if (tracker != null) tracker.Refresh();
        }

        /// <summary>
        /// Player hit 0 HP. Short beat for the death to read, then respawn
        /// at the Manor Cellars with full health (EK-style "you wake up" recovery).
        /// </summary>
        public void HandlePlayerDeath()
        {
            bool tutorialDone = PlayerSession.Instance != null && PlayerSession.Instance.TutorialComplete;

            // Post-tutorial, Manor Cellars is no longer "home" — use the real death screen
            // (Load Last Game / New Game / Quit) instead of auto-respawning there forever.
            if (tutorialDone && UI.DeathScreenUI.Instance != null)
            {
                UI.DeathScreenUI.Instance.Show();
                return;
            }

            if (UIManager.Instance != null)
                UIManager.Instance.LogCombat("You collapse...");
            StartCoroutine(RespawnRoutine(2f));
        }

        private System.Collections.IEnumerator RespawnRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            var player = CombatController.Instance;
            if (player == null) yield break;

            player.ReviveFull();

            bool tutorialDone = PlayerSession.Instance != null && PlayerSession.Instance.TutorialComplete;
            EnterManorCellars(isTutorial: !tutorialDone);

            if (UIManager.Instance != null)
                UIManager.Instance.LogCombat("You wake in the Manor Cellars, sore but alive.");
        }

        /// <summary>Exit Manor Cellars to London's west gates (tutorial or revisit).</summary>
        public void OnTutorialExitToGates()
        {
            EnsureQuestManager();
            if (!QuestManager.Instance.IsComplete(EscapeManorQuestId))
                QuestManager.Instance.CompleteQuest(EscapeManorQuestId);

            if (PlayerSession.Instance != null && !PlayerSession.Instance.TutorialComplete)
                PlayerSession.Instance.CompleteTutorial();

            LoadLondonAtWestGates();
        }

        private void LoadLondonAtWestGates()
        {
            InstanceDoorReadyAt = Time.unscaledTime + 1.5f;

            if (ChunkManager == null)
                ChunkManager = FindObjectOfType<ChunkManager>();

            if (ChunkManager == null || LondonChunk == null || LondonChunk.ChunkPrefab == null)
            {
                Debug.LogWarning("London chunk not wired — tutorial marked complete anyway.");
                if (UIManager.Instance != null)
                    UIManager.Instance.LogCombat("You step out through the manor gates.");
                return;
            }

            if (ChunkManager.CurrentChunkInstance != null)
                Destroy(ChunkManager.CurrentChunkInstance);

            ChunkManager.CurrentChunkData = LondonChunk;
            ChunkManager.CurrentChunkInstance = Instantiate(LondonChunk.ChunkPrefab, Vector3.zero, Quaternion.identity);
            ChunkManager.CurrentChunkInstance.name = LondonChunk.ChunkPrefab.name;

            float half = EKVibe.ChunkSize * 0.5f;
            // Spawn east of the manor door so cooldown + position avoid instant re-entry
            ChunkManager.TeleportPlayer(new Vector3(-half + 14f, 0f, 0f));

            EnsureManorEntranceOnCurrentLondon();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetLocationTime("London", 1, "11 PM");
                UIManager.Instance.LogCombat("Outside the Manor Cellars gates. London lies ahead.");
            }

            var tracker = FindObjectOfType<QuestTrackerUI>();
            if (tracker != null) tracker.Refresh();
        }

        /// <summary>Places / refreshes the west-path door back into Manor Cellars.</summary>
        public void EnsureManorEntranceOnCurrentLondon()
        {
            if (ChunkManager == null || ChunkManager.CurrentChunkInstance == null) return;

            var existing = ChunkManager.CurrentChunkInstance.GetComponentInChildren<InstanceDoor>();
            if (existing != null) return;

            float half = EKVibe.ChunkSize * 0.5f;
            GameObject door = new GameObject("ManorCellarsEntrance");
            door.transform.SetParent(ChunkManager.CurrentChunkInstance.transform, false);
            door.transform.localPosition = new Vector3(-half + 4f, 1.2f, 0f);

            var box = door.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.5f, 3f, 4f);

            var inst = door.AddComponent<InstanceDoor>();
            inst.Target = InstanceDoor.Destination.ManorCellars;
            inst.Prompt = "Enter Manor Cellars";
            inst.RequireTutorialComplete = true;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "DoorVisual";
            visual.transform.SetParent(door.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.5f, 2.6f, 3.2f);
            Object.Destroy(visual.GetComponent<Collider>());
            var r = visual.GetComponent<Renderer>();
            r.sharedMaterial = GetDoorMaterial();
        }

        private static Material _doorMaterial;

        private static Material GetDoorMaterial()
        {
            if (_doorMaterial == null)
            {
                Shader sh = Shader.Find("Unlit/Color")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("Standard");
                _doorMaterial = new Material(sh) { color = new Color(0.35f, 0.22f, 0.12f) };
            }
            return _doorMaterial;
        }

        private void SetUi(bool title, bool creator, bool hud)
        {
            if (TitleRoot != null) TitleRoot.SetActive(title);
            else
            {
                var t = GameObject.Find("TitleScreen");
                if (t != null) t.SetActive(title);
            }

            if (CreatorRoot != null) CreatorRoot.SetActive(creator);
            else
            {
                var c = GameObject.Find("CharacterCreator");
                if (c != null) c.SetActive(creator);
            }

            if (HudRoot != null) HudRoot.SetActive(hud);
        }
    }
}
