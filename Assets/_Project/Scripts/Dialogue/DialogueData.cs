using System;
using UnityEngine;

namespace Campusano.Dialogue
{
    /// <summary>
    /// TICKET-EP1-01 v2. Locked data shapes — do not add/rename fields.
    /// </summary>
    public enum DialoguePresentation
    {
        InScene,   // character speaking; subtitle + name label; Time.timeScale = 0
        Narrator   // past-tense voiceover; subtitle only, no name label; timeScale unchanged
    }

    [CreateAssetMenu(fileName = "SpeakerData", menuName = "Campusano/Dialogue/Speaker")]
    public class SpeakerData : ScriptableObject
    {
        public string speakerId;        // "nicky", "c", "father", "mook", "narrator"
        public string displayName;      // "" for narrator
        public Color subtitleColor = Color.white;
        public AudioClip voiceBlip;     // optional per-character UI blip
    }

    [Serializable]
    public struct DialogueLine
    {
        public string speakerId;        // resolves to SpeakerData via DialogueDatabase
        public string text;
        public AudioClip voiceClip;     // null = silent / text-only
        public bool autoAdvance;        // false = wait for player tap
        public float autoAdvanceDelay;  // ignored if autoAdvance == false; 0 = advance instantly
    }

    [Serializable]
    public class DialogueChoice
    {
        public string choiceId;         // "intimidate", "private_convo", "lie", ...
        public string choiceText;
        public string resultNodeId;     // references another DialogueNode by nodeId
    }

    [Serializable]
    public class DialogueNode
    {
        public string nodeId;
        public DialoguePresentation presentation;
        public DialogueLine[] lines;
        public DialogueChoice[] choices; // empty = linear
        public string nextNodeId;        // used only when choices is empty; empty string = node ends tree
    }

    [CreateAssetMenu(fileName = "DialogueDatabase", menuName = "Campusano/Dialogue/Database")]
    public class DialogueDatabase : ScriptableObject
    {
        public SpeakerData[] speakers;
        public DialogueNode[] nodes;

        public SpeakerData GetSpeaker(string speakerId)
        {
            if (speakers != null)
                foreach (var s in speakers)
                    if (s != null && s.speakerId == speakerId) return s;
            Debug.LogWarning($"[Dialogue] Missing speaker: '{speakerId}'. Rendering with defaults.");
            DialogueDebug.Fire(DialogueDebugEvent.MissingSpeaker, speakerId);
            return null;
        }

        public DialogueNode GetNode(string nodeId)
        {
            if (nodes != null)
                foreach (var n in nodes)
                    if (n != null && n.nodeId == nodeId) return n;
            return null;
        }

        /// <summary>Load-time validation: choices win over nextNodeId; warn on conflict.</summary>
        public void Validate()
        {
            if (nodes == null) return;
            foreach (var n in nodes)
            {
                if (n == null) continue;
                bool hasChoices = n.choices != null && n.choices.Length > 0;
                if (hasChoices && !string.IsNullOrEmpty(n.nextNodeId))
                {
                    Debug.LogWarning($"[Dialogue] Node '{n.nodeId}' has both choices and nextNodeId. Choices win.");
                    DialogueDebug.Fire(DialogueDebugEvent.NodeHasBothChoicesAndNext, n.nodeId);
                }
            }
        }
    }

    public enum DialogueEndReason
    {
        ReachedTerminalNode,   // last node completed and had no nextNodeId, no choices
        PlayerSkipped,         // force-close called
        Error                  // missing node, invalid target, etc.
    }

    public interface IDialogueRunner
    {
        event Action<DialogueNode> OnNodeStarted;
        event Action<DialogueLine> OnLineStarted;
        event Action<string, string> OnChoiceSelected;   // (nodeId, choiceId)
        event Action<DialogueEndReason> OnDialogueEnded;

        void StartDialogueTree(string rootNodeId);
        void AdvanceLine();
        void SelectChoice(int choiceIndex);
        void ForceClose(DialogueEndReason reason);
        bool IsDialogueActive { get; }
    }

    // ------------------------------------------------------------------ debug

    public enum DialogueDebugEvent
    {
        MissingRootNode,            // StartDialogueTree(id) -> id not in database
        MissingResultNode,          // choice.resultNodeId not in database
        MissingNextNode,            // node.nextNodeId not in database
        MissingSpeaker,             // line.speakerId not in database.speakers
        NullVoiceClip,              // informational only, not an error
        NodeHasBothChoicesAndNext,  // warning at load time
        ForceClosed                 // informational
    }

    public static class DialogueDebug
    {
        public static event Action<DialogueDebugEvent, string> OnDebugEvent;
        public static void Fire(DialogueDebugEvent evt, string context)
        {
            OnDebugEvent?.Invoke(evt, context);
        }
    }
}
