using System;
using UnityEngine;
using CODClone.Core;

namespace Campusano.Dialogue
{
    /// <summary>
    /// TICKET-EP1-01 v2 branching dialogue runner.
    /// Emits events only — never writes to JobState, ObjectiveManager, or missions.
    /// Only this class (and the future CinematicPlayer) may write Time.timeScale.
    /// </summary>
    public class DialogueController : MonoBehaviour, IDialogueRunner
    {
        public static DialogueController Instance { get; private set; }

        [SerializeField] private DialogueDatabase database;

        // Wired by the scene builder: view + gameplay input gate.
        private Campusano.UI.DialogueUI _view;
        private IInputProvider _input;

        public event Action<DialogueNode> OnNodeStarted;
        public event Action<DialogueLine> OnLineStarted;
        public event Action<string, string> OnChoiceSelected;
        public event Action<DialogueEndReason> OnDialogueEnded;

        private DialogueNode _currentNode;
        private int _lineIndex;
        private bool _active;
        private bool _awaitingChoice;
        private float _autoAdvanceTimer;
        private bool _timeScaleZeroed;

        public bool IsDialogueActive => _active;
        public DialogueDatabase Database => database;
        public string CurrentNodeId => _currentNode != null ? _currentNode.nodeId : null;

        public void Configure(DialogueDatabase db, Campusano.UI.DialogueUI view, IInputProvider input)
        {
            database = db;
            _view = view;
            _input = input;
            if (database != null) database.Validate();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // The scene builder no longer embeds the code-generated DB (its script
            // ref went stale in builds). Build it fresh so dialogue never ships empty.
            if (database == null)
                database = Ep1DialogueBank.BuildDatabase();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!_active || _awaitingChoice) return;
            var line = CurrentLine();
            if (line.HasValue && line.Value.autoAdvance)
            {
                // Unscaled: timeScale is 0 during InScene nodes.
                _autoAdvanceTimer -= Time.unscaledDeltaTime;
                if (_autoAdvanceTimer <= 0f) AdvanceLine();
            }

            // Desktop choice shortcuts (1/2/3 stay live — the gate only kills gameplay input).
            if (_awaitingChoice && _input != null)
            {
                if (_input.Choice1Pressed) SelectChoice(0);
                else if (_input.Choice2Pressed) SelectChoice(1);
                else if (_input.Choice3Pressed) SelectChoice(2);
            }
        }

        public void StartDialogueTree(string rootNodeId)
        {
            if (_active) ForceClose(DialogueEndReason.PlayerSkipped);
            if (database == null)
            {
                Debug.LogError("[Dialogue] StartDialogueTree: no database configured.");
                DialogueDebug.Fire(DialogueDebugEvent.MissingRootNode, rootNodeId);
                OnDialogueEnded?.Invoke(DialogueEndReason.Error);
                return;
            }
            var node = database.GetNode(rootNodeId);
            if (node == null)
            {
                Debug.LogError($"[Dialogue] Missing root node: '{rootNodeId}'.");
                DialogueDebug.Fire(DialogueDebugEvent.MissingRootNode, rootNodeId);
                OnDialogueEnded?.Invoke(DialogueEndReason.Error);
                return;
            }
            _active = true;
            SetGameplayInput(false);
            LoadNode(node);
        }

        public void AdvanceLine()
        {
            if (!_active || _awaitingChoice) return;
            var node = _currentNode;
            if (node == null || node.lines == null) { EndDialogue(DialogueEndReason.Error); return; }

            if (_lineIndex < node.lines.Length - 1)
            {
                _lineIndex++;
                ShowCurrentLine();
                return;
            }

            // Last line.
            bool hasChoices = node.choices != null && node.choices.Length > 0;
            if (hasChoices)
            {
                _awaitingChoice = true;
                if (_view != null)
                {
                    _view.SetTapHandler(null); // taps must hit a choice button
                    var texts = new string[node.choices.Length];
                    for (int i = 0; i < texts.Length; i++) texts[i] = node.choices[i].choiceText;
                    _view.ShowChoices(texts, SelectChoice);
                }
                return;
            }

            if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                var next = database.GetNode(node.nextNodeId);
                if (next == null)
                {
                    Debug.LogError($"[Dialogue] Missing next node: '{node.nextNodeId}' (from '{node.nodeId}').");
                    DialogueDebug.Fire(DialogueDebugEvent.MissingNextNode, node.nextNodeId);
                    EndDialogue(DialogueEndReason.Error);
                    return;
                }
                LoadNode(next);
                return;
            }

            EndDialogue(DialogueEndReason.ReachedTerminalNode);
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!_active || !_awaitingChoice) return;
            var node = _currentNode;
            if (node == null || node.choices == null || choiceIndex < 0 || choiceIndex >= node.choices.Length) return;
            var choice = node.choices[choiceIndex];
            var result = database.GetNode(choice.resultNodeId);
            if (result == null)
            {
                Debug.LogError($"[Dialogue] Missing result node: '{choice.resultNodeId}' (choice '{choice.choiceId}').");
                DialogueDebug.Fire(DialogueDebugEvent.MissingResultNode, choice.resultNodeId);
                EndDialogue(DialogueEndReason.Error);
                return;
            }
            _awaitingChoice = false;
            OnChoiceSelected?.Invoke(node.nodeId, choice.choiceId);
            LoadNode(result);
        }

        public void ForceClose(DialogueEndReason reason)
        {
            if (!_active) return;
            DialogueDebug.Fire(DialogueDebugEvent.ForceClosed, _currentNode != null ? _currentNode.nodeId : "");
            EndDialogue(reason);
        }

        // ------------------------------------------------------------- internals

        private void LoadNode(DialogueNode node)
        {
            _currentNode = node;
            _lineIndex = 0;
            _awaitingChoice = false;
            ApplyTimeScale(node.presentation);
            OnNodeStarted?.Invoke(node);
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            var line = CurrentLine();
            if (!line.HasValue) { EndDialogue(DialogueEndReason.Error); return; }
            var l = line.Value;
            var speaker = database.GetSpeaker(l.speakerId); // null + warning if missing; cosmetic only
            string name = speaker != null ? speaker.displayName : "";
            Color color = speaker != null ? speaker.subtitleColor : Color.white;
            bool isNarrator = _currentNode.presentation == DialoguePresentation.Narrator;
            if (_view != null)
            {
                _view.ShowLine(name, color, l.text, isNarrator);
                _view.SetTapHandler(AdvanceLine); // tap anywhere advances
            }
            OnLineStarted?.Invoke(l);
            _autoAdvanceTimer = l.autoAdvanceDelay;
            if (l.autoAdvance && _autoAdvanceTimer <= 0f)
            {
                // 0 = advance instantly.
                AdvanceLine();
            }
        }

        private DialogueLine? CurrentLine()
        {
            if (_currentNode == null || _currentNode.lines == null ||
                _lineIndex < 0 || _lineIndex >= _currentNode.lines.Length)
                return null;
            return _currentNode.lines[_lineIndex];
        }

        private void ApplyTimeScale(DialoguePresentation presentation)
        {
            if (presentation == DialoguePresentation.InScene)
            {
                Time.timeScale = 0f;
                _timeScaleZeroed = true;
            }
            else
            {
                if (_timeScaleZeroed) { Time.timeScale = 1f; _timeScaleZeroed = false; }
            }
        }

        private void EndDialogue(DialogueEndReason reason)
        {
            _active = false;
            _awaitingChoice = false;
            _currentNode = null;
            if (_timeScaleZeroed) { Time.timeScale = 1f; _timeScaleZeroed = false; }
            SetGameplayInput(true);
            if (_view != null) _view.HideDialogue();
            OnDialogueEnded?.Invoke(reason);
        }

        private void SetGameplayInput(bool enabled)
        {
            if (_input != null) _input.GameplayInputEnabled = enabled;
        }
    }
}
