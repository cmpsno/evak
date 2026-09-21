using UnityEngine;
using CODClone.Core;
using Campusano.Dialogue;
using Campusano.UI;

namespace CODClone.Interaction
{
    /// <summary>
    /// Attach to the player. Each frame finds the nearest IInteractable within
    /// range in front of the camera, shows its prompt, and routes E presses.
    /// TICKET-EP1-01 v2: while dialogue is active the input gate kills
    /// InteractPressed, and this also bails out explicitly — no re-trigger.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputProviderBehaviour;
        [SerializeField] private float interactRange = 2.2f;
        [SerializeField] private float forwardDotMin = 0.4f;

        private IInputProvider _input;
        private IInteractable _current;
        private DialogueUI _ui;

        private void Awake()
        {
            _input = inputProviderBehaviour as IInputProvider
                ?? InputProviderLookup.FindOnSelf(this);
            _ui = FindObjectOfType<DialogueUI>();
        }

        private void Update()
        {
            var runner = DialogueController.Instance;
            if (runner != null && runner.IsDialogueActive)
            {
                _current = null;
                return; // taps belong to the dialogue; don't start new interactions
            }

            _current = FindNearest();
            if (_current != null && _ui != null)
                _ui.ShowPrompt("E — " + _current.PromptText);
            else if (_ui != null)
                _ui.HidePrompt();

            if (_current != null && _input != null && _input.InteractPressed)
            {
                if (_ui != null) _ui.HidePrompt();
                _current.Interact(gameObject);
                _current = null;
            }
        }

        private IInteractable FindNearest()
        {
            var cam = Camera.main;
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 forward = cam != null ? cam.transform.forward : transform.forward;

            IInteractable best = null;
            float bestDist = interactRange;
            foreach (var hit in Physics.OverlapSphere(origin, interactRange))
            {
                var interactable = InteractableLookup.FindOn(hit.gameObject);
                if (interactable == null) continue;
                Vector3 to = hit.transform.position - origin;
                float dist = to.magnitude;
                if (dist > bestDist) continue;
                if (Vector3.Dot(to.normalized, forward) < forwardDotMin) continue;
                best = interactable;
                bestDist = dist;
            }
            return best;
        }
    }
}
