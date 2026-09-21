using System;
using UnityEngine;
using UnityEngine.UI;

namespace Campusano.Objectives
{
    public enum ObjectiveState
    {
        Inactive,
        Active,
        Completed,
        Failed
    }

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

        ObjectiveData CurrentObjective { get; }
        void SetObjective(string objectiveId, string title, string description,
            Vector3 waypoint = default, bool showWaypoint = false);
        void CompleteObjective(string objectiveId);
        void ClearObjective();
    }

    /// <summary>
    /// Single active objective with a self-built HUD (top-left title +
    /// description) and a world-space waypoint beacon (bobbing gold sphere).
    /// HUD is built in code so the system works without scene authoring.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour, IObjectiveManager
    {
        public static ObjectiveManager Instance { get; private set; }

        public event Action<ObjectiveData> OnObjectiveActivated;
        public event Action<ObjectiveData> OnObjectiveCompleted;

        public ObjectiveData CurrentObjective { get; private set; }

        private GameObject _hudPanel;
        private Text _titleText;
        private Text _descText;
        private GameObject _beacon;
        private float _beaconBaseY;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            BuildHud();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_beacon != null && _beacon.activeSelf)
            {
                _beacon.transform.position = new Vector3(
                    _beacon.transform.position.x,
                    _beaconBaseY + Mathf.Sin(Time.time * 2f) * 0.25f,
                    _beacon.transform.position.z);
            }
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
                hasWaypoint = showWaypoint,
            };
            if (_titleText != null) _titleText.text = title;
            if (_descText != null) _descText.text = description;
            if (_hudPanel != null) _hudPanel.SetActive(true);
            UpdateBeacon();
            OnObjectiveActivated?.Invoke(CurrentObjective);
            Debug.Log("[Objective] Activated: " + title);
        }

        public void CompleteObjective(string objectiveId)
        {
            if (CurrentObjective == null || CurrentObjective.objectiveId != objectiveId) return;
            CurrentObjective.state = ObjectiveState.Completed;
            OnObjectiveCompleted?.Invoke(CurrentObjective);
            Debug.Log("[Objective] Completed: " + CurrentObjective.title);
            ClearObjective();
        }

        public void ClearObjective()
        {
            CurrentObjective = null;
            if (_hudPanel != null) _hudPanel.SetActive(false);
            UpdateBeacon();
        }

        private void UpdateBeacon()
        {
            bool show = CurrentObjective != null && CurrentObjective.hasWaypoint;
            if (show)
            {
                if (_beacon == null)
                {
                    _beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    _beacon.name = "ObjectiveBeacon";
                    _beacon.transform.localScale = Vector3.one * 0.4f;
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(1f, 0.8f, 0.2f);
                    _beacon.GetComponent<Renderer>().sharedMaterial = mat;
                    var col = _beacon.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                }
                _beacon.transform.position = CurrentObjective.waypointPosition + Vector3.up * 1.6f;
                _beaconBaseY = _beacon.transform.position.y;
                _beacon.SetActive(true);
            }
            else if (_beacon != null)
            {
                _beacon.SetActive(false);
            }
        }

        private void BuildHud()
        {
            var canvasGO = new GameObject("ObjectiveHUD");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _hudPanel = new GameObject("ObjectivePanel");
            _hudPanel.transform.SetParent(canvasGO.transform, false);
            var rect = _hudPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(420f, 90f);
            rect.anchoredPosition = new Vector2(230f, -60f);
            var bg = _hudPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            var titleGO = new GameObject("ObjectiveTitle");
            titleGO.transform.SetParent(_hudPanel.transform, false);
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(-20f, 34f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            _titleText = titleGO.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 20;
            _titleText.color = new Color(1f, 0.85f, 0.4f);
            _titleText.text = "";

            var descGO = new GameObject("ObjectiveDesc");
            descGO.transform.SetParent(_hudPanel.transform, false);
            var descRect = descGO.AddComponent<RectTransform>();
            descRect.anchorMin = Vector2.zero;
            descRect.anchorMax = Vector2.one;
            descRect.sizeDelta = new Vector2(-20f, -44f);
            descRect.anchoredPosition = new Vector2(0f, -12f);
            _descText = descGO.AddComponent<Text>();
            _descText.font = font;
            _descText.fontSize = 16;
            _descText.color = Color.white;
            _descText.text = "";

            _hudPanel.SetActive(false);
        }
    }
}
