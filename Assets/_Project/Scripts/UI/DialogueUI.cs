using System;
using UnityEngine;
using UnityEngine.UI;

namespace Campusano.UI
{
    /// <summary>
    /// TICKET-EP1-01 v2 dialogue view (dumb). Driven by DialogueController.
    /// Mobile spec (Section 5): subtitle bottom 20%, black 70%, 18sp min,
    /// 3 lines max; speaker name above in speaker color; choice buttons
    /// bottom-center stack (max 3, 88px min, 20px spacing); tap-anywhere
    /// to continue + visible chevron.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        private GameObject _dialogueRoot;
        private Text _speakerText;
        private Text _subtitleText;
        private Text _continueHint;
        private GameObject _subtitlePanel;
        private RectTransform _choiceContainer;
        private Button _tapCatcher;

        private GameObject _promptGO;
        private Text _promptText;
        private GameObject _jobPopupGO;
        private Text _jobPopupText;

        private Action _onTap;

        private void Awake()
        {
            BuildUI();
            HideDialogue();
            HidePrompt();
        }

        // ------------------------------------------------------------- dialogue

        public void SetTapHandler(Action onTap) { _onTap = onTap; }

        public void ShowLine(string speakerName, Color color, string text, bool isNarrator)
        {
            _dialogueRoot.SetActive(true);
            _subtitlePanel.SetActive(true);
            _choiceContainer.gameObject.SetActive(false);
            if (isNarrator)
            {
                _speakerText.gameObject.SetActive(false);
                _subtitleText.color = Color.white;
            }
            else
            {
                _speakerText.gameObject.SetActive(true);
                _speakerText.text = speakerName;
                _speakerText.color = color;
                _subtitleText.color = Color.white;
            }
            _subtitleText.text = text;
            _continueHint.gameObject.SetActive(true);
        }

        public void ShowChoices(string[] choiceTexts, Action<int> onSelect)
        {
            _dialogueRoot.SetActive(true);
            _continueHint.gameObject.SetActive(false);
            // Clear old buttons.
            foreach (Transform child in _choiceContainer) Destroy(child.gameObject);
            int count = Math.Min(choiceTexts.Length, 3);
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                var btn = CreateChoiceButton(choiceTexts[i]);
                btn.onClick.AddListener(() => onSelect?.Invoke(idx));
            }
            _choiceContainer.gameObject.SetActive(true);
        }

        public void HideDialogue()
        {
            if (_dialogueRoot != null) _dialogueRoot.SetActive(false);
            _onTap = null;
        }

        // ------------------------------------------------------------- prompt / popup (v0.1 kept)

        public void ShowPrompt(string text)
        {
            _promptGO.SetActive(true);
            _promptText.text = text;
        }

        public void HidePrompt() { _promptGO.SetActive(false); }

        public void ShowJobPopup(string text)
        {
            _jobPopupGO.SetActive(true);
            _jobPopupText.text = text;
            CancelInvoke(nameof(HideJobPopup));
            Invoke(nameof(HideJobPopup), 4f);
        }

        private void HideJobPopup() { _jobPopupGO.SetActive(false); }

        // ------------------------------------------------------------- construction

        private void BuildUI()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _dialogueRoot = new GameObject("DialogueRoot");
            _dialogueRoot.transform.SetParent(transform, false);

            // Tap catcher: full-screen transparent button, bottom-most.
            var tapGO = new GameObject("TapCatcher");
            tapGO.transform.SetParent(_dialogueRoot.transform, false);
            var tapRect = tapGO.AddComponent<RectTransform>();
            tapRect.anchorMin = Vector2.zero; tapRect.anchorMax = Vector2.one;
            tapRect.offsetMin = Vector2.zero; tapRect.offsetMax = Vector2.zero;
            var tapImg = tapGO.AddComponent<Image>();
            tapImg.color = new Color(0, 0, 0, 0);
            _tapCatcher = tapGO.AddComponent<Button>();
            _tapCatcher.onClick.AddListener(() => _onTap?.Invoke());

            // Subtitle panel: bottom 20%.
            _subtitlePanel = new GameObject("SubtitlePanel");
            _subtitlePanel.transform.SetParent(_dialogueRoot.transform, false);
            var panelRect = _subtitlePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0); panelRect.anchorMax = new Vector2(1, 0.2f);
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            var bg = _subtitlePanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            _speakerText = MakeText(_subtitlePanel.transform, "SpeakerName", 20, TextAnchor.LowerLeft, font);
            var speakerRect = _speakerText.GetComponent<RectTransform>();
            speakerRect.anchorMin = new Vector2(0.05f, 0.62f); speakerRect.anchorMax = new Vector2(0.95f, 0.95f);
            speakerRect.offsetMin = Vector2.zero; speakerRect.offsetMax = Vector2.zero;

            _subtitleText = MakeText(_subtitlePanel.transform, "Subtitle", 18, TextAnchor.UpperLeft, font);
            var subRect = _subtitleText.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.05f, 0.18f); subRect.anchorMax = new Vector2(0.95f, 0.62f);
            subRect.offsetMin = Vector2.zero; subRect.offsetMax = Vector2.zero;

            _continueHint = MakeText(_subtitlePanel.transform, "ContinueHint", 14, TextAnchor.LowerRight, font);
            _continueHint.text = "TAP TO CONTINUE ▼";
            _continueHint.color = new Color(1, 1, 1, 0.6f);
            var hintRect = _continueHint.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.55f, 0.02f); hintRect.anchorMax = new Vector2(0.98f, 0.18f);
            hintRect.offsetMin = Vector2.zero; hintRect.offsetMax = Vector2.zero;

            // Choice container: bottom-center, above subtitle panel.
            var choiceGO = new GameObject("ChoiceContainer");
            choiceGO.transform.SetParent(_dialogueRoot.transform, false);
            _choiceContainer = choiceGO.AddComponent<RectTransform>();
            _choiceContainer.anchorMin = new Vector2(0.5f, 0.2f); _choiceContainer.anchorMax = new Vector2(0.5f, 0.2f);
            _choiceContainer.pivot = new Vector2(0.5f, 0f);
            _choiceContainer.anchoredPosition = new Vector2(0, 20);
            _choiceContainer.sizeDelta = new Vector2(600, 320);
            var layout = choiceGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Interact prompt (v0.1): centered, hidden by default.
            _promptGO = new GameObject("InteractPrompt");
            _promptGO.transform.SetParent(transform, false);
            var promptRect = _promptGO.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.5f); promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0, 60);
            promptRect.sizeDelta = new Vector2(600, 40);
            _promptText = MakeText(_promptGO.transform, "PromptText", 18, TextAnchor.MiddleCenter, font);
            var bgPrompt = _promptGO.AddComponent<Image>();
            bgPrompt.color = new Color(0, 0, 0, 0.55f);

            // Job popup (v0.1): top-center toast.
            _jobPopupGO = new GameObject("JobPopup");
            _jobPopupGO.transform.SetParent(transform, false);
            var jobRect = _jobPopupGO.AddComponent<RectTransform>();
            jobRect.anchorMin = new Vector2(0.5f, 1f); jobRect.anchorMax = new Vector2(0.5f, 1f);
            jobRect.pivot = new Vector2(0.5f, 1f);
            jobRect.anchoredPosition = new Vector2(0, -80);
            jobRect.sizeDelta = new Vector2(700, 60);
            _jobPopupText = MakeText(_jobPopupGO.transform, "JobText", 22, TextAnchor.MiddleCenter, font);
            _jobPopupText.color = new Color(1f, 0.85f, 0.3f);
            var jobBg = _jobPopupGO.AddComponent<Image>();
            jobBg.color = new Color(0, 0, 0, 0.65f);
            _jobPopupGO.SetActive(false);
        }

        private Text MakeText(Transform parent, string name, int size, TextAnchor anchor, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            return t;
        }

        private Button CreateChoiceButton(string label)
        {
            var go = new GameObject("ChoiceButton");
            go.transform.SetParent(_choiceContainer, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560, 88);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
            var btn = go.AddComponent<Button>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var t = MakeText(go.transform, "Label", 20, TextAnchor.MiddleCenter, font);
            t.text = label;
            // Invisible layout spacer keeps 88px min height.
            var spacer = go.AddComponent<LayoutElement>();
            spacer.minHeight = 88;
            spacer.preferredWidth = 560;
            return btn;
        }
    }
}
