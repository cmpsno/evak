using System;
using UnityEngine;

namespace Campusano.Dialogue
{
    /// <summary>
    /// Flags raised when the player picks a story-branching choice.
    /// The mission machine maps these onto JobStateData.
    /// </summary>
    public enum DialogueChoiceFlag
    {
        None,
        Ep1_M2_Intimidate,
        Ep1_M2_PrivateConversation,
        Ep1_DP_Lie,
        Ep1_DP_Truth,
        Ep1_DP_Resignation
    }

    [Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        public string text;
        public AudioClip voiceClip;
        public float autoAdvanceDelay; // Default 0 = manual tap to advance
    }

    [Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public string targetNodeId;
        public DialogueChoiceFlag flagToSet;
    }

    [Serializable]
    public class DialogueNode
    {
        public string nodeId;
        public DialogueLine[] lines;
        public DialogueChoice[] choices; // Empty array = linear node
        public string nextNodeId;        // Used if choices is empty and dialogue continues
        public bool completesObjectiveOnEnd;
    }

    public interface IDialogueRunner
    {
        event Action<DialogueNode> OnNodeStarted;
        event Action<DialogueChoiceFlag> OnChoiceSelected;
        event Action OnDialogueEnded;

        void StartDialogueTree(string rootNodeId);
        void AdvanceLine();
        void SelectChoice(int choiceIndex);
        bool IsDialogueActive { get; }
    }
}
