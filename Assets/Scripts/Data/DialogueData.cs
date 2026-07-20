using UnityEngine;
using System.Collections.Generic;

namespace ExiledAlvaston.Data
{
    [System.Serializable]
    public class DialogueChoice
    {
        [TextArea] public string ChoiceText;
        public DialogueNode NextNode;
        
        [Header("Stat Checks (Optional)")]
        public string RequiredStat; // e.g., "Personality", "STR"
        public int RequiredStatLevel;
        
        public bool MeetsRequirement(CoreTraits playerTraits)
        {
            if (string.IsNullOrEmpty(RequiredStat)) return true;
            
            // Simple mock evaluation
            if (RequiredStat == "STR" && playerTraits.Strength >= RequiredStatLevel) return true;
            if (RequiredStat == "INT" && playerTraits.Intelligence >= RequiredStatLevel) return true;
            if (RequiredStat == "Personality" && playerTraits.Awareness >= RequiredStatLevel) return true;
            
            return false;
        }
    }

    [System.Serializable]
    public class DialogueNode
    {
        public CharacterData Speaker;
        [TextArea(3, 10)] public string DialogueText;
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
    }

    /// <summary>
    /// Represents a full conversation tree.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueTree", menuName = "ExiledAlvaston/Data/Dialogue Tree")]
    public class DialogueData : ScriptableObject
    {
        public DialogueNode StartingNode;
    }
}
