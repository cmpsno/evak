using System;
using System.Collections.Generic;
using UnityEngine;
using CODClone.Core;
using CODClone.UI;

namespace Campusano.Dialogue
{
    /// <summary>
    /// Branching dialogue engine for Episode 1. Holds a node lookup (inspector
    /// list + runtime-registered banks), drives DialogueUI line-by-line, renders
    /// up to 3 choice buttons (tap on mobile, keys 1-3 on desktop), and raises
    /// OnChoiceSelected so the mission machine can persist story flags.
    /// Movement is frozen while active (PlayerMovement watches DialogueUI).
    /// </summary>
    public class DialogueController : MonoBehaviour, IDialogueRunner
    {
        public static DialogueController Instance { get; private set; }

        [Header("Node bank (inspector-authored or registered at runtime)")]
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();

        public event Action<DialogueNode> OnNodeStarted;
        public event Action<DialogueChoiceFlag> OnChoiceSelected;
        public event Action OnDialogueEnded;
        /// <summary>Extra: last node of the finished tree + the root it started from.</summary>
        public event Action<DialogueNode, string> OnTreeEnded;

        public bool IsDialogueActive => _active;

        private readonly Dictionary<string, DialogueNode> _lookup =
            new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
        private IInputProvider _input;
        private DialogueNode _current;
        private string _rootId;
        private int _lineIndex;
        private bool _active;
        private bool _choicesShown;
        private bool _openedThisFrame;
        private DialogueUI _ui;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _ui = DialogueUI.Instance ?? FindObjectOfType<DialogueUI>();
            RebuildLookup();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Add nodes at runtime (e.g. Ep1DialogueBank). Safe to call before/after Awake.</summary>
        public void RegisterNodes(IEnumerable<DialogueNode> more)
        {
            if (more == null) return;
            foreach (var n in more)
            {
                if (n == null || string.IsNullOrEmpty(n.nodeId)) continue;
                _lookup[n.nodeId] = n;
            }
        }

        private void RebuildLookup()
        {
            _lookup.Clear();
            RegisterNodes(nodes);
        }

        private void Update()
        {
            if (!_active) return;
            if (_openedThisFrame) { _openedThisFrame = false; return; }
            if (_input == null)
            {
                var provider = FindObjectOfType<PlayerInputProvider>();
                if (provider != null) _input = provider;
                else return;
            }

            if (_choicesShown)
            {
                if (_input.Choice1Pressed) SelectChoice(0);
                else if (_input.Choice2Pressed) SelectChoice(1);
                else if (_input.Choice3Pressed) SelectChoice(2);
                return;
            }

            if (_input.InteractPressed) AdvanceLine();
        }

        public void StartDialogueTree(string rootNodeId)
        {
            if (string.IsNullOrEmpty(rootNodeId) || !_lookup.TryGetValue(rootNodeId, out var root))
            {
                Debug.LogWarning("[Dialogue] Unknown root node: " + rootNodeId);
                return;
            }
            if (_active) return;
            _active = true;
            _rootId = rootNodeId;
            _openedThisFrame = true; // the E that opened this must not skip line 0
            EnterNode(root);
        }

        public void AdvanceLine()
        {
            if (!_active || _choicesShown || _current == null) return;
            _lineIndex++;
            if (_current.lines != null && _lineIndex < _current.lines.Length)
            {
                ShowCurrentLine();
                return;
            }
            // Node exhausted: choices, next node, or tree end.
            if (_current.choices != null && _current.choices.Length > 0)
            {
                _choicesShown = true;
                _ui?.ShowChoices(_current.choices, SelectChoice);
                return;
            }
            if (!string.IsNullOrEmpty(_current.nextNodeId)
                && _lookup.TryGetValue(_current.nextNodeId, out var next))
            {
                EnterNode(next);
                return;
            }
            EndTree();
        }

        public void SelectChoice(int choiceIndex)
        {
            if (!_active || !_choicesShown || _current == null) return;
            if (_current.choices == null || choiceIndex < 0 || choiceIndex >= _current.choices.Length) return;
            var choice = _current.choices[choiceIndex];
            _choicesShown = false;
            _ui?.HideChoices();
            if (choice.flagToSet != DialogueChoiceFlag.None)
                OnChoiceSelected?.Invoke(choice.flagToSet);
            if (!string.IsNullOrEmpty(choice.targetNodeId)
                && _lookup.TryGetValue(choice.targetNodeId, out var next))
            {
                EnterNode(next);
            }
            else
            {
                EndTree();
            }
        }

        private void EnterNode(DialogueNode node)
        {
            _current = node;
            _lineIndex = 0;
            _choicesShown = false;
            OnNodeStarted?.Invoke(node);
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            if (_ui == null) return;
            var line = _current.lines[_lineIndex];
            string speaker = string.IsNullOrEmpty(line.speakerName) ? "" : line.speakerName;
            _ui.ShowDialogueNode(speaker, line.text);
            // TODO(Ep1-VO): play line.voiceClip via audio source when ElevenLabs lines land.
        }

        private void EndTree()
        {
            var last = _current;
            var root = _rootId;
            _active = false;
            _choicesShown = false;
            _current = null;
            _rootId = null;
            _ui?.CloseDialogue();
            if (last != null) OnTreeEnded?.Invoke(last, root);
            OnDialogueEnded?.Invoke();
        }
    }
}
