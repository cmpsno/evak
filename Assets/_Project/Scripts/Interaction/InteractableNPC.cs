using UnityEngine;
using Campusano.Dialogue;
using Campusano.Missions;
using Campusano.Persistence;

namespace Campusano.Interaction
{
    /// <summary>
    /// TICKET-EP1-01 v2. Static NPC (C). Starts the branching dialogue tree;
    /// the mission advance + job flag happen via ChoiceRouter / EpisodeController,
    /// not here. Repeat line once the job is assigned.
    /// </summary>
    public class InteractableNPC : MonoBehaviour, Evak.Core.IInteractable
    {
        [SerializeField] private string npcName = "C";
        [SerializeField] private string dialogueTreeRoot = "C_First_Talk";
        [SerializeField] private string repeatLine = "Go handle it, kid.";

        public string PromptText => "Talk to " + npcName;

        public void Interact(GameObject interactor)
        {
            var saves = SaveManager.Instance;
            bool jobAssigned = saves != null && saves.CurrentProfile != null &&
                               saves.CurrentProfile.jobState.mookJobAssigned;

            var runner = DialogueController.Instance;
            if (runner == null) return;

            if (!jobAssigned)
            {
                // Live progression: approaching C starts the M1 dialogue beat.
                var missions = MissionStateMachine.Instance;
                if (missions != null) missions.AdvanceOnSignal("TalkedToC");
                runner.StartDialogueTree(dialogueTreeRoot);
            }
            else
            {
                runner.StartDialogueTree("C_Repeat");
            }
        }
    }
}
