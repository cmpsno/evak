using System;
using UnityEngine;

namespace Campusano.Objectives
{
    /// <summary>TICKET-EP1-01 v2. Locked shapes — do not add/rename fields.</summary>
    public enum ObjectiveState { Inactive, Active, Completed, Failed }

    [Serializable]
    public class ObjectiveData
    {
        public string objectiveId;
        public string title;
        public string description;
        public ObjectiveState state;
        public Vector3 waypointPosition;
        public bool hasWaypoint;
    }

    public interface IObjectiveManager
    {
        event Action<ObjectiveData> OnObjectiveActivated;
        event Action<ObjectiveData> OnObjectiveCompleted;
        event Action<ObjectiveData> OnObjectiveFailed;
        event Action OnObjectiveCleared;

        ObjectiveData CurrentObjective { get; }
        void SetObjective(string objectiveId, string title, string description,
                          Vector3 waypoint = default, bool showWaypoint = false);
        void CompleteObjective(string objectiveId);
        void FailObjective(string objectiveId, string reason);
        void ClearObjective();
    }

    /// <summary>
    /// Objectives are NOT persisted. On load, the current objective is re-derived
    /// from MissionStateMachine.CurrentStep via the OnStepRestored listener.
    /// HUD text + waypoint beacon are wired by the scene builder.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour, IObjectiveManager
    {
        public static ObjectiveManager Instance { get; private set; }

        public event Action<ObjectiveData> OnObjectiveActivated;
        public event Action<ObjectiveData> OnObjectiveCompleted;
        public event Action<ObjectiveData> OnObjectiveFailed;
        public event Action OnObjectiveCleared;

        public ObjectiveData CurrentObjective { get; private set; }

        // Wired by builder.
        private UnityEngine.UI.Text _titleText;
        private UnityEngine.UI.Text _descText;
        private GameObject _panel;
        private GameObject _waypointBeacon;

        public void Configure(UnityEngine.UI.Text titleText, UnityEngine.UI.Text descText,
                              GameObject panel, GameObject waypointBeacon)
        {
            _titleText = titleText;
            _descText = descText;
            _panel = panel;
            _waypointBeacon = waypointBeacon;
            RefreshHud();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetObjective(string objectiveId, string title, string description,
                                 Vector3 waypoint = default, bool showWaypoint = false)
        {
            CurrentObjective = new ObjectiveData
            {
                objectiveId = objectiveId,
                title = title,
                description = description,
                state = ObjectiveState.Active,
                waypointPosition = waypoint,
                hasWaypoint = showWaypoint
            };
            RefreshHud();
            OnObjectiveActivated?.Invoke(CurrentObjective);
            Debug.Log($"[Objective] Active: {title}");
        }

        public void CompleteObjective(string objectiveId)
        {
            if (CurrentObjective == null || CurrentObjective.objectiveId != objectiveId ||
                CurrentObjective.state != ObjectiveState.Active)
            {
                Debug.LogWarning($"[Objective] CompleteObjective on non-active objective '{objectiveId}'. No-op.");
                return;
            }
            CurrentObjective.state = ObjectiveState.Completed;
            RefreshHud();
            OnObjectiveCompleted?.Invoke(CurrentObjective);
            Debug.Log($"[Objective] Completed: {CurrentObjective.title}");
        }

        public void FailObjective(string objectiveId, string reason)
        {
            if (CurrentObjective == null || CurrentObjective.objectiveId != objectiveId ||
                CurrentObjective.state != ObjectiveState.Active)
            {
                Debug.LogWarning($"[Objective] FailObjective on non-active objective '{objectiveId}'. No-op.");
                return;
            }
            CurrentObjective.state = ObjectiveState.Failed;
            RefreshHud();
            OnObjectiveFailed?.Invoke(CurrentObjective);
            Debug.LogWarning($"[Objective] Failed: {CurrentObjective.title} ({reason})");
        }

        public void ClearObjective()
        {
            CurrentObjective = null;
            RefreshHud();
            OnObjectiveCleared?.Invoke();
        }

        private void RefreshHud()
        {
            bool show = CurrentObjective != null && CurrentObjective.state == ObjectiveState.Active;
            if (_panel != null) _panel.SetActive(show);
            if (show)
            {
                if (_titleText != null) _titleText.text = CurrentObjective.title;
                if (_descText != null) _descText.text = CurrentObjective.description;
            }
            if (_waypointBeacon != null)
                _waypointBeacon.SetActive(show && CurrentObjective.hasWaypoint);
            if (show && CurrentObjective.hasWaypoint && _waypointBeacon != null)
                _waypointBeacon.transform.position = CurrentObjective.waypointPosition;
        }
    }
}
