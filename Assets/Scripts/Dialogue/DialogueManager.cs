using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using ExiledAlvaston.Data;

namespace ExiledAlvaston.Dialogue
{
    /// <summary>
    /// Handles the Exiled Kingdoms style dialogue UI.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI References")]
        public GameObject DialoguePanel;
        public Image PortraitImage;
        public TextMeshProUGUI SpeakerNameText;
        public TextMeshProUGUI MainDialogueText;
        public Transform ChoicesContainer;
        public GameObject ChoiceButtonPrefab;

        private CharacterData _currentPlayerData; // To evaluate stat checks

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private bool _dialogueActive;

        public void StartDialogue(DialogueData data, CharacterData playerData)
        {
            if (data == null || data.StartingNode == null) return;
            if (_dialogueActive) return;

            _currentPlayerData = playerData;
            DialoguePanel.SetActive(true);

            // Pause the game during conversation
            _dialogueActive = true;
            ExiledAlvaston.Systems.PauseManager.Push();

            DisplayNode(data.StartingNode);
        }

        private void DisplayNode(DialogueNode node)
        {
            if (node.Speaker != null)
            {
                SpeakerNameText.text = node.Speaker.CharacterName;
                PortraitImage.sprite = node.Speaker.Portrait;
            }

            MainDialogueText.text = node.DialogueText;

            // Clear old choices
            foreach (Transform child in ChoicesContainer)
            {
                Destroy(child.gameObject);
            }

            // Populate new choices
            if (node.Choices == null || node.Choices.Count == 0)
            {
                // Add an "End Conversation" button if no choices exist
                CreateChoiceButton("End Conversation.", null, true);
            }
            else
            {
                for (int i = 0; i < node.Choices.Count; i++)
                {
                    DialogueChoice choice = node.Choices[i];

                    string displayText = $"{i + 1}. {choice.ChoiceText}";
                    bool selectable = true;

                    // Append stat requirement if it exists
                    if (!string.IsNullOrEmpty(choice.RequiredStat))
                    {
                        bool pass = _currentPlayerData != null && choice.MeetsRequirement(_currentPlayerData.BaseTraits);
                        string color = pass ? "green" : "red";
                        displayText = $"<color={color}>({choice.RequiredStat} {choice.RequiredStatLevel})</color> {displayText}";
                        selectable = pass;
                    }

                    CreateChoiceButton(displayText, choice.NextNode, selectable);
                }
            }
        }

        private void CreateChoiceButton(string text, DialogueNode nextNode, bool selectable)
        {
            GameObject btnObj = Instantiate(ChoiceButtonPrefab, ChoicesContainer);
            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = text;

            Button btn = btnObj.GetComponent<Button>();
            btn.interactable = selectable;
            btn.onClick.AddListener(() => OnChoiceSelected(nextNode));
        }

        private void OnChoiceSelected(DialogueNode nextNode)
        {
            if (nextNode == null || string.IsNullOrEmpty(nextNode.DialogueText))
            {
                EndDialogue();
            }
            else
            {
                DisplayNode(nextNode);
            }
        }

        private void EndDialogue()
        {
            DialoguePanel.SetActive(false);
            if (_dialogueActive)
            {
                _dialogueActive = false;
                ExiledAlvaston.Systems.PauseManager.Pop();
            }
        }
    }
}
