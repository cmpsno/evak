using System;
using UnityEngine;
using Campusano.Dialogue;
using Campusano.Objectives;
using Campusano.Persistence;

namespace Campusano.Missions
{
    /// <summary>
    /// Discrete Episode 1 steps, mapped 1:1 to the Story Bible build steps.
    /// </summary>
    public enum Ep1Step
    {
        Ep1_CO_S1_Setup,
        Ep1_CO_S2_Murder,
        Ep1_CO_S3_Witness,
        Ep1_CO_S4_TitleCard,
        Ep1_M1_S1_Spawn,
        Ep1_M1_S2_EnterClub,
        Ep1_M1_S3_ClubInterior,
        Ep1_M1_S5_ApproachC,
        Ep1_M1_S6_DialogueC,
        Ep1_M1_S7_CollectFromMook,
        Ep1_M1_S8_WalkToBackRoom,
        Ep1_M2_S1_BackRoomSetup,
        Ep1_M2_S3_MookConfrontation,
        Ep1_M2_S5_ShootingBeat,
        Ep1_M2_S6_MookDead,
        Ep1_M3_S1_ReportToC,
        Ep1_DP_S1_FatherHouse,
        Ep1_DP_S3_FatherChoice,
        Ep1_END_S1_NarratorEnding
    }

    public interface IMissionStateMachine
    {
        event Action<Ep1Step> OnStepChanged;

        Ep1Step CurrentStep { get; }
        void AdvanceToStep(Ep1Step nextStep);
        void TriggerStepFromVolume(string volumeId);
    }

    [Serializable]
    public struct VolumeStepMap
    {
        public string volumeId;
        public Ep1Step step;
    }

    /// <summary>
    /// Single source of truth for Episode 1 narrative progression. Listens to
    /// trigger volumes, dialogue completion, and dialogue choices; drives the
    /// objective manager and checkpoints every step change.
    /// </summary>
    public class MissionStateMachine : MonoBehaviour, IMissionStateMachine
    {
        public static MissionStateMachine Instance { get; private set; }

        [Header("Trigger volume id -> step mapping")]
        [SerializeField] private VolumeStepMap[] volumeSteps = new VolumeStepMap[]
        {
            new VolumeStepMap { volumeId = "Vol_HallwayDoor", step = Ep1Step.Ep1_M1_S8_WalkToBackRoom },
            new VolumeStepMap { volumeId = "Vol_BackRoomDoor", step = Ep1Step.Ep1_M2_S1_BackRoomSetup },
        };

        public event Action<Ep1Step> OnStepChanged;

        public Ep1Step CurrentStep { get; private set; } = Ep1Step.Ep1_M1_S1_Spawn;

        private IObjectiveManager _objectives;
        private ISaveManager _saves;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _saves = SaveManager.Instance;
            _objectives = ObjectiveManager.Instance;

            var runner = DialogueController.Instance;
            if (runner != null)
            {
                runner.OnTreeEnded += HandleTreeEnded;
                runner.OnChoiceSelected += HandleChoiceSelected;
            }

            // Restore checkpoint if one exists (quitting mid-mission resumes the step).
            if (_saves != null && _saves.HasSaveFile && _saves.LoadCheckpoint())
            {
                var data = _saves.CurrentData;
                if (data != null && Enum.TryParse(data.currentStepId, out Ep1Step saved))
                {
                    CurrentStep = saved;
                    Debug.Log("[Mission] Restored checkpoint at " + saved);
                    ReapplyObjectiveForStep(saved);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var runner = DialogueController.Instance;
            if (runner != null)
            {
                runner.OnTreeEnded -= HandleTreeEnded;
                runner.OnChoiceSelected -= HandleChoiceSelected;
            }
        }

        public void AdvanceToStep(Ep1Step nextStep)
        {
            if (nextStep == CurrentStep) return; // no double-triggering
            CurrentStep = nextStep;
            OnStepChanged?.Invoke(nextStep);
            _saves?.SaveCheckpoint(nextStep.ToString());
            Debug.Log("[Mission] Step -> " + nextStep);
        }

        public void TriggerStepFromVolume(string volumeId)
        {
            if (string.IsNullOrEmpty(volumeId)) return;
            foreach (var map in volumeSteps)
            {
                if (map.volumeId == volumeId)
                {
                    AdvanceToStep(map.step);
                    return;
                }
            }
            Debug.LogWarning("[Mission] Unmapped volume id: " + volumeId);
        }

        // ------------------------------------------------- dialogue wiring

        private void HandleTreeEnded(DialogueNode lastNode, string rootNodeId)
        {
            if (rootNodeId == "C_First_Talk")
            {
                CompleteCFirstTalk();
            }
            else if (lastNode != null && lastNode.completesObjectiveOnEnd
                     && _objectives?.CurrentObjective != null)
            {
                _objectives.CompleteObjective(_objectives.CurrentObjective.objectiveId);
            }
        }

        /// <summary>
        /// Ep1-M1-S6 done: flag the job, show the popup, set the objective,
        /// advance the step, checkpoint. Shared by the tree path and the
        /// legacy static-lines fallback.
        /// </summary>
        public void CompleteCFirstTalk()
        {
            CODClone.Core.JobState.MookJobAssigned = true;
            Debug.Log("[Story] Mook job assigned.");
            var ui = CODClone.UI.DialogueUI.Instance;
            if (ui != null) ui.ShowJobPopup("NEW JOB: Collect $2,000 from Mook");
            _objectives?.SetObjective(
                "M1_MOOK",
                "Collect from Mook",
                "Go to the back room. Card game.",
                new Vector3(9f, 0f, 12f), // back-room doorway
                showWaypoint: true);
            AdvanceToStep(Ep1Step.Ep1_M1_S7_CollectFromMook);
        }

        private void HandleChoiceSelected(DialogueChoiceFlag flag)
        {
            var data = _saves?.CurrentData?.jobState;
            if (data == null) return;
            switch (flag)
            {
                case DialogueChoiceFlag.Ep1_M2_Intimidate:
                    data.mookChoice = MookChoice.Intimidate;
                    break;
                case DialogueChoiceFlag.Ep1_M2_PrivateConversation:
                    data.mookChoice = MookChoice.PrivateConversation;
                    break;
                case DialogueChoiceFlag.Ep1_DP_Lie:
                    data.fatherChoice = FatherChoice.Lie;
                    break;
                case DialogueChoiceFlag.Ep1_DP_Truth:
                    data.fatherChoice = FatherChoice.Truth;
                    break;
                case DialogueChoiceFlag.Ep1_DP_Resignation:
                    data.fatherChoice = FatherChoice.Resignation;
                    break;
            }
            _saves.SaveCheckpoint(CurrentStep.ToString());
            Debug.Log("[Mission] Choice persisted: " + flag);
        }

        private void ReapplyObjectiveForStep(Ep1Step step)
        {
            // Re-show the Mook objective when resuming anywhere in the M1->M2 span.
            if (step >= Ep1Step.Ep1_M1_S7_CollectFromMook && step <= Ep1Step.Ep1_M2_S1_BackRoomSetup)
            {
                _objectives?.SetObjective(
                    "M1_MOOK",
                    "Collect from Mook",
                    "Go to the back room. Card game.",
                    new Vector3(9f, 0f, 12f),
                    showWaypoint: true);
            }
        }
    }
}
