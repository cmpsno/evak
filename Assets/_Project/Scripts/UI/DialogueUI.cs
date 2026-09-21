using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Campusano.Dialogue;
using CODClone.Core;

namespace CODClone.UI
{
    /// <summary>
    /// v0.1 interaction UI: prompt ("E — Talk to C"), static dialogue box
    /// (bottom third, speaker + line, E to advance), and a fading job popup.
    /// Ep1: also renders branching dialogue choices (tap on mobile, 1/2/3 on
    /// desktop) driven by Campusano.Dialogue.DialogueController.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        public static DialogueUI Instance { get; private set; }

        [SerializeField] private MonoBehaviour inputProviderBehaviour;
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text lineText;
        [SerializeField] private GameObject jobPopup;
        [SerializeField] private Text jobText;

        public bool IsDialogueOpen { get; private set; }

        private IInputProvider _input;
        // Legacy (v0.1) static-line mode:
        private string[] _lines;
        private int _lineIndex;
        private Action _onComplete;
        private bool _openedThisFrame;
        private float _jobTimer;
        // Ep1 runner mode (driven by DialogueController):
        private bool _runnerMode;
        private GameObject _choicePanel;
        private readonly List<Button> _choiceButtons = new List<Button>();
        private Font _font;

        private void Awake()
        {
            Instance = this;
            _input = inputProviderBehaviour as IInputProvider;
            if (_input == null)
            {
                var provider = FindObjectOfType<PlayerInputProvider>();
                if (provider != null) _input = provider;
            }
            HidePrompt();
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (jobPopup != null) jobPopup.SetActive(false);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildChoicePanel();
        }

        /// <summary>Choice buttons live above the dialogue panel, hidden until needed.</summary>
        private void BuildChoicePanel()
        {
            _choicePanel = new GameObject("ChoicePanel");
            _choicePanel.transform.SetParent(transform, false);
            var rect = _choicePanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(560f, 170f);
            rect.anchoredPosition = new Vector2(0f, 200f);
            var layout = _choicePanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.LowerCenter;
            _choicePanel.SetActive(false);
        }

        private Button GetChoiceButton(int index)
        {
            while (_choiceButtons.Count <= index)
            {
                int i = _choiceButtons.Count;
                var btnGO = new GameObject("ChoiceButton" + i);
                btnGO.transform.SetParent(_choicePanel.transform, false);
                var btnRect = btnGO.AddComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(560f, 48f);
                var img = btnGO.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
                var btn = btnGO.AddComponent<Button>();
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(btnGO.transform, false);
                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = Vector2.zero;
                var label = labelGO.AddComponent<Text>();
                label.font = _font;
                label.fontSize = 18;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
                btnGO.SetActive(false);
                _choiceButtons.Add(btn);
            }
            return _choiceButtons[index];
        }

        private void Update()
        {
            if (_openedThisFrame) { _openedThisFrame = false; return; }

            // Runner mode is advanced by DialogueController (E) / choice keys.
            if (IsDialogueOpen && !_runnerMode && _input != null && _input.InteractPressed)
                Advance();

            if (jobPopup != null && jobPopup.activeSelf)
            {
                _jobTimer -= Time.deltaTime;
                if (_jobTimer <= 0f) jobPopup.SetActive(false);
            }
        }

        public void ShowPrompt(string text)
        {
            if (promptText == null) return;
            promptText.text = text;
            promptText.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
        }

        public void ShowDialogue(string speaker, string[] lines, Action onComplete)
        {
            if (lines == null || lines.Length == 0) { onComplete?.Invoke(); return; }
            _runnerMode = false;
            _lines = lines;
            _lineIndex = 0;
            _onComplete = onComplete;
            IsDialogueOpen = true;
            _openedThisFrame = true; // don't let the opening E press skip line 0
            if (speakerText != null) speakerText.text = speaker;
            if (lineText != null) lineText.text = _lines[0];
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            HideChoices();
            HidePrompt();
        }

        // ------------------------------------------------- Ep1 runner API

        /// <summary>Show one line of a branching tree (driven by DialogueController).</summary>
        public void ShowDialogueNode(string speaker, string text)
        {
            _runnerMode = true;
            IsDialogueOpen = true;
            if (speakerText != null) speakerText.text = speaker;
            if (lineText != null) lineText.text = text;
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            HidePrompt();
        }

        /// <summary>Render choice buttons; onSelect receives the picked index.</summary>
        public void ShowChoices(IReadOnlyList<DialogueChoice> choices, Action<int> onSelect)
        {
            if (_choicePanel == null || choices == null) return;
            for (int i = 0; i < _choiceButtons.Count; i++)
                _choiceButtons[i].gameObject.SetActive(false);
            for (int i = 0; i < choices.Count; i++)
            {
                var btn = GetChoiceButton(i);
                var label = btn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = (i + 1) + ". " + choices[i].choiceText;
                int captured = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onSelect?.Invoke(captured));
                btn.gameObject.SetActive(true);
            }
            _choicePanel.SetActive(true);
        }

        public void HideChoices()
        {
            if (_choicePanel != null) _choicePanel.SetActive(false);
        }

        /// <summary>Close a runner-driven dialogue.</summary>
        public void CloseDialogue()
        {
            _runnerMode = false;
            IsDialogueOpen = false;
            HideChoices();
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        public void ShowJobPopup(string text)
        {
            if (jobText != null) jobText.text = text;
            if (jobPopup != null)
            {
                jobPopup.SetActive(true);
                _jobTimer = 3f;
            }
        }

        private void Advance()
        {
            _lineIndex++;
            if (_lineIndex >= _lines.Length)
            {
                IsDialogueOpen = false;
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
                var cb = _onComplete;
                _onComplete = null;
                cb?.Invoke();
            }
            else if (lineText != null)
            {
                lineText.text = _lines[_lineIndex];
            }
        }
    }
}
