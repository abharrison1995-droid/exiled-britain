using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ExiledAlvaston.Vibe;

namespace ExiledAlvaston.UI
{
    /// <summary>
    /// EK-style HUD: portrait + bars, combat log, location/time, joystick, action cluster.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD Panels")]
        public RectTransform TopLeftPortraitPanel;
        public RectTransform BottomCenterQuickSlotPanel;
        public RectTransform RightActionPanel;
        public RectTransform CombatLogPanel;
        public VirtualJoystick Joystick;

        [Header("Player HUD")]
        public Image PlayerPortrait;
        public Image PlayerHealthFill;
        public Image PlayerManaFill;
        public TextMeshProUGUI LevelText;
        public TextMeshProUGUI LocationTimeText;
        public TextMeshProUGUI WantedKnivesText;

        [Header("Combat Log")]
        public TextMeshProUGUI CombatLogText;
        public int MaxCombatLogLines = 5;

        [Header("Companion HUD")]
        public GameObject CompanionHUDTemplate;
        public Transform CompanionHUDContainer;

        private readonly Queue<string> _logLines = new Queue<string>();
        private float _playerHpMax = 100f;
        private float _playerMpMax = 50f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void UpdatePlayerHealth(int current, int max)
        {
            _playerHpMax = Mathf.Max(1, max);
            if (PlayerHealthFill != null)
                PlayerHealthFill.fillAmount = Mathf.Clamp01(current / _playerHpMax);
        }

        public void UpdatePlayerMana(int current, int max)
        {
            _playerMpMax = Mathf.Max(1, max);
            if (PlayerManaFill != null)
                PlayerManaFill.fillAmount = Mathf.Clamp01(current / _playerMpMax);
        }

        public void SetLevel(int level)
        {
            if (LevelText != null)
                LevelText.text = level.ToString();
        }

        public void SetLocationTime(string location, int day, string clock)
        {
            if (LocationTimeText != null)
                LocationTimeText.text = $"{location}; Day {day}, {clock}";
        }

        public void UpdateKnivesUI(int knives)
        {
            if (WantedKnivesText != null)
                WantedKnivesText.text = $"Knives: {knives}";
        }

        /// <summary>EK combat log style: "> Elite Bandit hits you, 14-7=7"</summary>
        public void LogCombat(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (!message.StartsWith(">"))
                message = "> " + message;

            _logLines.Enqueue(message);
            while (_logLines.Count > MaxCombatLogLines)
                _logLines.Dequeue();

            if (CombatLogText != null)
                CombatLogText.text = string.Join("\n", _logLines);
        }

        public void OnActionButtonPressed(int abilityIndex)
        {
            var combat = Combat.CombatController.Instance;
            if (combat != null)
                combat.TryCastAbility(abilityIndex);
        }

        public void OnAttackPressed()
        {
            var combat = Combat.CombatController.Instance;
            if (combat != null)
                combat.PerformMeleeAttack();
        }

        public void OnInventoryPressed()
        {
            var inv = FindObjectOfType<InventoryController>();
            if (inv != null)
                inv.ToggleInventory();
        }
    }
}
