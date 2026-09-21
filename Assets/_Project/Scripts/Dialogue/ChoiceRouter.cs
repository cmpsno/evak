using UnityEngine;
using Campusano.Dialogue;
using Campusano.Persistence;

namespace Campusano.Missions
{
    /// <summary>
    /// TICKET-EP1-01 v2. One per episode. The ONLY writer of JobState choice
    /// fields from dialogue. DialogueController stays decoupled from persistence.
    /// Adding a new choice in a later episode means adding a case here —
    /// no dialogue data-shape changes, no JobState schema changes.
    /// </summary>
    public class ChoiceRouter : MonoBehaviour
    {
        public DialogueController dialogueController;
        public MissionStateMachine missionStateMachine;
        public SaveManager saveManager;

        private void OnEnable()
        {
            if (dialogueController != null)
                dialogueController.OnChoiceSelected += HandleChoice;
        }

        private void OnDisable()
        {
            if (dialogueController != null)
                dialogueController.OnChoiceSelected -= HandleChoice;
        }

        private void HandleChoice(string nodeId, string choiceId)
        {
            if (saveManager == null || saveManager.CurrentProfile == null)
            {
                Debug.LogError("[ChoiceRouter] No SaveManager/profile; cannot record choice.");
                return;
            }
            var job = saveManager.CurrentProfile.jobState;

            switch (choiceId)
            {
                case "intimidate":   job.mookChoice = MookChoice.Intimidate; break;
                case "private_convo": job.mookChoice = MookChoice.PrivateConversation; break;
                case "lie":          job.fatherChoice = FatherChoice.Lie; break;
                case "truth":        job.fatherChoice = FatherChoice.Truth; break;
                case "resignation":  job.fatherChoice = FatherChoice.Resignation; break;
                default:
                    Debug.LogWarning($"[ChoiceRouter] Unmapped choiceId: '{choiceId}' (node '{nodeId}').");
                    return;
            }

            saveManager.SaveProfile(saveManager.CurrentProfile);
            Debug.Log($"[ChoiceRouter] Recorded choice '{choiceId}' from node '{nodeId}'.");
        }
    }
}
