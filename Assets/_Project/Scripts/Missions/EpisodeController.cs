using System;
using UnityEngine;
using Campusano.Dialogue;
using Campusano.Objectives;
using Campusano.Persistence;
using Campusano.Economy;

namespace Campusano.Missions
{
    /// <summary>
    /// TICKET-EP1-01 v2. Episode-owned glue. The ONLY runtime caller of
    /// AdvanceOnDialogueComplete; routes dialogue endings to mission advances,
    /// cash awards, and objective updates. Owns the ChoiceRouter wiring.
    /// </summary>
    public class EpisodeController : MonoBehaviour
    {
        [SerializeField] private int episodeNumber = 1;

        // Set by the scene builder's Configure(); [SerializeField] so the refs
        // survive into builds (Configure runs in the editor only).
        [SerializeField] private MissionStateMachine _missions;
        [SerializeField] private DialogueController _dialogue;
        [SerializeField] private ObjectiveManager _objectives;
        [SerializeField] private SaveManager _saves;
        [SerializeField] private CashService _cash;

        // nodeId -> (signal or direct step advance, objective, cash)
        private string _lastEndedNode;

        public void Configure(MissionStateMachine missions, DialogueController dialogue,
                              ObjectiveManager objectives, SaveManager saves, CashService cash,
                              int episode = 1)
        {
            _missions = missions;
            _dialogue = dialogue;
            _objectives = objectives;
            _saves = saves;
            _cash = cash;
            episodeNumber = episode;
        }

        private void Start()
        {
            if (_dialogue != null)
            {
                _dialogue.OnDialogueEnded += HandleDialogueEnded;
                _dialogue.OnNodeStarted += HandleNodeStarted;
            }
            if (_missions != null)
            {
                _missions.OnStepChanged += HandleStepChanged;
                _missions.OnStepRestored += HandleStepRestored;
            }
            Boot();
        }

        private void OnDestroy()
        {
            if (_dialogue != null)
            {
                _dialogue.OnDialogueEnded -= HandleDialogueEnded;
                _dialogue.OnNodeStarted -= HandleNodeStarted;
            }
            if (_missions != null)
            {
                _missions.OnStepChanged -= HandleStepChanged;
                _missions.OnStepRestored -= HandleStepRestored;
            }
        }

        private void Boot()
        {
            // Load flow: profile -> episode save -> silent restore. Fresh start otherwise.
            if (_saves != null && _missions != null && _saves.LoadEpisode(episodeNumber, _missions))
            {
                Debug.Log($"[Episode] Loaded ep{episodeNumber} save @ {_missions.CurrentStep}.");
                return;
            }
            Debug.Log($"[Episode] Fresh ep{episodeNumber} start.");
            _missions?.AdvanceOnSignal("GameStart");
        }

        // ------------------------------------------------------------- events

        private void HandleDialogueEnded(DialogueEndReason reason)
        {
            string nodeId = _lastEndedNode;
            _lastEndedNode = null;
            if (reason == DialogueEndReason.Error) return;
            if (string.IsNullOrEmpty(nodeId)) return;

            // Route per Section 3 integrations.
            switch (nodeId)
            {
                case "C_First_Talk":
                    if (_saves?.CurrentProfile != null)
                    {
                        _saves.CurrentProfile.jobState.mookJobAssigned = true;
                        _saves.SaveProfile(_saves.CurrentProfile);
                    }
                    _missions?.AdvanceOnDialogueComplete("C_First_Talk");
                    break;
                case "knife_pull":
                    _missions?.AdvanceOnDialogueComplete("knife_pull");
                    break;
                case "C_Aftermath":
                    _cash?.Award(500, "Mook collection cut");
                    _missions?.AdvanceOnDialogueComplete("C_Aftermath");
                    break;
                case "father_lie":
                case "father_truth":
                case "father_resignation":
                    _missions?.AdvanceOnDialogueComplete(nodeId);
                    break;
            }
        }

        private void HandleStepChanged(Ep1Step step)
        {
            ApplyObjectiveForStep(step);
        }

        private void HandleStepRestored(Ep1Step step)
        {
            // Fast-forward without re-executing side effects: objectives only.
            ApplyObjectiveForStep(step);
        }

        // ------------------------------------------------------------- helpers

        private void HandleNodeStarted(Campusano.Dialogue.DialogueNode node)
        {
            if (node != null) _lastEndedNode = node.nodeId;
        }

        private void ApplyObjectiveForStep(Ep1Step step)
        {
            if (_objectives == null) return;
            switch (step)
            {
                case Ep1Step.Ep1_M1_S5_ApproachC:
                case Ep1Step.Ep1_M1_S6_DialogueC:
                    _objectives.SetObjective("M1_TALK_C", "Talk to C", "Find C in the back room.");
                    break;
                case Ep1Step.Ep1_M1_S7_CollectFromMook:
                case Ep1Step.Ep1_M1_S8_WalkToBackRoom:
                    _objectives.SetObjective("M1_MOOK", "Collect from Mook", "Go to the back room.",
                        new Vector3(13f, 1f, 11f), true);
                    break;
                case Ep1Step.Ep1_M2_S1_BackRoomSetup:
                case Ep1Step.Ep1_M2_S3_MookConfrontation:
                    _objectives.SetObjective("M2_CONFRONT", "Confront Mook", "Talk to Mook at the card table.");
                    break;
                case Ep1Step.Ep1_M3_S1_ReportToC:
                    _objectives.SetObjective("M3_REPORT", "Report to C", "Return to C with the money.");
                    break;
                default:
                    _objectives.ClearObjective();
                    break;
            }
        }
    }
}
