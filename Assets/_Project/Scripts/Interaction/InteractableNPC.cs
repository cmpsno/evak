using UnityEngine;
using CODClone.Core;
using CODClone.UI;

namespace CODClone.Interaction
{
    /// <summary>
    /// v0.1 static dialogue stub for C. Prefers the Ep1 branching tree
    /// (DialogueController) when present; falls back to the legacy fixed lines.
    /// </summary>
    public class InteractableNPC : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName = "C";
        [SerializeField] private bool useDialogueTree = true;
        [SerializeField] private string dialogueTreeRoot = "C_First_Talk";
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "Look who it is. The kid who didn't rat.",
            "I got a job for you. Guy named Mook owes me two grand. He's late.",
            "Go collect it. Back room, card game. You know where to find him."
        };
        [SerializeField] private string repeatLine = "Go handle it, kid.";

        public string PromptText => "Talk to " + npcName;

        public void Interact(GameObject interactor)
        {
            if (DialogueUI.Instance == null) return;

            if (!JobState.MookJobAssigned)
            {
                var runner = Campusano.Dialogue.DialogueController.Instance;
                if (useDialogueTree && runner != null && !string.IsNullOrEmpty(dialogueTreeRoot))
                    runner.StartDialogueTree(dialogueTreeRoot);
                else
                    DialogueUI.Instance.ShowDialogue(npcName, dialogueLines, OnDialogueComplete);
            }
            else
            {
                DialogueUI.Instance.ShowDialogue(npcName, new[] { repeatLine }, null);
            }
        }

        private void OnDialogueComplete()
        {
            var machine = Campusano.Missions.MissionStateMachine.Instance;
            if (machine != null)
                machine.CompleteCFirstTalk();
            else
            {
                JobState.MookJobAssigned = true;
                Debug.Log("[Story] Mook job assigned.");
                if (DialogueUI.Instance != null)
                    DialogueUI.Instance.ShowJobPopup("NEW JOB: Collect $2,000 from Mook");
            }
        }
    }
}
