using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Campusano.Persistence;

namespace Campusano.Missions
{
    /// <summary>TICKET-EP1-01 v2. Locked step enum — do not add/rename.</summary>
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

    public enum TriggerKind { Volume, DialogueComplete, Death, Timer, Signal }

    [Serializable]
    public struct StepTrigger
    {
        public Ep1Step fromStep;
        public Ep1Step toStep;
        public TriggerKind kind;
        public string triggerId;     // volumeId, nodeId, actorId, or signal id
        public float timerSeconds;   // for Timer only
    }

    public enum MissionDebugEvent
    {
        InvalidStepTransition,
        UntriggeredAdvance
    }

    public static class MissionDebug
    {
        public static event Action<MissionDebugEvent, string> OnDebugEvent;
        public static void Fire(MissionDebugEvent evt, string context)
        {
            OnDebugEvent?.Invoke(evt, context);
        }
    }

    public interface IMissionStateMachine
    {
        event Action<Ep1Step> OnStepChanged;      // fires on live progression only
        event Action<Ep1Step> OnStepRestored;     // fires on load-time restore only

        Ep1Step CurrentStep { get; }

        void AdvanceToStep(Ep1Step nextStep);
        void AdvanceOnVolumeEntered(string volumeId);
        void AdvanceOnDialogueComplete(string nodeId);
        void AdvanceOnDeath(string actorId);
        void AdvanceOnTimer(float seconds, Ep1Step targetStep);
        void AdvanceOnSignal(string signalId);

        void RestoreToStep(Ep1Step step);
    }

    /// <summary>
    /// Single source of truth for narrative progression. Every live AdvanceToStep
    /// writes an episode checkpoint (+ profile). RestoreToStep is silent and
    /// load-only: SaveManager brackets it via BeginLoad/EndLoad.
    /// </summary>
    public class MissionStateMachine : MonoBehaviour, IMissionStateMachine
    {
        public static MissionStateMachine Instance { get; private set; }

        [SerializeField] private StepTrigger[] triggers = new StepTrigger[0];
        [SerializeField] private int episodeNumber = 1;

        public event Action<Ep1Step> OnStepChanged;
        public event Action<Ep1Step> OnStepRestored;

        public Ep1Step CurrentStep { get; private set; } = Ep1Step.Ep1_M1_S1_Spawn;

        // Set by the scene builder's Configure(); serialized so it survives builds.
        [SerializeField] private SaveManager _saves;
        private bool _inLoadFlow;

        public void Configure(StepTrigger[] stepTriggers, SaveManager saves, int episode = 1)
        {
            triggers = stepTriggers ?? new StepTrigger[0];
            _saves = saves;
            episodeNumber = episode;
        }

        internal void BeginLoad() { _inLoadFlow = true; }
        internal void EndLoad() { _inLoadFlow = false; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------- advance

        public void AdvanceToStep(Ep1Step nextStep)
        {
            if (!IsValidTransition(CurrentStep, nextStep))
            {
                Debug.LogError($"[Mission] Invalid step transition: {CurrentStep} -> {nextStep}. Not advancing.");
                MissionDebug.Fire(MissionDebugEvent.InvalidStepTransition, $"{CurrentStep}->{nextStep}");
                return;
            }
            CurrentStep = nextStep;
            Debug.Log($"[Mission] Step -> {nextStep}");
            WriteCheckpoint();
            OnStepChanged?.Invoke(nextStep);
        }

        public void AdvanceOnVolumeEntered(string volumeId)
        {
            if (TryResolve(TriggerKind.Volume, volumeId, out var to)) AdvanceToStep(to);
            else Untriggered(volumeId);
        }

        public void AdvanceOnDialogueComplete(string nodeId)
        {
            if (TryResolve(TriggerKind.DialogueComplete, nodeId, out var to)) AdvanceToStep(to);
            else Untriggered(nodeId);
        }

        public void AdvanceOnDeath(string actorId)
        {
            if (TryResolve(TriggerKind.Death, actorId, out var to)) AdvanceToStep(to);
            else Untriggered(actorId);
        }

        public void AdvanceOnSignal(string signalId)
        {
            if (TryResolve(TriggerKind.Signal, signalId, out var to)) AdvanceToStep(to);
            else Untriggered(signalId);
        }

        public void AdvanceOnTimer(float seconds, Ep1Step targetStep)
        {
            StartCoroutine(TimerRoutine(seconds, targetStep));
        }

        private IEnumerator TimerRoutine(float seconds, Ep1Step targetStep)
        {
            yield return new WaitForSeconds(seconds);
            AdvanceToStep(targetStep);
        }

        // ------------------------------------------------------------- restore

        public void RestoreToStep(Ep1Step step)
        {
            if (!_inLoadFlow)
            {
                Debug.LogError("[Mission] RestoreToStep called outside the load flow. Use SaveManager load. Not executing.");
                return;
            }
            CurrentStep = step;
            Debug.Log($"[Mission] Restored to {step} (silent).");
            OnStepRestored?.Invoke(step);
        }

        // ------------------------------------------------------------- internals

        private bool IsValidTransition(Ep1Step from, Ep1Step to)
        {
            if (triggers == null) return false;
            foreach (var t in triggers)
                if (t.fromStep == from && t.toStep == to) return true;
            return false;
        }

        private bool TryResolve(TriggerKind kind, string id, out Ep1Step to)
        {
            to = CurrentStep;
            if (triggers == null) return false;
            foreach (var t in triggers)
            {
                if (t.fromStep == CurrentStep && t.kind == kind && t.triggerId == id)
                {
                    to = t.toStep;
                    return true;
                }
            }
            return false;
        }

        private void Untriggered(string id)
        {
            Debug.LogWarning($"[Mission] No trigger for '{id}' from {CurrentStep}. Not advancing.");
            MissionDebug.Fire(MissionDebugEvent.UntriggeredAdvance, $"{CurrentStep}:{id}");
        }

        private void WriteCheckpoint()
        {
            if (_saves == null || _saves.CurrentProfile == null) return;
            _saves.SaveEpisodeCheckpoint(episodeNumber, CurrentStep.ToString(), _saves.CurrentProfile.jobState);
        }
    }
}
